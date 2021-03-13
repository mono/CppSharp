// ------------------------------------------------------------------------------------------- //
// CppSharp C++/CLI helpers
//
// String marshaling code adapted from:
//    http://blog.nuclex-games.com/mono-dotnet/cxx-cli-string-marshaling
//
// Licensed under the MIT license
// ------------------------------------------------------------------------------------------- //
#pragma once

#if defined(__cplusplus_cli)

#include <string>
#include <ostream>
#include <vcclr.h>
#include <msclr/marshal.h>

public interface class ICppInstance
{
  property System::IntPtr __Instance
  {
    System::IntPtr get();
    void set(System::IntPtr);
  }
};

// CLI extensions namespace
namespace clix {

  /// <summary>Encoding types for strings</summary>
  enum Encoding {

    /// <summary>ANSI encoding</summary>
    /// <remarks>
    ///   This is the default encoding you've most likely been using all around in C++. ANSI
    ///   means 8 Bit encoding with character codes depending on the system's selected code page.
    /// </remarks>
    E_ANSI,

    /// <summary>UTF-8 encoding</summary>
    /// <remarks>
    ///   This is the encoding commonly used for multilingual C++ strings. All ASCII characters
    ///   (0-127) will be represented as single bytes. Be aware that UTF-8 uses more than one
    ///   byte for extended characters, so std::string::length() might not reflect the actual
    ///   length of the string in characters if it contains any non-ASCII characters.
    /// </remarks>
    E_UTF8,

    /// <summary>UTF-16 encoding</summary>
    /// <remarks>
    ///   This is the suggested to be used for marshaling and the native encoding of .NET
    ///   strings. It is similar to UTF-8 but uses a minimum of two bytes per character, making
    ///   the number of bytes required for a given string better predictable. Be aware, however,
    ///   that UTF-16 can still use more than two bytes for a character, so std::wstring::length()
    ///   might not reflect the actual length of the string.
    /// </remarks>
    E_UTF16, E_UNICODE = E_UTF16

  };

  // Ignore this if you're just scanning the headers for informations!
  /* All this template stuff might seem like overkill, but it is well thought out and enables
     you to use a readable and convenient call while still keeping the highest possible code
     efficiency due to compile-time evaluation of the required conversion path.
  */
  namespace detail {

    // Get C++ string type for specified encoding
    template<Encoding encoding> struct StringTypeSelector;
    template<> struct StringTypeSelector<E_ANSI> { typedef std::string Type; };
    template<> struct StringTypeSelector<E_UTF8> { typedef std::string Type; };
    template<> struct StringTypeSelector<E_UTF16> { typedef std::wstring Type; };

    // Compile-time selection depending on whether a string is managed
    template<typename StringType> struct IfManaged {
      struct Select {
        template<typename TrueType, typename FalseType>
        struct Either { typedef FalseType Type; };
      };
      enum { Result = false };
    };
    template<> struct IfManaged<System::String ^> {
      struct Select {
        template<typename TrueType, typename FalseType>
        struct Either { typedef TrueType Type; };
      };
      enum { Result = true };
    };

    // Direction of the marshaling process
    enum MarshalingDirection {
      CxxFromNet,
      NetFromCxx
    };

    // The actual marshaling code
    template<MarshalingDirection direction> struct StringMarshaler;

    // Marshals to .NET from C++ strings
    template<> struct StringMarshaler<NetFromCxx> {

      template<Encoding encoding, typename SourceType>
      static System::String ^marshal(const SourceType &string) {
        // Constructs a std::[w]string in case someone gave us a char * to choke on
        return marshalCxxString<encoding, SourceType>(string);
      }

      template<Encoding encoding, typename SourceType>
      static System::String ^marshalCxxString(
        const typename StringTypeSelector<encoding>::Type &cxxString
      ) {
        typedef typename StringTypeSelector<encoding>::Type SourceStringType;
        size_t byteCount = cxxString.length() * sizeof(SourceStringType::value_type);

        // Empty strings would cause trouble accessing the array below
        if(byteCount == 0) {
          return System::String::Empty;
        }

        // Copy the C++ string contents into a managed array of bytes
        cli::array<unsigned char> ^bytes = gcnew cli::array<unsigned char>(byteCount);
        { pin_ptr<unsigned char> pinnedBytes = &bytes[0];
          memcpy(pinnedBytes, cxxString.c_str(), byteCount);
        }

        // Now let one of .NET's encoding classes do the rest
        return decode<encoding>(bytes);
      }

      private:
        // Converts a byte array based on the selected encoding
        template<Encoding encoding> static System::String ^decode(cli::array<unsigned char> ^bytes);
        template<> static System::String ^decode<E_ANSI>(cli::array<unsigned char> ^bytes) {
          return System::Text::Encoding::Default->GetString(bytes);
        }
        template<> static System::String ^decode<E_UTF8>(cli::array<unsigned char> ^bytes) {
          return System::Text::Encoding::UTF8->GetString(bytes);
        }
        template<> static System::String ^decode<E_UTF16>(cli::array<unsigned char> ^bytes) {
          return System::Text::Encoding::Unicode->GetString(bytes);
        }
    };

    // Marshals to C++ strings from .NET
    template<> struct StringMarshaler<CxxFromNet> {

      template<Encoding encoding, typename SourceType>
      static typename detail::StringTypeSelector<encoding>::Type marshal(
        System::String ^string
      ) {
        typedef typename StringTypeSelector<encoding>::Type StringType;

        // Empty strings would cause a problem when accessing the empty managed array
        if(!string || string->Length == 0) {
          return StringType();
        }

        // First, we use .NET's encoding classes to convert the string into a byte array
        cli::array<unsigned char> ^bytes = encode<encoding>(string);

        // Then we construct our native string from that byte array
        pin_ptr<unsigned char> pinnedBytes(&bytes[0]);
        return StringType(
          reinterpret_cast<StringType::value_type *>(static_cast<unsigned char *>(pinnedBytes)),
          bytes->Length / sizeof(StringType::value_type)
        );
      }

      template<> static std::wstring marshal<E_UTF16, System::String ^>(
        System::String ^string
      ) {
        // We can directly accesss the characters in the managed string
        pin_ptr<const wchar_t> pinnedChars(::PtrToStringChars(string));
        return std::wstring(pinnedChars, string->Length);
      }

      private:
        // Converts a string based on the selected encoding
        template<Encoding encoding> static cli::array<unsigned char> ^encode(System::String ^string);
        template<> static cli::array<unsigned char> ^encode<E_ANSI>(System::String ^string) {
          return System::Text::Encoding::Default->GetBytes(string);
        }
        template<> static cli::array<unsigned char> ^encode<E_UTF8>(System::String ^string) {
          return System::Text::Encoding::UTF8->GetBytes(string);
        }
        template<> static cli::array<unsigned char> ^encode<E_UTF16>(System::String ^string) {
          return System::Text::Encoding::Unicode->GetBytes(string);
        }

    };

  } // namespace detail

  // ----------------------------------------------------------------------------------------- //
  // clix::marshalString()
  // ----------------------------------------------------------------------------------------- //
  /// <summary>Marshals strings between .NET managed and C++ native</summary>
  /// <remarks>
  ///   This all-in-one function marshals native C++ strings to .NET strings and vice versa.
  ///   You have to specify an encoding to use for the conversion, which always applies to the
  ///   native C++ string as .NET always uses UTF-16 for its own strings.
  /// </remarks>
  /// <param name="string">String to be marshalled to the other side</param>
  /// <returns>The marshaled representation of the string</returns>
  template<Encoding encoding, typename SourceType>
  typename detail::IfManaged<SourceType>::Select::Either<
    typename detail::StringTypeSelector<encoding>::Type,
    System::String ^
  >::Type marshalString(SourceType string) {

    // Pass on the call to our nifty template routines
    return detail::StringMarshaler<
      detail::IfManaged<SourceType>::Result ? detail::CxxFromNet : detail::NetFromCxx
    >::marshal<encoding, SourceType>(string);

  }

} // namespace clix

// std::ostream marshaling using a System::IO::TextWriter
namespace msclr {
    namespace interop {
        namespace details {
            class text_writer_streambuf : public std::streambuf
            {
            public:
                text_writer_streambuf(const gcroot<System::IO::TextWriter^> & tw)
                    : std::streambuf()
                {
                    m_tw = tw;
                }
                ~text_writer_streambuf()
                {
                    m_tw->Flush();
                }
                int_type overflow(int_type ch)
                {
                    if (traits_type::not_eof(ch))
                    {
                        auto c = traits_type::to_char_type(ch);
                        xsputn(&c, 1);
                    }
                    return traits_type::not_eof(ch);
                }
                std::streamsize xsputn(const char *_Ptr, std::streamsize _Count)
                {
                    auto s = gcnew System::String(_Ptr, 0, (int)_Count, System::Text::Encoding::UTF8);
                    m_tw->Write(s);
                    return _Count;
                }
            private:
                gcroot<System::IO::TextWriter^> m_tw;
            };

            class text_writer_ostream : public std::ostream {
            public:
                text_writer_ostream(const gcroot<System::IO::TextWriter^> & s) :
                    std::ios(),
                    std::ostream(0),
                    m_sbuf(s)
                {
                        init(&m_sbuf);
                }
            private:
                text_writer_streambuf m_sbuf;
            };
        }

        template<>
        ref class context_node<std::ostream*, System::IO::TextWriter^> : public context_node_base
        {
        private:
            std::ostream* toPtr;
        public:
            context_node(std::ostream*& toObject, System::IO::TextWriter^ fromObject)
            {
                // (Step 4) Initialize toPtr to the appropriate empty value.
                toPtr = new details::text_writer_ostream(fromObject);
                // (Step 5) Insert conversion logic here.
                // (Step 6) Set toObject to the converted parameter.
                toObject = toPtr;
            }
            ~context_node()
            {
                this->!context_node();
            }
        protected:
            !context_node()
            {
                // (Step 7) Clean up native resources.
                delete toPtr;
            }
        };
    }
} // namespace msclr

#endif

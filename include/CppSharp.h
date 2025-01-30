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
#include <msclr/marshal_cppstd.h>

public interface class ICppInstance
{
  property System::IntPtr __Instance
  {
    System::IntPtr get();
    void set(System::IntPtr);
  }
};

namespace msclr {
    namespace interop {
        namespace details
        {
            inline _Check_return_ size_t GetUTF8StringSize(System::String^ _str)
            {
                cli::pin_ptr<const wchar_t> _pinned_ptr = PtrToStringChars(_str);

                auto _size = static_cast<size_t>(
                    ::WideCharToMultiByte(CP_UTF8, WC_NO_BEST_FIT_CHARS, _pinned_ptr,
                        _str->Length, NULL, 0, NULL, NULL));
                if (_size == 0 && _str->Length != 0)
                {
                    throw gcnew System::ArgumentException(_EXCEPTION_WC2MB);
                }
                // adding 1 for terminating nul
                _size += 1;
                return _size;
            }

            inline _Check_return_ size_t GetUnicodeStringSizeUTF8Source(_In_reads_z_(_count + 1) const char* _str, size_t _count)
            {
                if (_count > INT_MAX)
                {
                    throw gcnew System::ArgumentOutOfRangeException(_EXCEPTION_GREATER_THAN_INT_MAX);
                }

                auto _size = static_cast<size_t>(::MultiByteToWideChar(CP_UTF8, 0, _str, static_cast<int>(_count), NULL, 0));
                if (_size == 0 && _count != 0)
                {
                    throw gcnew System::ArgumentException(_EXCEPTION_MB2WC);
                }

                //adding 1 for terminating nul
                _size += 1;
                return _size;
            }

            inline void WriteUTF8String(_Out_writes_all_(_size) _Post_z_ char* _buf, size_t _size, System::String^ _str)
            {
                cli::pin_ptr<const wchar_t> _pinned_ptr = PtrToStringChars(_str);

                //checking for overflow
                if (_size > INT_MAX)
                {
                    // this should never happen if _size was returned by GetAnsiStringSize()
                    throw gcnew System::ArgumentOutOfRangeException(_EXCEPTION_GREATER_THAN_INT_MAX);
                }

                const auto _written = static_cast<size_t>(::WideCharToMultiByte(CP_UTF8, WC_NO_BEST_FIT_CHARS,
                    _pinned_ptr, _str->Length, _buf, static_cast<int>(_size), NULL, NULL));
                if (_written >= _size || (_written == 0 && _size != 1)) // allowing empty string
                {
                    throw gcnew System::ArgumentException(_EXCEPTION_WC2MB);
                }

                _buf[_written] = '\0';
            }

            inline void WriteUnicodeStringUTF8Source(_Out_writes_all_(_size) _Post_z_ wchar_t* _dest, size_t _size, _In_reads_bytes_(_count)const char* _src, size_t _count)
            {
                //checking for overflow
                if (_size > INT_MAX || _count > INT_MAX)
                {
                    throw gcnew System::ArgumentOutOfRangeException(_EXCEPTION_GREATER_THAN_INT_MAX);
                }

                size_t _written = static_cast<size_t>(::MultiByteToWideChar(CP_UTF8, 0, _src,
                    static_cast<int>(_count), _dest, static_cast<int>(_size)));
                if (_written >= _size || (_written == 0 && _size != 1)) // allowing empty string
                {
                    throw gcnew System::ArgumentException(_EXCEPTION_MB2WC);
                }

                _dest[_written] = L'\0';
            }

            inline System::String^ InternalUTF8ToStringHelper(_In_reads_z_(_count + 1)const char* _src, size_t _count)
            {
                const size_t _size = details::GetUnicodeStringSizeUTF8Source(_src, _count);
                if (_size > INT_MAX || _size <= 0)
                {
                    throw gcnew System::ArgumentOutOfRangeException(_EXCEPTION_GREATER_THAN_INT_MAX);
                }

                details::char_buffer<wchar_t> _wchar_buf(_size);
                if (_wchar_buf.get() == NULL)
                {
                    throw gcnew System::InsufficientMemoryException();
                }

                details::WriteUnicodeStringUTF8Source(_wchar_buf.get(), _size, _src, _count);
                return gcnew System::String(_wchar_buf.get(), 0, static_cast<int>(_size) - 1);
            }
        }

        template <class _To_Type, class _From_Type>
        inline _To_Type marshal_as_utf8(const _From_Type& _from_object);

        template <>
        inline std::string marshal_as_utf8(System::String^ const& _from_obj)
        {
            if (_from_obj == nullptr)
            {
                throw gcnew System::ArgumentNullException(_EXCEPTION_NULLPTR);
            }
            std::string _to_obj;
            size_t _size = details::GetUTF8StringSize(_from_obj);

            if (_size > 1)
            {
                // -1 because resize will automatically +1 for the NULL
                _to_obj.resize(_size - 1);
                char* _dest_buf = &(_to_obj[0]);

                details::WriteUTF8String(_dest_buf, _size, _from_obj);
            }

            return _to_obj;
        }

        template <>
        inline System::String^ marshal_as_utf8(char const* const& _from_obj)
        {
            return details::InternalUTF8ToStringHelper(_from_obj, strlen(_from_obj));
        }

        template <>
        inline System::String^ marshal_as_utf8(const std::string& _from_obj)
        {
            return details::InternalUTF8ToStringHelper(_from_obj.c_str(), _from_obj.length());
        }

        template <>
        inline System::String^ marshal_as_utf8(const std::string_view& _from_obj)
        {
            return details::InternalUTF8ToStringHelper(_from_obj.data(), _from_obj.length());
        }

        template <>
        inline System::String^ marshal_as(const std::string_view& _from_obj)
        {
            return details::InternalAnsiToStringHelper(_from_obj.data(), _from_obj.length());
        }

        template <>
        inline System::String^ marshal_as(const std::wstring_view& _from_obj)
        {
            return details::InternalUnicodeToStringHelper(_from_obj.data(), _from_obj.length());
        }
    }
}

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
        E_UTF16,
        E_UNICODE = E_UTF16
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
        template<>
        struct StringMarshaler<NetFromCxx> {

            template<Encoding encoding, typename SourceType>
            static System::String ^marshal(const SourceType& string) {
                if constexpr (encoding == E_UTF8)
                    return msclr::interop::marshal_as_utf8<System::String^>(string);
                else
                    return msclr::interop::marshal_as<System::String^>(string);
            }
        };

        // Marshals to C++ strings from .NET
        template<>
        struct StringMarshaler<CxxFromNet> {
            template<Encoding encoding, typename SourceType>
            static typename detail::StringTypeSelector<encoding>::Type marshal(
                System::String^ string
            ) {
                using ResultType = typename detail::StringTypeSelector<encoding>::Type;
                return msclr::interop::marshal_as<ResultType>(string);
            }

            template<>
            static std::string marshal<E_UTF8, System::String^>(System::String^ string) {
                return msclr::interop::marshal_as_utf8<std::string>(string);
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
    template<Encoding encoding, typename SourceType, class ResultType =
    typename detail::IfManaged<SourceType>::Select::Either<
        typename detail::StringTypeSelector<encoding>::Type,
        System::String^>::Type
    >
    ResultType marshalString(SourceType string) {
      constexpr detail::MarshalingDirection direction =
          detail::IfManaged<SourceType>::Result ? detail::CxxFromNet : detail::NetFromCxx;
      using StringMarshaler = detail::StringMarshaler<direction>;
      // Pass on the call to our nifty template routines
      return StringMarshaler::template marshal<encoding, SourceType>(string);
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

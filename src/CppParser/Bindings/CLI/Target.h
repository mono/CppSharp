#pragma once

#include "CppSharp.h"
#include <Target.h>

namespace CppSharp
{
    namespace Parser
    {
        enum struct ParserIntType;
        ref class ParserTargetInfo;
    }
}

namespace CppSharp
{
    namespace Parser
    {
        public enum struct ParserIntType
        {
            NoInt = 0,
            SignedChar = 1,
            UnsignedChar = 2,
            SignedShort = 3,
            UnsignedShort = 4,
            SignedInt = 5,
            UnsignedInt = 6,
            SignedLong = 7,
            UnsignedLong = 8,
            SignedLongLong = 9,
            UnsignedLongLong = 10
        };

        public ref class ParserTargetInfo : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ParserTargetInfo* NativePtr;
            property System::IntPtr __Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ParserTargetInfo(::CppSharp::CppParser::ParserTargetInfo* native);
            static ParserTargetInfo^ __CreateInstance(::System::IntPtr native);
            static ParserTargetInfo^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
            ParserTargetInfo();

            ParserTargetInfo(CppSharp::Parser::ParserTargetInfo^ _0);

            ~ParserTargetInfo();

            property System::String^ ABI
            {
                System::String^ get();
                void set(System::String^);
            }

            property CppSharp::Parser::ParserIntType Char16Type
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType Char32Type
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType Int64Type
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType IntMaxType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType IntPtrType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType SizeType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType UIntMaxType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType WCharType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property CppSharp::Parser::ParserIntType WIntType
            {
                CppSharp::Parser::ParserIntType get();
                void set(CppSharp::Parser::ParserIntType);
            }

            property unsigned int BoolAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int BoolWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int CharAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int CharWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int Char16Align
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int Char16Width
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int Char32Align
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int Char32Width
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int HalfAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int HalfWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int FloatAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int FloatWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int DoubleAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int DoubleWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int ShortAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int ShortWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int IntAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int IntWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int IntMaxTWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongDoubleAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongDoubleWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongLongAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int LongLongWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int PointerAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int PointerWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int WCharAlign
            {
                unsigned int get();
                void set(unsigned int);
            }

            property unsigned int WCharWidth
            {
                unsigned int get();
                void set(unsigned int);
            }

            protected:
            bool __ownsNativeInstance;
        };
    }
}

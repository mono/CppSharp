/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#using <CppSharp.AST.dll>
using namespace System::Collections::Generic;

public enum ParserIntType 
{
    NoInt = 0,
    SignedChar,
    UnsignedChar,
    SignedShort,
    UnsignedShort,
    SignedInt,
    UnsignedInt,
    SignedLong,
    UnsignedLong,
    SignedLongLong,
    UnsignedLongLong
};

public ref struct ParserTargetInfo
{
    System::String^ ABI;

    ParserIntType Char16Type;
    ParserIntType Char32Type;
    ParserIntType Int64Type;
    ParserIntType IntMaxType;
    ParserIntType IntPtrType;
    ParserIntType SizeType;
    ParserIntType UIntMaxType;
    ParserIntType WCharType;
    ParserIntType WIntType;

    unsigned int BoolAlign;
    unsigned int BoolWidth;
    unsigned int CharAlign;
    unsigned int CharWidth;
    unsigned int Char16Align;
    unsigned int Char16Width;
    unsigned int Char32Align;
    unsigned int Char32Width;
    unsigned int HalfAlign;
    unsigned int HalfWidth;
    unsigned int FloatAlign;
    unsigned int FloatWidth;
    unsigned int DoubleAlign;
    unsigned int DoubleWidth;
    unsigned int ShortAlign;
    unsigned int ShortWidth;
    unsigned int IntAlign;
    unsigned int IntWidth;
    unsigned int IntMaxTWidth;
    unsigned int LongAlign;
    unsigned int LongWidth;
    unsigned int LongDoubleAlign;
    unsigned int LongDoubleWidth;
    unsigned int LongLongAlign;
    unsigned int LongLongWidth;
    unsigned int PointerAlign;
    unsigned int PointerWidth;
    unsigned int WCharAlign;
    unsigned int WCharWidth;
};

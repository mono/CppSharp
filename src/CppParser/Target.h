/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include "Helpers.h"

namespace CppSharp { namespace CppParser {

enum class ParserIntType
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

struct CS_API ParserTargetInfo
{
    ParserTargetInfo();
    ~ParserTargetInfo();

    STRING(ABI);

    ParserIntType char16Type;
    ParserIntType char32Type;
    ParserIntType int64Type;
    ParserIntType intMaxType;
    ParserIntType intPtrType;
    ParserIntType sizeType;
    ParserIntType uIntMaxType;
    ParserIntType wCharType;
    ParserIntType wIntType;

    unsigned int boolAlign;
    unsigned int boolWidth;
    unsigned int charAlign;
    unsigned int charWidth;
    unsigned int char16Align;
    unsigned int char16Width;
    unsigned int char32Align;
    unsigned int char32Width;
    unsigned int halfAlign;
    unsigned int halfWidth;
    unsigned int floatAlign;
    unsigned int floatWidth;
    unsigned int doubleAlign;
    unsigned int doubleWidth;
    unsigned int shortAlign;
    unsigned int shortWidth;
    unsigned int intAlign;
    unsigned int intWidth;
    unsigned int intMaxTWidth;
    unsigned int longAlign;
    unsigned int longWidth;
    unsigned int longDoubleAlign;
    unsigned int longDoubleWidth;
    unsigned int longLongAlign;
    unsigned int longLongWidth;
    unsigned int pointerAlign;
    unsigned int pointerWidth;
    unsigned int wCharAlign;
    unsigned int wCharWidth;
    unsigned int float128Align;
    unsigned int float128Width;
};

} }

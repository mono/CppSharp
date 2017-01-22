/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Target.h"

namespace CppSharp { namespace CppParser {

ParserTargetInfo::ParserTargetInfo() :
    boolAlign(0),
    boolWidth(0),
    charAlign(0),
    charWidth(0),
    char16Align(0),
    char16Width(0),
    char32Align(0),
    char32Width(0),
    halfAlign(0),
    halfWidth(0),
    floatAlign(0),
    floatWidth(0),
    doubleAlign(0),
    doubleWidth(0),
    shortAlign(0),
    shortWidth(0),
    intAlign(0),
    intWidth(0),
    intMaxTWidth(0),
    longAlign(0),
    longWidth(0),
    longDoubleAlign(0),
    longDoubleWidth(0),
    longLongAlign(0),
    longLongWidth(0),
    pointerAlign(0),
    pointerWidth(0),
    wCharAlign(0),
    wCharWidth(0)
{
}

ParserTargetInfo::~ParserTargetInfo() {}

} }
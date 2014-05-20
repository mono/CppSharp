#include "Sources.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::SourceLocation::SourceLocation(::CppSharp::CppParser::SourceLocation* native)
{
    __ID = native->ID;
}

CppSharp::Parser::SourceLocation::SourceLocation(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::SourceLocation*)native.ToPointer();
    __ID = __native->ID;
}

CppSharp::Parser::SourceLocation::SourceLocation(unsigned int ID)
{
    ::CppSharp::CppParser::SourceLocation _native(ID);
    this->ID = _native.ID;
}

unsigned int CppSharp::Parser::SourceLocation::ID::get()
{
    return __ID;
}

void CppSharp::Parser::SourceLocation::ID::set(unsigned int value)
{
    __ID = value;
}


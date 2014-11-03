#pragma once

#include "CppSharp.h"
#include <Sources.h>

namespace CppSharp
{
    namespace Parser
    {
        value struct SourceLocation;
    }
}

namespace CppSharp
{
    namespace Parser
    {
        public value struct SourceLocation
        {
        public:

            SourceLocation(::CppSharp::CppParser::SourceLocation* native);
            static SourceLocation^ __CreateInstance(::System::IntPtr native);
            SourceLocation(unsigned int ID);

            property unsigned int ID
            {
                unsigned int get();
                void set(unsigned int);
            }

            private:
            unsigned int __ID;
        };
    }
}

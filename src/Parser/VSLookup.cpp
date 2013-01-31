/************************************************************************
*
* Cxxi
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#ifdef _MSC_VER

#include <string>
#include <vector>
#include <cstdio>

static std::string exec(const char* cmd)
{
    FILE* pipe = _popen(cmd, "r");
    if (!pipe) return "ERROR";
    char buffer[128];
    std::string result = "";
    while(!feof(pipe)) {
        if(fgets(buffer, 128, pipe) != NULL)
            result += buffer;
    }
    _pclose(pipe);
    return result;
}

#pragma comment(lib, "shlwapi.lib")     //_popen, _pclose
#include <shlwapi.h>

//
//  Returns true if got settings, false if not.
//
bool GetVisualStudioEnv(
    const char* envVar,                         // [in] "INCLUDE" / "LIBPATH"
    std::vector<std::string>& vecPathes,
    int iProbeFrom = 0, int iProbeTo = 0        // 2005 - 8, 2008 - 9, 2010 - 10
)
{
    std::string s;
    
    if( iProbeFrom == 0 && iProbeTo == 0 )
    {
        // Environment variable specifies which VS to use.
        if( getenv("VSENV") != NULL )
        {
            int iVer = atoi( getenv("VSENV") );
            
            if( iVer == 0 )
                return false;
            
            return GetVisualStudioEnv(envVar, vecPathes, iVer, iVer );
        }
        
        iProbeFrom = 20;
        iProbeTo = 9;  
    }

    for( int iProbeVer = iProbeFrom; iProbeVer >= iProbeTo; iProbeVer-- )
    {
        char envvar[64];
        sprintf(envvar, "VS%d0COMNTOOLS", iProbeVer);
        
        // Not defined
        if( getenv(envvar) == NULL ) 
            continue;

        std::string cmd = getenv(envvar);
        cmd += "\\..\\..\\vc\\vcvarsall.bat";
        
        if( !PathFileExistsA( cmd.c_str() ) )
            continue;
            
        //printf("Located: %s", envvar);
        
        cmd = "cmd /C \"" + cmd + "\" ^>nul ^& set";
        s = exec(cmd.c_str());
        break;
    }
    
    if( s.length() == 0 ) 
        return false;

    char* envline;
    
    vecPathes.clear();

    for (envline = strtok( &s[0], "\n" );  envline;  envline = strtok( NULL, "\n" ))
    {
        char* varend = strchr( envline, '=');
        if( !varend )
            continue;
        
        *varend = 0;
        
        if( strcmp(envline,envVar) == 0 )
        {
            char* val;
            for (val = strtok( varend + 1, ";" );  val;  val = strtok( NULL, ";" ))
            {
                vecPathes.push_back(val);
            }
        }
        *varend = '=';
    }

    return true;
}

#endif
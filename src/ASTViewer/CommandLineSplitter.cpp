#ifdef _WIN32
#include <windows.h>
#else
#include <wordexp.h>
#endif

#include <vector>
#include <string>
#include <cassert>

std::vector<std::string> splitCommandLine(std::string const &cmdline)
{
    int i;
    char **argv = NULL;
    int argc;
    std::vector<std::string> result;
    // Posix.
#ifndef _WIN32
    {
        wordexp_t p;

        // Note! This expands shell variables.
        if (wordexp(cmdline.c_str(), &p, 0))
        {
            return result;
        }

        argc = p.we_wordc;

        if (!(argv = (char**) calloc(argc, sizeof(char *))))
        {
            goto fail;
        }

        for (i = 0; i < p.we_wordc; i++)
        {
            result.push_back(p.we_wordv[i]);
        }

        wordfree(&p);

        return result;
    fail:
        wordfree(&p);
        return result;
    }
#else // WIN32
    {
        wchar_t **wargs = NULL;
        size_t needed = 0;
        wchar_t *cmdlinew = NULL;
        size_t len = cmdline.size() + 1;

        if (!(cmdlinew = static_cast<wchar_t*>(calloc(len, sizeof(wchar_t)))))
            goto fail;

        if (!MultiByteToWideChar(CP_ACP, 0, cmdline.c_str(), -1, cmdlinew, len))
            goto fail;

        if (!(wargs = CommandLineToArgvW(cmdlinew, &argc)))
            goto fail;

        // Convert from wchar_t * to ANSI char *
        for (i = 0; i < argc; i++)
        {
            // Get the size needed for the target buffer.
            // CP_ACP = Ansi Codepage.
            needed = WideCharToMultiByte(CP_ACP, 0, wargs[i], -1,
                NULL, 0, NULL, NULL);
            char *argv;
            if (!(argv = static_cast<char*>(malloc(needed))))
                goto fail;

            // Do the conversion.
            needed = WideCharToMultiByte(CP_ACP, 0, wargs[i], -1,
                argv, needed, NULL, NULL);
            result.push_back(argv);
            free(argv);
        }

        if (wargs) LocalFree(wargs);
        if (cmdlinew) free(cmdlinew);
        return result;

    fail:
        if (wargs) LocalFree(wargs);
        if (cmdlinew) free(cmdlinew);
    }
#endif // WIN32
}
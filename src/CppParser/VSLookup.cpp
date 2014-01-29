/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#ifdef _MSC_VER

#include <string>
#include <vector>
#include <cctype>

// Include the necessary headers to interface with the Windows registry and
// environment.
#define WIN32_LEAN_AND_MEAN
#define NOGDI
#define NOMINMAX
#include <Windows.h>

/// \brief Read registry string.
/// This also supports a means to look for high-versioned keys by use
/// of a $VERSION placeholder in the key path.
/// $VERSION in the key path is a placeholder for the version number,
/// causing the highest value path to be searched for and used.
/// I.e. "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\$VERSION".
/// There can be additional characters in the component.  Only the numeric
/// characters are compared.
static bool getSystemRegistryString(const char *keyPath, const char *valueName,
                                    char *value, size_t maxLength) {
  HKEY hRootKey = NULL;
  HKEY hKey = NULL;
  const char* subKey = NULL;
  DWORD valueType;
  DWORD valueSize = maxLength - 1;
  long lResult;
  bool returnValue = false;

  if (strncmp(keyPath, "HKEY_CLASSES_ROOT\\", 18) == 0) {
    hRootKey = HKEY_CLASSES_ROOT;
    subKey = keyPath + 18;
  } else if (strncmp(keyPath, "HKEY_USERS\\", 11) == 0) {
    hRootKey = HKEY_USERS;
    subKey = keyPath + 11;
  } else if (strncmp(keyPath, "HKEY_LOCAL_MACHINE\\", 19) == 0) {
    hRootKey = HKEY_LOCAL_MACHINE;
    subKey = keyPath + 19;
  } else if (strncmp(keyPath, "HKEY_CURRENT_USER\\", 18) == 0) {
    hRootKey = HKEY_CURRENT_USER;
    subKey = keyPath + 18;
  } else {
    return false;
  }

  const char *placeHolder = strstr(subKey, "$VERSION");
  char bestName[256];
  bestName[0] = '\0';
  // If we have a $VERSION placeholder, do the highest-version search.
  if (placeHolder) {
    const char *keyEnd = placeHolder - 1;
    const char *nextKey = placeHolder;
    // Find end of previous key.
    while ((keyEnd > subKey) && (*keyEnd != '\\'))
      keyEnd--;
    // Find end of key containing $VERSION.
    while (*nextKey && (*nextKey != '\\'))
      nextKey++;
    size_t partialKeyLength = keyEnd - subKey;
    char partialKey[256];
    if (partialKeyLength > sizeof(partialKey))
      partialKeyLength = sizeof(partialKey);
    strncpy(partialKey, subKey, partialKeyLength);
    partialKey[partialKeyLength] = '\0';
    HKEY hTopKey = NULL;
    lResult = RegOpenKeyExA(hRootKey, partialKey, 0, KEY_READ, &hTopKey);
    if (lResult == ERROR_SUCCESS) {
      char keyName[256];
      int bestIndex = -1;
      double bestValue = 0.0;
      DWORD index, size = sizeof(keyName) - 1;
      for (index = 0; RegEnumKeyExA(hTopKey, index, keyName, &size, NULL,
          NULL, NULL, NULL) == ERROR_SUCCESS; index++) {
        const char *sp = keyName;
        while (*sp && !isdigit(*sp))
          sp++;
        if (!*sp)
          continue;
        const char *ep = sp + 1;
        while (*ep && (isdigit(*ep) || (*ep == '.')))
          ep++;
        char numBuf[32];
        strncpy(numBuf, sp, sizeof(numBuf) - 1);
        numBuf[sizeof(numBuf) - 1] = '\0';
        double value = strtod(numBuf, NULL);

        // Check if InstallDir key value exists.
        bool isViableVersion = false;

        lResult = RegOpenKeyExA(hTopKey, keyName, 0, KEY_READ, &hKey);
        if (lResult == ERROR_SUCCESS) {
          lResult = RegQueryValueExA(hKey, valueName, NULL, NULL, NULL, NULL);
          if (lResult == ERROR_SUCCESS)
            isViableVersion = true;
          RegCloseKey(hKey);
        }

        if (isViableVersion && (value > bestValue)) {
          bestIndex = (int)index;
          bestValue = value;
          strcpy(bestName, keyName);
        }
        size = sizeof(keyName) - 1;
      }
      // If we found the highest versioned key, open the key and get the value.
      if (bestIndex != -1) {
        // Append rest of key.
        strncat(bestName, nextKey, sizeof(bestName) - 1);
        bestName[sizeof(bestName) - 1] = '\0';
        // Open the chosen key path remainder.
        lResult = RegOpenKeyExA(hTopKey, bestName, 0, KEY_READ, &hKey);
        if (lResult == ERROR_SUCCESS) {
          lResult = RegQueryValueExA(hKey, valueName, NULL, &valueType,
            (LPBYTE)value, &valueSize);
          if (lResult == ERROR_SUCCESS)
            returnValue = true;
          RegCloseKey(hKey);
        }
      }
      RegCloseKey(hTopKey);
    }
  } else {
    lResult = RegOpenKeyExA(hRootKey, subKey, 0, KEY_READ, &hKey);
    if (lResult == ERROR_SUCCESS) {
      lResult = RegQueryValueExA(hKey, valueName, NULL, &valueType,
        (LPBYTE)value, &valueSize);
      if (lResult == ERROR_SUCCESS)
        returnValue = true;
      RegCloseKey(hKey);
    }
  }
  return returnValue;
}

/// \brief Get Windows SDK installation directory.
static bool getWindowsSDKDir(std::string &path) {
  char windowsSDKInstallDir[256];
  // Try the Windows registry.
  bool hasSDKDir = getSystemRegistryString(
   "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows\\$VERSION",
                                           "InstallationFolder",
                                           windowsSDKInstallDir,
                                           sizeof(windowsSDKInstallDir) - 1);
    // If we have both vc80 and vc90, pick version we were compiled with.
  if (hasSDKDir && windowsSDKInstallDir[0]) {
    path = windowsSDKInstallDir;
    return true;
  }
  return false;
}

  // Get Visual Studio installation directory.
static bool getVisualStudioDir(std::string &path) {
  // First check the environment variables that vsvars32.bat sets.
  const char* vcinstalldir = getenv("VCINSTALLDIR");
  if (vcinstalldir) {
    char *p = const_cast<char *>(strstr(vcinstalldir, "\\VC"));
    if (p)
      *p = '\0';
    path = vcinstalldir;
    return true;
  }

  char vsIDEInstallDir[256];
  char vsExpressIDEInstallDir[256];
  // Then try the windows registry.
  bool hasVCDir = getSystemRegistryString(
    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\$VERSION",
    "InstallDir", vsIDEInstallDir, sizeof(vsIDEInstallDir) - 1);
  bool hasVCExpressDir = getSystemRegistryString(
    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VCExpress\\$VERSION",
    "InstallDir", vsExpressIDEInstallDir, sizeof(vsExpressIDEInstallDir) - 1);
    // If we have both vc80 and vc90, pick version we were compiled with.
  if (hasVCDir && vsIDEInstallDir[0]) {
    char *p = (char*)strstr(vsIDEInstallDir, "\\Common7\\IDE");
    if (p)
      *p = '\0';
    path = vsIDEInstallDir;
    return true;
  }

  if (hasVCExpressDir && vsExpressIDEInstallDir[0]) {
    char *p = (char*)strstr(vsExpressIDEInstallDir, "\\Common7\\IDE");
    if (p)
      *p = '\0';
    path = vsExpressIDEInstallDir;
    return true;
  }

  // Try the environment.
  const char *vs120comntools = getenv("VS120COMNTOOLS");
  const char *vs110comntools = getenv("VS110COMNTOOLS");
  const char *vs100comntools = getenv("VS100COMNTOOLS");
  const char *vs90comntools = getenv("VS90COMNTOOLS");
  const char *vs80comntools = getenv("VS80COMNTOOLS");
  const char *vscomntools = NULL;

  // Try to find the version that we were compiled with
  if(false) {}
  #if (_MSC_VER >= 1800)  // VC120
  else if (vs120comntools) {
    vscomntools = vs120comntools;
  }
  #elif (_MSC_VER == 1700)  // VC110
  else if (vs110comntools) {
    vscomntools = vs110comntools;
  }
  #elif (_MSC_VER == 1600)  // VC100
  else if(vs100comntools) {
    vscomntools = vs100comntools;
  }
  #elif (_MSC_VER == 1500) // VC80
  else if(vs90comntools) {
    vscomntools = vs90comntools;
  }
  #elif (_MSC_VER == 1400) // VC80
  else if(vs80comntools) {
    vscomntools = vs80comntools;
  }
  #endif
  // Otherwise find any version we can
  else if (vs120comntools)
      vscomntools = vs120comntools;
  else if (vs110comntools)
      vscomntools = vs110comntools;
  else if (vs100comntools)
    vscomntools = vs100comntools;
  else if (vs90comntools)
    vscomntools = vs90comntools;
  else if (vs80comntools)
    vscomntools = vs80comntools;

  if (vscomntools && *vscomntools) {
    const char *p = strstr(vscomntools, "\\Common7\\Tools");
    path = p ? std::string(vscomntools, p) : vscomntools;
    return true;
  }
  return false;
}

std::vector<std::string> GetWindowsSystemIncludeDirs() {
  std::vector<std::string> Paths;

  std::string VSDir;
  std::string WindowsSDKDir;

  // When built with access to the proper Windows APIs, try to actually find
  // the correct include paths first.
  if (getVisualStudioDir(VSDir)) {
    Paths.push_back(VSDir + "\\VC\\include");
    if (getWindowsSDKDir(WindowsSDKDir))
      Paths.push_back(WindowsSDKDir + "\\include");
    else
      Paths.push_back(VSDir + "\\VC\\PlatformSDK\\Include");
  }

  return Paths;
}

#endif
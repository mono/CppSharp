/* 
InjectModuleInitializer

Command line program to inject a module initializer into a .NET assembly.

Copyright (C) 2009-2016 Einar Egilsson
http://einaregilsson.com/module-initializers-in-csharp/

This program is licensed under the MIT license: http://opensource.org/licenses/MIT
 */
using System;

namespace EinarEgilsson.Utilities.InjectModuleInitializer
{
    internal static class Errors
    {
        internal static string AssemblyDoesNotExist(string assembly)
        {
            return String.Format("Assembly '{0}' does not exist", assembly);
        }

        internal static string NoModuleInitializerTypeFound()
        {
            return "Found no type named 'ModuleInitializer', this type must exist or the ModuleInitializer parameter must be used";
        }

        internal static string InvalidFormatForModuleInitializer()
        {
            return "Invalid format for ModuleInitializer parameter, use Full.Type.Name::MethodName";
        }
        
        internal static string TypeNameDoesNotExist(string typeName)
        {
            return string.Format("No type named '{0}' exists in the given assembly!", typeName);
        }

        internal static string MethodNameDoesNotExist(string typeName, string methodName)
        {
            return string.Format("No method named '{0}' exists in the type '{0}'", methodName, typeName);
        }

        internal static string KeyFileDoesNotExist(string keyfile)
        {
            return string.Format("The key file'{0}' does not exist", keyfile);
        }
        
        internal static string ModuleInitializerMayNotBePrivate()
        {
            return "Module initializer method may not be private or protected, use public or internal instead";
        }
        
        internal static string ModuleInitializerMustBeVoid()
        {
            return "Module initializer method must have 'void' as return type";
        }

        internal static string ModuleInitializerMayNotHaveParameters()
        {
            return "Module initializer method must not have any parameters";
        }

        internal static string ModuleInitializerMustBeStatic()
        {
            return "Module initializer method must be static";
        }
    }
}

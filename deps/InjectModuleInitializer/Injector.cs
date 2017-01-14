/* 
InjectModuleInitializer

Command line program to inject a module initializer into a .NET assembly.

Copyright (C) 2009-2016 Einar Egilsson
http://einaregilsson.com/module-initializers-in-csharp/

This program is licensed under the MIT license: http://opensource.org/licenses/MIT
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace EinarEgilsson.Utilities.InjectModuleInitializer
{
    public class InjectionException : Exception
    {
        public InjectionException(string msg) : base(msg) { }
    }

    internal class Injector
    {
        public const string DefaultInitializerClassName = "ModuleInitializer";
        public const string DefaultInitializerMethodName = "Run";

        private AssemblyDefinition Assembly { get; set; }

        private string PdbFile(string assemblyFile)
        {
            Debug.Assert(assemblyFile != null);
            string path = Path.ChangeExtension(assemblyFile, ".pdb");
            if (File.Exists(path))
            {
                return path;
            }
            return null;
        }


        public void Inject(string assemblyFile, string moduleInitializer=null, string keyfile=null)
        {
            try
            {
                if (!File.Exists(assemblyFile))
                {
                    throw new InjectionException(Errors.AssemblyDoesNotExist(assemblyFile));
                }
                if (keyfile != null && !File.Exists(keyfile))
                {
                    throw new InjectionException(Errors.KeyFileDoesNotExist(keyfile));
                }
                ReadAssembly(assemblyFile);
                MethodReference callee = GetModuleInitializer(moduleInitializer);
                InjectInitializer(callee);

                WriteAssembly(assemblyFile, keyfile);
            }
            catch (Exception ex)
            {
                throw new InjectionException(ex.Message);
            }
        }

        public void InjectInitializer(MethodReference callee)
        {
            Debug.Assert(Assembly != null);
            TypeReference voidRef = Assembly.MainModule.Import(callee.ReturnType);
            const MethodAttributes attributes = MethodAttributes.Static
                                                | MethodAttributes.SpecialName
                                                | MethodAttributes.RTSpecialName;
            var cctor = new MethodDefinition(".cctor", attributes, voidRef);
            ILProcessor il = cctor.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Call, callee));
            il.Append(il.Create(OpCodes.Ret));

            TypeDefinition moduleClass = Find(Assembly.MainModule.Types, t => t.Name == "<Module>");
            moduleClass.Methods.Add(cctor);

            Debug.Assert(moduleClass != null, "Found no module class!");
        }

        public void WriteAssembly(string assemblyFile, string keyfile)
        {
            Debug.Assert(Assembly != null);
            var writeParams = new WriterParameters();
            if (PdbFile(assemblyFile) != null)
            {
                writeParams.WriteSymbols = true;
                writeParams.SymbolWriterProvider = new PdbWriterProvider();
            }
            if (keyfile != null)
            {
                writeParams.StrongNameKeyPair = new StrongNameKeyPair(File.ReadAllBytes(keyfile));
            }
            Assembly.Write(assemblyFile, writeParams);
        }

        public void ReadAssembly(string assemblyFile)
        {
            Debug.Assert(Assembly == null);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyFile));

            var readParams = new ReaderParameters(ReadingMode.Immediate)
            {
                AssemblyResolver = resolver
            };
            
            if (PdbFile(assemblyFile) != null)
            {
                readParams.ReadSymbols = true;
                readParams.SymbolReaderProvider = new PdbReaderProvider();
            }
            Assembly = AssemblyDefinition.ReadAssembly(assemblyFile, readParams);
        }

        public MethodReference GetModuleInitializer(string moduleInitializer = null)
        {
            Debug.Assert(Assembly != null);
            ModuleDefinition module = Assembly.MainModule;
            string methodName;
            TypeDefinition moduleInitializerClass;
            if (string.IsNullOrEmpty(moduleInitializer))
            {
                methodName = DefaultInitializerMethodName;
                moduleInitializerClass = Find(module.Types, t => t.Name == DefaultInitializerClassName);
                if (moduleInitializerClass == null)
                {
                    return null;
                }
            }
            else
            {
                if (!moduleInitializer.Contains("::"))
                {
                    return null;
                }
                string typeName = moduleInitializer.Substring(0, moduleInitializer.IndexOf("::", StringComparison.Ordinal));
                methodName = moduleInitializer.Substring(typeName.Length + 2);
                moduleInitializerClass = Find(module.Types, t => t.FullName == typeName);
                if (moduleInitializerClass == null)
                {
                    return null;
                }
            }

            MethodDefinition callee = Find(moduleInitializerClass.Methods, m => m.Name == methodName);
            if (callee == null)
            {
                throw new InjectionException(Errors.MethodNameDoesNotExist(moduleInitializerClass.FullName, methodName));
            }
            if (callee.Parameters.Count > 0)
            {
                throw new InjectionException(Errors.ModuleInitializerMayNotHaveParameters());
            }
            if (callee.IsPrivate || callee.IsFamily)
            {
                throw new InjectionException(Errors.ModuleInitializerMayNotBePrivate());
            }
            if (!callee.ReturnType.FullName.Equals(typeof(void).FullName)) //Don't compare the types themselves, they might be from different CLR versions.
            {
                throw new InjectionException(Errors.ModuleInitializerMustBeVoid());
            }
            if (!callee.IsStatic)
            {
                throw new InjectionException(Errors.ModuleInitializerMustBeStatic());
            }
            return callee;
        }

        //No LINQ, since we want to target 2.0
        private static T Find<T>(Collection<T> objects, Predicate<T> condition) where T:class
        {
            foreach (T obj in objects)
            {
                if (condition(obj))
                {
                    return obj;
                }
            }
            return null;
        }
    }
}

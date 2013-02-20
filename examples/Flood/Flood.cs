using System.Collections.Generic;
using Cxxi.Generators;
using Cxxi.Generators.CLI;
using Cxxi.Passes;
using Cxxi.Types;

namespace Cxxi.Libraries
{
    /// <summary>
    /// Transform the Flood library declarations to something more .NET friendly.
    /// </summary>
    class Flood : ILibrary
    {
        public void Preprocess(Library lib)
        {
            lib.IgnoreModulessWithName("API.h");
            lib.IgnoreModulessWithName("Concurrency.h");
            lib.IgnoreModulessWithName("ConcurrentQueue.h");
            lib.IgnoreModulessWithName("Delegate.h");
            lib.IgnoreModulessWithName("Event.h");
            lib.IgnoreModulessWithName("Handle.h");
            lib.IgnoreModulessWithName("Object.h");
            lib.IgnoreModulessWithName("Pointers.h");
            lib.IgnoreModulessWithName("References.h");
            lib.IgnoreModulessWithName("ReflectionHelpers.h");

            //Core
            lib.SetClassAsValueType("StringHash");
            lib.IgnoreClassWithName("RawStringCompare");

            lib.IgnoreFunctionWithName("LogCreate");
            lib.SetClassAsValueType("LogEntry");
            lib.SetClassAsValueType("FileWatchEvent");
            lib.SetClassAsValueType("ExtensionMetadata");

            lib.IgnoreFunctionWithName("AllocatorAllocate");
            lib.IgnoreFunctionWithName("AllocatorDeallocate");
            lib.SetNameOfFunction("AllocatorReset", "AllocatorResetMemory");
            lib.IgnoreFunctionWithPattern(".+GetType");

            lib.SetClassAsOpaque("FileStream");

            lib.IgnoreClassWithName("StreamFuncs");
            lib.IgnoreFunctionWithName("ClassGetIdMap");
            lib.IgnoreFunctionWithName("ReflectionSetHandleContext");
            lib.IgnoreFunctionWithName("SerializerCreateJSON");
            lib.IgnoreFunctionWithName("SerializerCreateBinary");

            // Math
            lib.SetClassAsValueType("ColorP");
            lib.SetClassAsValueType("Color");
            lib.SetClassAsValueType("Vector2P");
            lib.SetClassAsValueType("Vector2");
            lib.SetClassAsValueType("Vector2i");
            lib.SetClassAsValueType("Vector3P");
            lib.SetClassAsValueType("Vector3");
            lib.SetClassAsValueType("Vector4");
            lib.SetClassAsValueType("EulerAngles");
            lib.SetClassAsValueType("QuaternionP");
            lib.SetClassAsValueType("Quaternion");
            lib.SetClassAsValueType("Matrix4x4");

            // Resources
            lib.IgnoreFunctionWithName("ResourcesInitialize");
            lib.IgnoreFunctionWithName("ResourcesDeinitialize");
            lib.SetClassAsValueType("ResourceEvent");
            lib.SetClassAsValueType("ResourceLoadOption");
            lib.SetClassAsValueType("ResourceLoadOptions");
            lib.SetNameOfClassMethod("Texture", "allocate", "alloc");

            // Engine
            lib.IgnoreClassMethodWithName("Engine", "addSubsystem");
        }

        public void Postprocess(Library lib)
        {
        }

        public void Setup(DriverOptions options)
        {
            options.LibraryName = "Engine";
            options.OutputNamespace = "Flood";
            options.OutputDir = @"C:\Development\flood2\src\EngineManaged\Bindings";
            options.IncludeDirs.Add(@"C:\Development\flood2\inc");
            options.GeneratorKind = LanguageGeneratorKind.CPlusPlusCLI;

            SetupHeaders(options.Headers);
        }

        public void SetupHeaders(List<string> headers)
        {
            var sources = new string[]
                {
                    "Core/Log.h",
                    "Core/Extension.h",
                    "Core/Reflection.h",
                    "Core/Serialization.h",
                    "Resources/Resource.h",
                    "Resources/ResourceLoader.h",
                    "Resources/ResourceManager.h",
                    "Graphics/Graphics.h",
                    "Graphics/RenderDevice.h",
                    "Graphics/RenderBatch.h",
                    "Graphics/Texture.h",
                    "Engine/Engine.h"
                };

            headers.AddRange(sources);
        }

        public void SetupPasses(PassBuilder p)
        {
            const RenameTargets renameTargets = RenameTargets.Function
                | RenameTargets.Method | RenameTargets.Field;
            p.RenameDeclsCase(renameTargets, RenameCasePattern.UpperCamelCase);

            p.FunctionToInstanceMethod();
            p.FunctionToStaticMethod();
            p.CheckDuplicateNames();
        }

        public void GenerateStart(TextTemplate template)
        {
            template.WriteLine("/************************************************************************");
            template.WriteLine("*");
            template.WriteLine("* Flood Project \u00A9 (2008-201x)");
            template.WriteLine("* Licensed under the simplified BSD license. All rights reserved.");
            template.WriteLine("*");
            template.WriteLine("************************************************************************/");
            template.NewLine();
            if (template is CLISourcesTemplate)
                template.WriteLine("#include \"_Marshal.h\"");
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
            if (template is CLISourcesTemplate)
                template.WriteLine("using namespace clix;");
        }
    }

    namespace Types.Flood
    {
        [TypeMap("RefPtr")]
        public class RefPtr : TypeMap
        {
            public override string Signature()
            {
                var type = Type as TemplateSpecializationType;
                return string.Format("{0}", type.Arguments[0].Type);
            }

            public override string MarshalToNative(MarshalContext ctx)
            {
                throw new System.NotImplementedException();
            }

            public override string MarshalFromNative(MarshalContext ctx)
            {
                return "nullptr";
            }
        }

        [TypeMap("ResourceHandle")]
        public class ResourceHandle : TypeMap
        {
            public override string Signature()
            {
                return "uint";
            }

            public override string MarshalToNative(MarshalContext ctx)
            {
                return string.Format("(HandleId){0}", ctx.Parameter.Name);
            }

            public override string MarshalFromNative(MarshalContext ctx)
            {
                return string.Format("{0}.id", ctx.ReturnVarName);
            }
        }

        [TypeMap("Path")]
        [TypeMap("String")]
        public class String : Cxxi.Types.Std.String
        {
        }

        [TypeMap("StringWide")]
        public class StringWide : Cxxi.Types.Std.WString
        {
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                Cxxi.Program.Run(new Libraries.Flood());
            }
        }
    }
}

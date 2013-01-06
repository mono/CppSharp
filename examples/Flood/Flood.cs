using Cxxi.Generators;
using Cxxi.Generators.CLI;
using Cxxi.Passes;
using Cxxi.Types;

namespace Cxxi.Libraries
{
    /// <summary>
    /// Transform the Flush library declarations to something more .NET friendly.
    /// </summary>
    class Flush : ILibrary
    {
        public void Preprocess(LibraryHelpers g)
        {
            g.IgnoreModulessWithName("API.h");
            g.IgnoreModulessWithName("Concurrency.h");
            g.IgnoreModulessWithName("ConcurrentQueue.h");
            g.IgnoreModulessWithName("Delegate.h");
            g.IgnoreModulessWithName("Event.h");
            g.IgnoreModulessWithName("Handle.h");
            //g.IgnoreModulessWithName("Memory.h");
            g.IgnoreModulessWithName("Object.h");
            g.IgnoreModulessWithName("Pointers.h");
            g.IgnoreModulessWithName("References.h");
            //g.IgnoreModulessWithName("Reflection.h");
            g.IgnoreModulessWithName("Serialization.h");

            //Core
            g.SetClassAsValueType("StringHash");
            g.IgnoreClassWithName("RawStringCompare");

            g.IgnoreFunctionWithName("LogCreate");
            g.SetClassAsValueType("LogEntry");
            g.SetClassAsValueType("FileWatchEvent");
            g.SetClassAsValueType("ExtensionMetadata");

            g.IgnoreClassWithName("StreamFuncs");

            // Math
            g.SetClassAsValueType("ColorP");
            g.SetClassAsValueType("Color");
            g.SetClassAsValueType("Vector2P");
            g.SetClassAsValueType("Vector2");
            g.SetClassAsValueType("Vector2i");
            g.SetClassAsValueType("Vector3P");
            g.SetClassAsValueType("Vector3");
            g.SetClassAsValueType("Vector4");
            g.SetClassAsValueType("EulerAngles");
            g.SetClassAsValueType("QuaternionP");
            g.SetClassAsValueType("Quaternion");

            // Resources
            g.IgnoreFunctionWithName("ResourcesInitialize");
            g.IgnoreFunctionWithName("ResourcesDeinitialize");
            g.SetClassAsValueType("ResourceEvent");
            g.SetClassAsValueType("ResourceLoadOption");
            g.SetClassAsValueType("ResourceLoadOptions");

            // Engine
            g.IgnoreClassMethodWithName("Engine", "addSubsystem");
        }

        public void Postprocess(LibraryHelpers generator)
        {
        }

        public void SetupPasses(PassBuilder p)
        {
            p.RenameDeclsCase(RenameTargets.Function | RenameTargets.Method | RenameTargets.Field,
                RenameCasePattern.UpperCamelCase);
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

    namespace Types.Flush
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
                return string.Format("(HandleId){0}", ctx.ReturnVarName);
            }
        }

        [TypeMap("Path")]
        [TypeMap("String")]
        [TypeMap("StringWide")]
        public class String : Cxxi.Types.Std.String
        {
        }
    }
}

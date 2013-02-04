using System;
using System.Collections.Generic;
using System.Linq;
using Cxxi.Generators;
using Cxxi.Types;

namespace Cxxi.Templates.CSharp
{
    class CSharpTypePrinter : ITypePrinter
    {
        public Library Library { get; set; }

        public CSharpTypePrinter()
        {
        }

        public string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            throw new NotImplementedException();

            // C# only supports fixed arrays in unsafe sections
            // and they are constrained to a set of built-in types.

            //return string.Format("{0}[]", Type);
        }

        public string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            throw new NotImplementedException();

            //string args = string.Empty;

            //if (Arguments.Count > 0)
            //    args = ToArgumentString(hasNames: false);

            //if (ReturnType.IsPrimitiveType(PrimitiveType.Void))
            //{
            //    if (!string.IsNullOrEmpty(args))
            //        args = string.Format("<{0}>", args);
            //    return string.Format("Action{0}", args);
            //}

            //if (!string.IsNullOrEmpty(args))
            //    args = string.Format(", {0}", args);

            //return string.Format("Func<{0}{1}>",
            //    ReturnType.ToCSharp(), args);
        }

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            throw new NotImplementedException();

            //if (Pointee is FunctionType)
            //{
            //    var function = Pointee as FunctionType;
            //    return function.ToCSharp();
            //}

            //if (Pointee is TagType)
            //    return Pointee.ToCSharp();

            //return "IntPtr";

            //return string.Format("{0}{1}",
            //  Pointee.ToCSharp(), ConvertModifierToString(Modifier));
        }

        public string VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string GetArgumentsString(FunctionType function, bool hasNames)
        {
            throw new NotImplementedException();
        }

        public string GetArgumentString(Parameter arg, bool hasName)
        {
            throw new NotImplementedException();

            //if (hasName && !string.IsNullOrEmpty(Name))
            //    return string.Format("{0} {1}", Type.ToCSharp(), Name);
            //else
            //    return Type.ToCSharp();
        }

        public string ToDelegateString(FunctionType function)
        {
            throw new NotImplementedException();
        }
    }

    public class CSharpModule : TextTemplate
    {
        // from https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
        private static string[] keywords = new string[]
        {
            "abstract","event","new","struct","as","explicit","null","switch","base","extern",
            "this","false","operator","throw","break","finally","out","true",
            "fixed","override","try","case","params","typeof","catch","for",
            "private","foreach","protected","checked","goto","public",
            "unchecked","class","if","readonly","unsafe","const","implicit","ref",
            "continue","in","return","using","virtual","default",
            "interface","sealed","volatile","delegate","internal","do","is",
            "sizeof","while","lock","stackalloc","else","static","enum",
            "namespace",
            "object","bool","byte","float","uint","char","ulong","ushort",
            "decimal","int","sbyte","short","double","long","string","void",
            "partial", "yield", "where"
        };

        public static string SafeIdentifier(string proposedName)
        {
            proposedName = new string(((IEnumerable<char>)proposedName).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return keywords.Contains(proposedName) ? "@" + proposedName : proposedName;
        }

        public override string FileExtension { get { return "cs"; } }

        protected override void Generate()
        {
            throw new NotImplementedException();
        }
    }
}

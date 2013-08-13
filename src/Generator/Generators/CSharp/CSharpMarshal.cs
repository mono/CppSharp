using System.Collections.Generic;
using System.Text;
using CppSharp.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CSharp
{
    public enum CSharpMarshalKind
    {
        Unknown,
        NativeField
    }

    public class CSharpMarshalContext : MarshalContext
    {
        public CSharpMarshalContext(Driver driver)
            : base(driver)
        {
            Kind = CSharpMarshalKind.Unknown;
            Cleanup = new TextGenerator();
        }

        public CSharpMarshalKind Kind { get; set; }

        public TextGenerator Cleanup { get; private set; }
    }

    public abstract class CSharpMarshalPrinter : MarshalPrinter
    {
        public CSharpMarshalContext CSharpContext
        {
            get { return Context as CSharpMarshalContext; }
        }

        protected CSharpMarshalPrinter(CSharpMarshalContext context)
            : base(context)
        {
            Options.VisitFunctionParameters = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }
    }

    public class CSharpMarshalNativeToManagedPrinter : CSharpMarshalPrinter
    {
        public CSharpMarshalNativeToManagedPrinter(CSharpMarshalContext context)
            : base(context)
        {
            Context.MarshalToManaged = this;
        }

        public static string QualifiedIdentifier(Declaration decl)
        {
            var names = new List<string> { decl.Name };

            var ctx = decl.Namespace;
            while (ctx != null)
            {
                if (!string.IsNullOrWhiteSpace(ctx.Name))
                    names.Add(ctx.Name);
                ctx = ctx.Namespace;
            }

            //if (Options.GenerateLibraryNamespace)
            //    names.Add(Options.OutputNamespace);

            names.Reverse();
            return string.Join(".", names);
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap))
            {
                typeMap.Type = type;
                typeMap.CSharpMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.CSharpMarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    Context.Return.Write("null");
                    break;
                case ArrayType.ArraySize.Variable:
                    Context.Return.Write("null");
                    break;
            }

            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var pointee = pointer.Pointee;

            if (CSharpTypePrinter.IsConstCharString(pointer))
            {
                Context.Return.Write("Marshal.PtrToStringAnsi({0})",
                    Context.ReturnVarName);
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive))
            {
                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            if (!pointee.Visit(this, quals))
                return false;

            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Context.Return.Write(Context.ReturnVarName);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            FunctionType function;
            if (decl.Type.IsPointerTo(out function))
            {
                var ptrName = Helpers.GeneratedIdentifier("ptr") +
                    Context.ParameterIndex;

                Context.SupportBefore.WriteLine("var {0} = {1};", ptrName,
                    Context.ReturnVarName);

                Context.Return.Write("({1})Marshal.GetDelegateForFunctionPointer({0}, typeof({1}))",
                    ptrName, typedef.ToString());
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitClassDecl(Class @class)
        {
            var ctx = Context as CSharpMarshalContext;

            string instance = Context.ReturnVarName;
            if (ctx.Kind == CSharpMarshalKind.NativeField)
            {
                instance = string.Format("new global::System.IntPtr(&{0})", instance);
            }

            Context.Return.Write("new {0}({1})", QualifiedIdentifier(@class),
                instance);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("{0}", Context.ReturnVarName);
            return true;
        }
    }

    public class CSharpMarshalManagedToNativePrinter : CSharpMarshalPrinter
    {
        public CSharpMarshalManagedToNativePrinter(CSharpMarshalContext context)
            : base(context)
        {
            Context.MarshalToNative = this;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap))
            {
                typeMap.Type = type;
                typeMap.CSharpMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            TypeMap typeMap;
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.CSharpMarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            Context.Return.Write("null");
            return true;
        }

        public bool VisitDelegateType(FunctionType function, string type)
        {
            Context.Return.Write("Marshal.GetFunctionPointerForDelegate({0})",
                Context.Parameter.Name);

            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var pointee = pointer.Pointee;

            Type type = pointee.Desugar();
            if (type.IsPrimitiveType(PrimitiveType.Char) ||
                type.IsPrimitiveType(PrimitiveType.WideChar))
            {
                Context.Return.Write("Marshal.StringToHGlobalAnsi({0})",
                    Context.Parameter.Name);
                CSharpContext.Cleanup.WriteLine("Marshal.FreeHGlobal({0});",
                    Context.ArgName);
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return VisitDelegateType(function, function.ToString());
            }

            Class @class;
            if (type.IsTagDecl(out @class) && @class.IsValueType)
            {
                if (Context.Parameter.Usage == ParameterUsage.Out)
                {
                    Context.SupportBefore.WriteLine("var {0} = new {1}.Internal();",
                        Helpers.GeneratedIdentifier(Context.ArgName), @class.Name);
                }
                else
                {
                    Context.SupportBefore.WriteLine("var {0} = {1}.ToInternal();",
                            Helpers.GeneratedIdentifier(Context.ArgName),
                            Helpers.SafeIdentifier(Context.Parameter.Name));
                }

                Context.Return.Write("new global::System.IntPtr(&{0})",
                    Helpers.GeneratedIdentifier(Context.ArgName));
                return true;
            }

            PrimitiveType primitive;
            if (type.IsPrimitiveType(out primitive))
            {
                Context.Return.Write(Context.Parameter.Name);
                return true;
            }

            return pointee.Visit(this, quals);
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Context.Return.Write(Context.Parameter.Name);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                VisitDelegateType(func, typedef.Declaration.OriginalName);
                return true;
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            Context.Return.Write(param.Parameter.Name);
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsValueType)
            {
                MarshalValueClass(@class);
            }
            else
            {
                MarshalRefClass(@class);
            }

            return true;
        }

        private void MarshalRefClass(Class @class)
        {
            var method = Context.Function as Method;
            if (method != null
                && method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                && Context.ParameterIndex == 0)
            {
                Context.Return.Write("{0}", Helpers.InstanceIdentifier);
                return;
            }

            if (Context.Parameter.Type.IsPointer() || Context.Parameter.Type.IsReference())
            {
                Context.Return.Write("{0}.{1}", Helpers.SafeIdentifier(Context.Parameter.Name),
                    Helpers.InstanceIdentifier);
                return;
            }

            Context.Return.Write("*({0}.Internal*){1}.{2}", @class.Name,
                Helpers.SafeIdentifier(Context.Parameter.Name), Helpers.InstanceIdentifier);

        }

        private void MarshalValueClass(Class @class)
        {
            Context.Return.Write("{0}.ToInternal()", Helpers.SafeIdentifier(Context.Parameter.Name));
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = field.QualifiedType
            };

            return field.Type.Visit(this, field.QualifiedType.Qualifiers);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.Parameter.Name);
            return true;
        }
    }
}

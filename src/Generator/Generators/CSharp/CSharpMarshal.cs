using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

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
            ArgumentPrefix = new TextGenerator();
            Cleanup = new TextGenerator();
        }

        public CSharpMarshalKind Kind { get; set; }
        public QualifiedType FullType;

        public TextGenerator ArgumentPrefix { get; private set; }
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

        public int VarSuffix { get; set; }

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
                    var supportBefore = Context.SupportBefore;
                    string value = Generator.GeneratedIdentifier("value");
                    supportBefore.WriteLine("{0}[] {1} = new {0}[{2}];", array.Type, value, array.Size);
                    string v = Generator.GeneratedIdentifier("v");
                    supportBefore.WriteLine("fixed ({0}* {1} = {2})", array.Type, v, value);
                    supportBefore.WriteStartBraceIndent();
                    string to = Generator.GeneratedIdentifier("to");
                    supportBefore.WriteLine("{0}* {1} = {2};", array.Type, to, v);
                    string from = Generator.GeneratedIdentifier("from");
                    supportBefore.WriteLine("{0}* {1} = {2}->{3};",
                        array.Type, from, Generator.GeneratedIdentifier("ptr"), Context.ArgName);
                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    supportBefore.WriteLineIndent("*{0}++ = *{1}++;", to, from);
                    supportBefore.WriteCloseBraceIndent();
                    Context.Return.Write(value);
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
                Context.Return.Write(MarshalStringToManaged(Context.ReturnVarName));
                return true;
            }

            PrimitiveType primitive;
            if (pointee.Desugar().IsPrimitiveType(out primitive))
            {
                var param = Context.Parameter;
                if (param != null && (param.IsOut || param.IsInOut))
                {
                    Context.Return.Write("_{0}", param.Name);
                    return true;
                }

                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            if (!pointee.Visit(this, quals))
                return false;

            return true;
        }

        private string MarshalStringToManaged(string varName)
        {
            if (Equals(Context.Driver.Options.Encoding, Encoding.ASCII))
            {
                return string.Format("Marshal.PtrToStringAnsi({0})", varName);
            }
            if (Equals(Context.Driver.Options.Encoding, Encoding.Unicode) ||
                Equals(Context.Driver.Options.Encoding, Encoding.BigEndianUnicode))
            {
                return string.Format("Marshal.PtrToStringUni({0})", varName);
            }
            throw new NotSupportedException(string.Format("{0} is not supported yet.",
                Context.Driver.Options.Encoding.EncodingName));
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

            throw new NotImplementedException();
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            var decl = typedef.Declaration;

            FunctionType function;
            if (decl.Type.IsPointerTo(out function))
            {
                var ptrName = Generator.GeneratedIdentifier("ptr") +
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
                string copy = Generator.GeneratedIdentifier("copy");
                if (VarSuffix > 0)
                    copy += VarSuffix;
                Context.SupportBefore.WriteLine(
                    "var {0} = new global::System.IntPtr(&{1});", copy, instance);
                instance = copy;
            }

            if (@class.IsRefType &&
                (Context.ReturnType.Qualifiers.IsConst || !Context.ReturnType.Type.IsAddress()) &&
                (!Context.Driver.Options.GenerateAbstractImpls || !@class.IsAbstract))
            {
                var instanceName = Generator.GeneratedIdentifier("instance");
                if (VarSuffix > 0)
                    instanceName += VarSuffix;

                // Allocate memory for a new native object and call the ctor.
                Context.SupportBefore.WriteLine("var {0} = Marshal.AllocHGlobal({1});",
                    instanceName, @class.Layout.Size);

                if (@class.HasNonTrivialCopyConstructor)
                {
                    // Find a valid copy constructor overload.
                    var copyCtorMethod = @class.Methods.FirstOrDefault(method =>
                        method.IsCopyConstructor);

                    if (copyCtorMethod == null)
                        throw new NotSupportedException("Expected a valid copy constructor");

                    // Call the copy constructor.
                    Context.SupportBefore.WriteLine("{0}.Internal.{1}({2}, new global::System.IntPtr(&{3}));",
                        QualifiedIdentifier(@class),
                        CSharpTextTemplate.GetFunctionNativeIdentifier(copyCtorMethod),
                        instanceName, instance);
                }
                else
                {
                    instance = instance.Trim('*');
                    Context.SupportBefore.WriteLine(
                        "CppSharp.Runtime.Helpers.memcpy({0}, new IntPtr(&{1}), new UIntPtr({2}));",
                        instanceName, instance, @class.Layout.Size);
                }

                instance = instanceName;
            }

            Context.Return.Write("new {0}({1})",
                QualifiedIdentifier(@class.OriginalClass ?? @class) +
                (Context.Driver.Options.GenerateAbstractImpls && @class.IsAbstract ?
                    "Internal" : ""),
                instance);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write("{0}", Context.ReturnVarName);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (parameter.Usage == ParameterUsage.Unknown || parameter.IsIn)
                return base.VisitParameterDecl(parameter);

            var ctx = new CSharpMarshalContext(base.Context.Driver)
            {
                ReturnType = base.Context.ReturnType,
                ReturnVarName = base.Context.ReturnVarName
            };

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            parameter.Type.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(ctx.SupportBefore))
                Context.SupportBefore.WriteLine(ctx.SupportBefore);

            if (!string.IsNullOrWhiteSpace(ctx.Return))
            {
                Context.SupportBefore.WriteLine("var _{0} = {1};", parameter.Name,
                    ctx.Return);
            }

            Context.Return.Write("_{0}", parameter.Name);
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

            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    var supportBefore = Context.SupportBefore;
                    supportBefore.WriteLine("if ({0}.Length != {1})",
                        Context.ArgName, array.Size);
                    supportBefore.WriteLineIndent(
                        "throw new ArgumentException(\"The value must be an array of size {0}.\");",
                        array.Size);
                    string v = Generator.GeneratedIdentifier("v");
                    supportBefore.WriteLine("fixed ({0}* {1} = {2})", array.Type, v, Context.ArgName);
                    supportBefore.WriteLineIndent("for (int i = 0; i < {0}; i++)", array.Size);
                    supportBefore.PushIndent();
                    supportBefore.WriteLineIndent("*({0}->{1} + i) = *({2} + i);",
                        Generator.GeneratedIdentifier("ptr"), Context.ReturnVarName, v);
                    supportBefore.PopIndent();
                    supportBefore.WriteLine("fixed ({0}* {1} = {2})", array.Type, v, Context.ArgName);
                    supportBefore.PushIndent();
                    supportBefore.Write("*");
                    Context.Return.Write("*{0}", v);
                    break;
                default:
                    Context.Return.Write("null");
                    break;
            }
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
            if ((type.IsPrimitiveType(PrimitiveType.Char) ||
                type.IsPrimitiveType(PrimitiveType.WideChar)) &&
                pointer.QualifiedPointee.Qualifiers.IsConst)
            {
                Context.Return.Write(MarshalStringToUnmanaged(
                    Helpers.SafeIdentifier(Context.Parameter.Name)));
                CSharpContext.Cleanup.WriteLine("Marshal.FreeHGlobal({0});",
                    Helpers.SafeIdentifier(Context.ArgName));
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
                        Generator.GeneratedIdentifier(Context.ArgName), @class.Name);
                }
                else
                {
                    Context.SupportBefore.WriteLine("var {0} = {1}.ToInternal();",
                            Generator.GeneratedIdentifier(Context.ArgName),
                            Helpers.SafeIdentifier(Context.Parameter.Name));
                }

                Context.Return.Write("new global::System.IntPtr(&{0})",
                    Generator.GeneratedIdentifier(Context.ArgName));
                return true;
            }

            PrimitiveType primitive;
            if (type.IsPrimitiveType(out primitive))
            {
                var param = Context.Parameter;

                // From MSDN: "note that a ref or out parameter is classified as a moveable
                // variable". This means we must create a local variable to hold the result
                // and then assign this value to the parameter.

                if (param.IsOut || param.IsInOut)
                {
                    var typeName = Type.TypePrinterDelegate(type);

                    Context.SupportBefore.WriteLine("{0} _{1};", typeName,
                        Helpers.SafeIdentifier(param.Name));
                    Context.Return.Write("&_{0}", param.Name);

                }
                else
                    Context.Return.Write(Context.Parameter.Name);

                return true;
            }

            return pointee.Visit(this, quals);
        }

        private string MarshalStringToUnmanaged(string varName)
        {
            if (Equals(Context.Driver.Options.Encoding, Encoding.ASCII))
            {
                return string.Format("Marshal.StringToHGlobalAnsi({0})", varName);
            }
            if (Equals(Context.Driver.Options.Encoding, Encoding.Unicode) ||
                Equals(Context.Driver.Options.Encoding, Encoding.BigEndianUnicode))
            {
                return string.Format("Marshal.StringToHGlobalUni({0})", varName);
            }
            throw new NotSupportedException(string.Format("{0} is not supported yet.",
                Context.Driver.Options.Encoding.EncodingName));
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

            string param = Helpers.SafeIdentifier(Context.Parameter.Name);
            Type type = Context.Parameter.Type.Desugar();
            if (type.IsAddress())
            {
                Class decl;
                if (type.IsTagDecl(out decl) && decl.IsValueType)
                    Context.Return.Write("{0}.{1}", param, Helpers.InstanceIdentifier);
                else
                    Context.Return.Write("{0} == ({2}) null ? global::System.IntPtr.Zero : {0}.{1}", param,
                        Helpers.InstanceIdentifier, type);
                return;
            }

            var qualifiedIdentifier = CSharpMarshalNativeToManagedPrinter.QualifiedIdentifier(
                @class.OriginalClass ?? @class);
            Context.Return.Write(
                "{1} == ({0}) null ? new {0}.Internal() : *({0}.Internal*) ({1}.{2})", qualifiedIdentifier,
                param, Helpers.InstanceIdentifier);
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

    public static class CSharpMarshalExtensions
    {
        public static void CSharpMarshalToNative(this QualifiedType type,
            CSharpMarshalManagedToNativePrinter printer)
        {
            (printer.Context as CSharpMarshalContext).FullType = type;
            type.Visit(printer);
        }

        public static void CSharpMarshalToNative(this Type type,
            CSharpMarshalManagedToNativePrinter printer)
        {
            CSharpMarshalToNative(new QualifiedType(type), printer);
        }

        public static void CSharpMarshalToNative(this ITypedDecl decl,
            CSharpMarshalManagedToNativePrinter printer)
        {
            CSharpMarshalToNative(decl.QualifiedType, printer);
        }

        public static void CSharpMarshalToManaged(this QualifiedType type,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            (printer.Context as CSharpMarshalContext).FullType = type;
            type.Visit(printer);
        }

        public static void CSharpMarshalToManaged(this Type type,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            CSharpMarshalToManaged(new QualifiedType(type), printer);
        }

        public static void CSharpMarshalToManaged(this ITypedDecl decl,
            CSharpMarshalNativeToManagedPrinter printer)
        {
            CSharpMarshalToManaged(decl.QualifiedType, printer);
        }
    }
}

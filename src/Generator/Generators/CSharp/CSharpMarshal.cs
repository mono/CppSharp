using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
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
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
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
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
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
                    supportBefore.WriteLine("{0}[] {1} = null;", array.Type, value, array.Size);
                    supportBefore.WriteLine("if ({0} != null)", Context.ReturnVarName);
                    supportBefore.WriteStartBraceIndent();
                    supportBefore.WriteLine("{0} = new {1}[{2}];", value, array.Type, array.Size);
                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    if (array.Type.IsPointerToPrimitiveType(PrimitiveType.Void))
                        supportBefore.WriteLineIndent("{0}[i] = new global::System.IntPtr({1}[i]);",
                            value, Context.ReturnVarName);
                    else
                        supportBefore.WriteLineIndent("{0}[i] = {1}[i];", value, Context.ReturnVarName);
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

            var pointee = pointer.Pointee.Desugar();

            if (CSharpTypePrinter.IsConstCharString(pointer))
            {
                Context.Return.Write(MarshalStringToManaged(Context.ReturnVarName,
                    pointer.Pointee.Desugar() as BuiltinType));
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive) || pointee.IsEnumType())
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

            return pointer.Pointee.Visit(this, quals);
        }

        private string MarshalStringToManaged(string varName, BuiltinType type)
        {
            var encoding = type.Type == PrimitiveType.Char ?
                Encoding.ASCII : Encoding.Unicode;

            if (Equals(encoding, Encoding.ASCII))
                encoding = Context.Driver.Options.Encoding;

            if (Equals(encoding, Encoding.ASCII))
                return string.Format("Marshal.PtrToStringAnsi({0})", varName);

            if (Equals(encoding, Encoding.Unicode) ||
                Equals(encoding, Encoding.BigEndianUnicode))
                return string.Format("Marshal.PtrToStringUni({0})", varName);

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
                case PrimitiveType.Char:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.WideChar:
                case PrimitiveType.Null:
                    Context.Return.Write(Context.ReturnVarName);
                    return true;
                case PrimitiveType.Char16:
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

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var ptrName = Generator.GeneratedIdentifier("ptr") + Context.ParameterIndex;

            Context.SupportBefore.WriteLine("var {0} = {1};", ptrName,
                Context.ReturnVarName);

            Context.Return.Write("({1})Marshal.GetDelegateForFunctionPointer({0}, typeof({1}))",
                ptrName, function.ToString());
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            var instance = Context.ReturnVarName;

            @class = @class.OriginalClass ?? @class;
            Type returnType = Context.ReturnType.Type.Desugar();

            var type = QualifiedIdentifier(@class) +
                (Context.Driver.Options.GenerateAbstractImpls && @class.IsAbstract ?
                    "Internal" : "");

            if (returnType.IsAddress())
                Context.Return.Write("({0} == IntPtr.Zero) ? {1} : {2}.{3}({0})", instance,
                    @class.IsRefType ? "null" : string.Format("new {0}()", type),
                    type, Helpers.CreateInstanceIdentifier);
            else
                Context.Return.Write("{0}.{1}({2})", type, Helpers.CreateInstanceIdentifier, instance);

            return true;
        }

        private static bool FindTypeMap(ITypeMapDatabase typeMapDatabase,
            Class @class, out TypeMap typeMap)
        {
            return typeMapDatabase.FindTypeMap(@class, out typeMap) ||
                   (@class.HasBase && FindTypeMap(typeMapDatabase, @class.Bases[0].Class, out typeMap));
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
            if (Context.Driver.TypeDatabase.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
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
            if (Context.Driver.TypeDatabase.FindTypeMap(decl, out typeMap) && typeMap.DoesMarshalling)
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
                    supportBefore.WriteLine("if ({0} != null)", Context.ArgName);
                    supportBefore.WriteStartBraceIndent();
                    supportBefore.WriteLine("for (int i = 0; i < {0}; i++)", array.Size);
                    supportBefore.WriteLineIndent("{0}[i] = {1}[i]{2};",
                        Context.ReturnVarName, Context.ArgName,
                        array.Type.IsPointerToPrimitiveType(PrimitiveType.Void) ? ".ToPointer()" : string.Empty);
                    supportBefore.WriteCloseBraceIndent();
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

            var pointee = pointer.Pointee.Desugar();

            if ((pointee.IsPrimitiveType(PrimitiveType.Char) ||
                pointee.IsPrimitiveType(PrimitiveType.WideChar)) &&
                pointer.QualifiedPointee.Qualifiers.IsConst)
            {
                if (Context.Parameter.IsOut)
                {
                    Context.Return.Write("IntPtr.Zero");
                    CSharpContext.ArgumentPrefix.Write("&");
                }
                else if (Context.Parameter.IsInOut)
                {
                    Context.Return.Write(MarshalStringToUnmanaged(Context.Parameter.Name));
                    CSharpContext.ArgumentPrefix.Write("&");
                }
                else
                {
                    Context.Return.Write(MarshalStringToUnmanaged(Context.Parameter.Name));
                    CSharpContext.Cleanup.WriteLine("Marshal.FreeHGlobal({0});", Context.ArgName);
                }
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return VisitDelegateType(function, function.ToString());
            }

            Class @class;
            if (pointee.TryGetClass(out @class) && @class.IsValueType)
            {
                if (Context.Parameter.Usage == ParameterUsage.Out)
                {
                    Context.SupportBefore.WriteLine("var {0} = new {1}.Internal();",
                        Generator.GeneratedIdentifier(Context.ArgName), @class.Name);
                }
                else
                {
                    Context.SupportBefore.WriteLine("var {0} = {1}.{2};",
                            Generator.GeneratedIdentifier(Context.ArgName),
                            Context.Parameter.Name,
                            Helpers.InstanceIdentifier);
                }

                Context.Return.Write("new global::System.IntPtr(&{0})",
                    Generator.GeneratedIdentifier(Context.ArgName));
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive) || pointee.IsEnumType())
            {
                var param = Context.Parameter;

                // From MSDN: "note that a ref or out parameter is classified as a moveable
                // variable". This means we must create a local variable to hold the result
                // and then assign this value to the parameter.

                if (param.IsOut || param.IsInOut)
                {
                    var typeName = Type.TypePrinterDelegate(pointee);

                    if (param.IsInOut)
                        Context.SupportBefore.WriteLine("{0} _{1} = {1};", typeName, param.Name);
                    else
                        Context.SupportBefore.WriteLine("{0} _{1};", typeName, param.Name);

                    Context.Return.Write("&_{0}", param.Name);
                }
                else
                    Context.Return.Write(Context.Parameter.Name);

                return true;
            }

            return pointer.Pointee.Visit(this, quals);
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
                case PrimitiveType.Char:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.WideChar:
                    Context.Return.Write(Helpers.SafeIdentifier(Context.Parameter.Name));
                    return true;
                case PrimitiveType.Char16:
                    return false;
            }

            throw new NotImplementedException();
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
                MarshalValueClass();
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

            string param = Context.Parameter.Name;
            Type type = Context.Parameter.Type.Desugar();
            if (type.IsAddress())
            {
                Class decl;
                if (type.TryGetClass(out decl) && decl.IsValueType)
                    Context.Return.Write("{0}.{1}", param, Helpers.InstanceIdentifier);
                else
                    Context.Return.Write("{0}{1}.{2}",
                        method != null && method.OperatorKind == CXXOperatorKind.EqualEqual
                            ? string.Empty
                            : string.Format("ReferenceEquals({0}, null) ? global::System.IntPtr.Zero : ", param),
                        param,
                        Helpers.InstanceIdentifier, type);
                return;
            }

            var qualifiedIdentifier = CSharpMarshalNativeToManagedPrinter.QualifiedIdentifier(
                @class.OriginalClass ?? @class);
            Context.Return.Write(
                "ReferenceEquals({0}, null) ? new {1}.Internal() : *({1}.Internal*) ({0}.{2})", param,
                qualifiedIdentifier, Helpers.InstanceIdentifier);
        }

        private void MarshalValueClass()
        {
            Context.Return.Write("{0}.{1}", Context.Parameter.Name, Helpers.InstanceIdentifier);
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

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = property.QualifiedType
            };

            return base.VisitProperty(property);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Helpers.SafeIdentifier(Context.Parameter.Name));
            return true;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitClassDecl(template.TemplatedClass);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
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

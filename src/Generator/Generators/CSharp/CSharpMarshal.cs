using System.Collections.Generic;
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

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
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

            if (pointee.Desugar().IsPrimitiveType(PrimitiveType.Void))
            {
                Context.Return.Write("new System.IntPtr({0})", Context.ReturnVarName);
                return true;
            }

            if (CSharpTypePrinter.IsConstCharString(pointer))
            {
                Context.Return.Write("Marshal.PtrToStringAnsi(new System.IntPtr({0}))",
                    Context.ReturnVarName);
                return true;
            }

            PrimitiveType primitive;
            if (pointee.Desugar().IsPrimitiveType(out primitive))
            {
                Context.Return.Write("new System.IntPtr({0})", Context.ReturnVarName);
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
                Context.SupportBefore.WriteLine("var {0} = new System.IntPtr({1});",
                    Helpers.GeneratedIdentifier("ptr"), Context.ReturnVarName);

                Context.Return.Write("({1})Marshal.GetDelegateForFunctionPointer({0}, typeof({1}))",
                    Helpers.GeneratedIdentifier("ptr"), typedef.ToString());
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
                if (Context.ReturnVarName.StartsWith("*"))
                    Context.ReturnVarName = Context.ReturnVarName.Substring(1);

                instance = string.Format("new System.IntPtr({0})",
                    Context.ReturnVarName);
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

        public bool VisitDelegateType(FunctionType function, string type)
        {
            // We marshal function pointer types by calling
            // GetFunctionPointerForDelegate to get a native function
            // pointer ouf of the delegate. Then we can pass it in the
            // native call. Since references are not tracked in the
            // native side, we need to be careful and protect it with an
            // explicit GCHandle if necessary.

            //var sb = new StringBuilder();
            //sb.AppendFormat("({0})(", type);
            //sb.Append("Marshal.GetFunctionPointerForDelegate(");
            //sb.AppendFormat("{0}).ToPointer())", Context.Parameter.Name);
            //Context.Return.Write(sb.ToString());

            Context.Return.Write(Context.Parameter.Name);

            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            var pointee = pointer.Pointee;

            if (pointee.IsPrimitiveType(PrimitiveType.Char) ||
                pointee.IsPrimitiveType(PrimitiveType.WideChar))
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
                // TODO: We have to translate the function type typedef to C/C++
                return VisitDelegateType(function, function.ToString());
            }

            Class @class;
            if (pointee.IsTagDecl(out @class))
            {
                if (@class.IsRefType)
                    Context.Return.Write("{0}.Instance",
                        Helpers.SafeIdentifier(Context.Parameter.Name));
                else
                    Context.Return.Write("new System.IntPtr(&{0})",
                        Context.Parameter.Name);
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive))
            {
                Context.Return.Write("{0}.ToPointer()", Context.Parameter.Name);
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

            if (Context.Parameter.Type.IsPointer())
                Context.Return.Write("{0}.{1}", Context.Parameter.Name,
                    Helpers.InstanceIdentifier);
            else
                Context.Return.Write("{0}", Context.Parameter.Name);
        }

        private void MarshalValueClass(Class @class)
        {

            var marshalVar = "_marshal" + Context.ParameterIndex++;

            Context.SupportBefore.WriteLine("auto {0} = ::{1}();", marshalVar,
                @class.QualifiedOriginalName);

            MarshalValueClassFields(@class, marshalVar);

            Context.Return.Write(marshalVar);
        }

        private void MarshalValueClassFields(Class @class, string marshalVar)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore)
                    continue;

                var baseClass = @base.Class;
                MarshalValueClassFields(baseClass, marshalVar);
            }

            foreach (var field in @class.Fields)
            {
                if (field.Ignore)
                    continue;

                MarshalValueClassField(field, marshalVar);
            }
        }

        private void MarshalValueClassField(Field field, string marshalVar)
        {
            var fieldRef = string.Format("{0}.{1}", Context.Parameter.Name,
                                         field.Name);

            var marshalCtx = new CSharpMarshalContext(Context.Driver)
            {
                ArgName = fieldRef,
                ParameterIndex = Context.ParameterIndex++
            };

            var marshal = new CSharpMarshalManagedToNativePrinter(marshalCtx);
            field.Visit(marshal);

            Context.ParameterIndex = marshalCtx.ParameterIndex;

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Context.SupportBefore.Write(marshal.Context.SupportBefore);

            if (field.Type.IsPointer())
            {
                Context.SupportBefore.WriteLine("if ({0} != nullptr)", fieldRef);
                Context.SupportBefore.PushIndent();
            }

            Context.SupportBefore.WriteLine("{0}.{1} = {2};", marshalVar,
                field.OriginalName, marshal.Context.Return);

            if (field.Type.IsPointer())
                Context.SupportBefore.PopIndent();
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

        public override bool VisitParameterDecl(Parameter parameter)
        {
            var paramType = parameter.Type;

            Class @class;
            if (paramType.Desugar().IsTagDecl(out @class))
            {
                if (@class.IsRefType)
                {
                    Context.Return.Write(
                        "*({0}.Internal*){1}.Instance.ToPointer()",
                        @class.Name, parameter.Name);
                    return true;
                }
                else
                {
                    if (parameter.Kind == ParameterKind.OperatorParameter)
                    {
                        Context.Return.Write("new System.IntPtr(&{0})",
                            parameter.Name);
                        return true;
                    }

                    Context.Return.Write("*({0}.Internal*)&{1}",
                        @class.Name, parameter.Name);
                    return true;
                }
            }

            return paramType.Visit(this, parameter.QualifiedType.Qualifiers);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.Return.Write(Context.Parameter.Name);
            return true;
        }
    }
}

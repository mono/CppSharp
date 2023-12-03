using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.Cpp;
using CppSharp.Types;
using Delegate = CppSharp.AST.Delegate;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.NAPI
{
    public class NAPIMarshalNativeToManagedPrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public NAPIMarshalNativeToManagedPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }

        public string MemoryAllocOperator =>
            (Context.Context.Options.GeneratorKind == GeneratorKind.CLI) ?
                "gcnew" : "new";

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.MarshalToManaged(Context);
                return false;
            }

            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                case ArrayType.ArraySize.Incomplete:
                case ArrayType.ArraySize.Variable:
                    Context.Return.Write("nullptr");
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            Context.Return.Write(Context.ReturnVarName);
            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            if (pointer.IsConstCharString())
                return VisitPrimitiveType(PrimitiveType.String);

            var pointee = pointer.Pointee.Desugar();

            return pointer.QualifiedPointee.Visit(this);
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public static (string type, string func) GetNAPIPrimitiveType(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Bool:
                    return ("napi_boolean", "napi_get_boolean");
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Short:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    return ("napi_number", "napi_create_int32");
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                    return ("napi_number", "napi_create_uint32");
                case PrimitiveType.LongLong:
                    return ("napi_number", "napi_create_bigint_int64");
                case PrimitiveType.ULongLong:
                    return ("napi_number", "napi_create_bigint_uint64");
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    return ("napi_number", "napi_create_double");
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                    return ("napi_bigint", "napi_create_bigint_words");
                case PrimitiveType.String:
                    return ("napi_string", "napi_create_string_latin1");
                case PrimitiveType.Null:
                    return ("napi_null", "napi_get_null");
                case PrimitiveType.Void:
                    return ("napi_undefined", "napi_get_undefined");
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Decimal:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var result = Generator.GeneratedIdentifier(Context.ReturnVarName);
            var (_, func) = GetNAPIPrimitiveType(primitive);

            switch (primitive)
            {
                case PrimitiveType.Bool:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Short:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    Context.Before.WriteLine($"napi_value {result};");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}, &{result});");
                    Context.Before.WriteLine("assert(status == napi_ok);");
                    Context.Return.Write(result);
                    return true;

                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                    return true;

                case PrimitiveType.String:
                    Context.Before.WriteLine($"napi_value {result};");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}, NAPI_AUTO_LENGTH, &{result});");
                    Context.Before.WriteLine("assert(status == napi_ok);");
                    Context.Return.Write(result);
                    return true;

                case PrimitiveType.Null:
                    Context.Before.WriteLine($"napi_value {result};");
                    Context.Before.WriteLine($"status = {func}(env, &{result});");
                    Context.Before.WriteLine("assert(status == napi_ok);");
                    Context.Return.Write(result);
                    return true;

                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Decimal:
                default:
                    throw new NotImplementedException();
            }
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.Type = typedef;
                typeMap.MarshalToManaged(Context);
                return typeMap.IsValueType;
            }

            if (decl.Type.IsPointerTo(out FunctionType _))
            {
                var typeName = typePrinter.VisitDeclaration(decl);
                Context.Return.Write(typeName);
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = template;
                typeMap.MarshalToManaged(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            var ctor = @class.Constructors.FirstOrDefault();
            var ctorRef = $"ctor_{NAPISources.GetCIdentifier(Context.Context, ctor)}";
            var ctorId = $"__{Context.ReturnVarName}_ctor";
            Context.Before.WriteLine($"napi_value {ctorId};");
            Context.Before.WriteLine($"status = napi_get_reference_value(env, {ctorRef}, &{ctorId});");
            Context.Before.WriteLine("assert(status == napi_ok);");
            Context.Before.NewLine();

            var instanceId = $"__{Context.ReturnVarName}_instance";
            Context.Before.WriteLine($"napi_value {instanceId};");
            Context.Before.WriteLine($"status = napi_new_instance(env, {ctorId}, 0, nullptr, &{instanceId});");
            Context.Before.WriteLine("assert(status == napi_ok);");
            Context.Before.NewLine();

            /*
                        var refId = $"__{Context.ReturnVarName}_ref";
                        Context.Before.WriteLine($"napi_ref {refId};");

                        var dtorId = $"dtor_{NAPISources.GetCIdentifier(Context.Context, ctor)}";
                        Context.Before.WriteLine($"status = napi_wrap(env, _this, {instanceId}, {dtorId}" +
                                                 $", nullptr, &{refId});");
                        Context.Before.WriteLine("assert(status == napi_ok);");
            */
            Context.Return.Write($"{instanceId}");

            return true;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (!string.IsNullOrEmpty(decl.TranslationUnit.Module.OutputNamespace))
                return $"{decl.TranslationUnit.Module.OutputNamespace}::{decl.QualifiedName}";

            return decl.QualifiedName;
        }

        public override bool VisitFieldDecl(Field field)
        {
            return field.Type.Visit(this);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public override bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            Context.Parameter = parameter;
            var ret = parameter.Type.Visit(this, parameter.QualifiedType.Qualifiers);
            Context.Parameter = null;

            return ret;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Context.ArgName = $"(int32_t) {Context.ArgName}";
            VisitPrimitiveType(PrimitiveType.Int);

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            return variable.Type.Visit(this, variable.QualifiedType.Qualifiers);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }
    }

    public class NAPIMarshalManagedToNativePrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public NAPIMarshalManagedToNativePrinter(MarshalContext ctx)
            : base(ctx)
        {
            Context.MarshalToNative = this;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (!VisitType(tag, quals))
                return false;

            return tag.Declaration.Visit(this);
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            switch (array.SizeType)
            {
                default:
                    Context.Return.Write("nullptr");
                    break;
            }

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this);
        }

        public bool VisitDelegateType(string type)
        {
            Context.Return.Write(Context.Parameter.Name);
            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            if (pointer.IsConstCharString())
                return VisitPrimitiveType(PrimitiveType.String);

            var pointee = pointer.Pointee.Desugar();

            if (pointee is FunctionType)
            {
                typePrinter.PushContext(TypePrinterContextKind.Managed);
                var cppTypeName = pointer.Visit(typePrinter, quals);
                typePrinter.PopContext();

                return VisitDelegateType(cppTypeName);
            }

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var isRef = Context.Parameter.Usage == ParameterUsage.Out ||
                    Context.Parameter.Usage == ParameterUsage.InOut;

                Context.ArgumentPrefix.Write("&");
                Context.Return.Write($"(::{@enum.QualifiedOriginalName}){0}{Context.Parameter.Name}",
                    isRef ? string.Empty : "*");
                return true;
            }

            Class @class;
            if (pointee.TryGetClass(out @class) && @class.IsValueType)
            {
                if (Context.Function == null)
                    Context.Return.Write("&");
                return pointer.QualifiedPointee.Visit(this);
            }

            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                var cppTypeName = pointer.Visit(typePrinter, quals);

                Context.Return.Write($"({cppTypeName})");
                Context.Return.Write(Context.Parameter.Name);
                return true;
            }

            return pointer.QualifiedPointee.Visit(this);
        }

        public override bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public static (string func, string type, string cast) GetNAPIPrimitiveGetter(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Bool:
                    return ("napi_get_value_bool", "bool", "bool");
                case PrimitiveType.WideChar:
                    return ("napi_get_value_int32", "int32_t", "wchar_t");
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                    return ("napi_get_value_int32", "int32_t", "char");
                case PrimitiveType.Char16:
                    return ("napi_get_value_int32", "int32_t", "char16_t");
                case PrimitiveType.Char32:
                    return ("napi_get_value_int32", "int32_t", "char32_t");
                case PrimitiveType.Short:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    return ("napi_get_value_int32", "int32_t", null);
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                    return ("napi_get_value_uint32", "uint32_t", null);
                case PrimitiveType.LongLong:
                    return ("napi_get_value_bigint_int64", "int64_t", null);
                case PrimitiveType.ULongLong:
                    return ("napi_get_value_bigint_uint64", "uint64_t", null);
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                    return ("napi_get_value_double", "double", "float");
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    return ("napi_get_value_double", "double", null);
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                    return (null, null, null);
                case PrimitiveType.String:
                    return ("napi_get_value_string_utf8", "char*", null);
                case PrimitiveType.Null:
                    return ("napi_get_null", null, null);
                case PrimitiveType.Void:
                    return (null, null, null);
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                    return (null, null, null);
                case PrimitiveType.Decimal:
                default:
                    throw new NotImplementedException();
            }
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var (func, type, cast) = GetNAPIPrimitiveGetter(primitive);

            switch (primitive)
            {
                case PrimitiveType.Bool:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Short:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    Context.Before.WriteLine($"{type} {Context.Parameter.Name};");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}," +
                                             $" &{Context.Parameter.Name});");

                    if (!string.IsNullOrEmpty(cast))
                        Context.Return.Write($"({cast})");

                    Context.Return.Write($"{Context.Parameter.Name}");
                    return true;

                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                    Context.Before.WriteLine($"{type} {Context.Parameter.Name};");
                    Context.Before.WriteLine("bool lossless;");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}," +
                                             $" &{Context.Parameter.Name}, &lossless);");

                    if (!string.IsNullOrEmpty(cast))
                        Context.Return.Write($"({cast})");

                    Context.Return.Write($"{Context.Parameter.Name}");
                    return true;

                case PrimitiveType.String:
                    var size = $"_{Context.Parameter.Name}_size";
                    Context.Before.WriteLine($"size_t {size};");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}, " +
                                             $"nullptr, 0, &{size});");
                    Context.Before.NewLine();

                    var buf = $"{Context.Parameter.Name}";
                    Context.Before.WriteLine($"char* {buf} = (char*) malloc({size});");
                    Context.Before.WriteLine($"status = {func}(env, {Context.ArgName}, " +
                                             $"nullptr, 0, &{size});");
                    Context.Before.WriteLine("assert(status == napi_ok);");

                    Context.Cleanup.WriteLine($"free({buf});");
                    Context.Return.Write($"{buf}");
                    return true;

                case PrimitiveType.Null:
                    return true;

                case PrimitiveType.Void:
                    return true;

                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                    return true;

                case PrimitiveType.Decimal:
                default:
                    throw new NotImplementedException();
            }
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
                return typeMap.IsValueType;
            }

            if (decl.Type.IsPointerTo(out FunctionType _))
            {
                // Use the original typedef name if available, otherwise just use the function pointer type
                string cppTypeName;
                if (!decl.IsSynthetized)
                {
                    cppTypeName = "::" + typedef.Declaration.QualifiedOriginalName;
                }
                else
                {
                    cppTypeName = decl.Type.Visit(typePrinter, quals);
                }

                VisitDelegateType(cppTypeName);
                return true;
            }

            PrimitiveType primitive;
            if (decl.Type.IsPrimitiveType(out primitive))
            {
                Context.Return.Write($"(::{typedef.Declaration.QualifiedOriginalName})");
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.Context.TypeMaps.FindTypeMap(template, out typeMap) && typeMap.DoesMarshalling)
            {
                typeMap.Type = template;
                typeMap.MarshalToNative(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public override bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            Context.Return.Write(param.Parameter.Name);
            return true;
        }

        public override bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

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
            var type = Context.Parameter.Type.Desugar();

            /*
                        TypeMap typeMap;
                        if (Context.Context.TypeMaps.FindTypeMap(type, out typeMap) &&
                            typeMap.DoesMarshalling)
                        {
                            typeMap.NAPIMarshalToNative(Context);
                            return;
                        }
            */
            var instance = $"{Context.Parameter.Name}_instance";
            Context.Before.WriteLine($"{@class.QualifiedOriginalName}* {instance};");
            Context.Before.WriteLine($"status = napi_unwrap(env, _this, (void**) &{instance});");

            var isPointer = type.SkipPointerRefs().IsPointer();
            var deref = isPointer ? "" : "*";

            if (!isPointer && Context.Parameter.Type.IsReference())
                Context.VarPrefix.Write("&");

            Context.Return.Write($"{deref}{instance}");
        }

        private void MarshalValueClass(Class @class)
        {
            throw new System.NotImplementedException();
        }

        public override bool VisitFieldDecl(Field field)
        {
            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = field.QualifiedType
            };

            return field.Type.Visit(this);
        }

        public override bool VisitProperty(Property property)
        {
            Context.Parameter = new Parameter
            {
                Name = Context.ArgName,
                QualifiedType = property.QualifiedType
            };

            return base.VisitProperty(property);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public override bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            VisitPrimitiveType(@enum.BuiltinType.Type);

            Context.Return.StringBuilder.Clear();
            Context.Return.Write($"(::{@enum.QualifiedOriginalName}){Context.Parameter.Name}");

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        public override bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public override bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitDelegate(Delegate @delegate)
        {
            throw new NotImplementedException();
        }
    }
}

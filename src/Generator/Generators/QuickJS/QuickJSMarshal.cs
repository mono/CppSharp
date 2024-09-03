using System;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;
using CppSharp.Types;
using Delegate = CppSharp.AST.Delegate;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.Cpp
{
    public class QuickJSMarshalNativeToManagedPrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public QuickJSMarshalNativeToManagedPrinter(MarshalContext marshalContext)
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

            var pointee = pointer.Pointee.Desugar();

            var param = Context.Parameter;
            if (param != null && (param.IsOut || param.IsInOut) &&
                pointee.IsPrimitiveType(out _))
            {
                Context.Return.Write(Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(out _))
            {
                if (pointer.IsConstCharString())
                {
                    var retName = Generator.GeneratedIdentifier(Context.ReturnVarName);
                    Context.Before.Write($"JSValue {retName} = JS_NewString(ctx, {Context.ArgName});");
                    Context.Return.Write(retName);
                    return true;
                }

                if (pointer.Pointee is TypedefType)
                {
                    var desugaredPointer = new PointerType()
                    {
                        IsDependent = pointer.IsDependent,
                        Modifier = pointer.Modifier,
                        QualifiedPointee = new QualifiedType(pointee)
                    };
                    var nativeTypeName = desugaredPointer.Visit(typePrinter, quals);
                    Context.Return.Write("reinterpret_cast<{0}>({1})", nativeTypeName,
                        Context.ReturnVarName);
                }
                else
                    Context.Return.Write(Context.ReturnVarName);

                return true;
            }

            Context.Context.TypeMaps.FindTypeMap(pointee, out var typeMap);

            if (pointee.TryGetClass(out var @class) && typeMap == null)
            {
                var instance = (pointer.IsReference) ? "&" + Context.ReturnVarName
                    : Context.ReturnVarName;
                WriteClassInstance(@class, instance);
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

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            var @enum = item.Namespace as Enumeration;
            return VisitPrimitiveType(@enum.BuiltinType.Type);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var retName = Generator.GeneratedIdentifier(Context.ReturnVarName);
            Context.Before.Write($"JSValue {retName} = ");

            (uint width, uint _alignment) =
            primitive.GetInfo(Context.Context.TargetInfo, out bool _signed);

            switch (primitive)
            {
                case PrimitiveType.Void:
                    Context.Before.WriteLine("JS_UNDEFINED;");
                    break;

                case PrimitiveType.Bool:
                    Context.Before.WriteLine($"JS_NewBool(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.Char:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                    Context.Before.WriteLine($"JS_NewInt32(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    if (width == 64)
                        Context.Before.WriteLine($"JS_NewBigInt64(ctx, {Context.ArgName});");
                    else
                        Context.Before.WriteLine($"JS_NewInt32(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                    if (width == 64)
                        Context.Before.WriteLine($"JS_NewBigUint64(ctx, {Context.ArgName});");
                    else
                        Context.Before.WriteLine($"JS_NewUint32(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.LongLong:
                    Context.Before.WriteLine($"JS_NewBigInt64(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.ULongLong:
                    Context.Before.WriteLine($"JS_NewBigUint64(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Context.Before.WriteLine($"JS_NewFloat64(ctx, {Context.ArgName});");
                    break;

                case PrimitiveType.LongDouble:
                    throw new NotImplementedException();

                case PrimitiveType.Null:
                    Context.Before.WriteLine($"JS_NULL;");
                    break;

                default:
                    throw new NotImplementedException();
            }

            Context.Return.Write(retName);
            return true;
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out var typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.Type = typedef;
                typeMap.MarshalToManaged(Context);
                return typeMap.IsValueType;
            }

            if (decl.Type.IsPointerTo(out FunctionType _))
            {
                var typeName = typePrinter.VisitDeclaration(decl);
                var typeName2 = decl.Type.Visit(typePrinter);
                Context.Return.Write(typeName);
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            if (Context.Context.TypeMaps.FindTypeMap(template, out var typeMap) && typeMap.DoesMarshalling)
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

            var instance = string.Empty;

            if (Context.Context.Options.GeneratorKind == GeneratorKind.CLI)
            {
                if (!Context.ReturnType.Type.IsPointer())
                    instance += "&";
            }

            instance += Context.ReturnVarName;
            var needsCopy = Context.MarshalKind != MarshalKind.NativeField;

            if (@class.IsRefType && needsCopy)
            {
                var name = Generator.GeneratedIdentifier(Context.ReturnVarName);
                Context.Before.WriteLine($"auto {name} = {MemoryAllocOperator} ::{{0}}({{1}});",
                    @class.QualifiedOriginalName, Context.ReturnVarName);
                instance = name;
            }

            WriteClassInstance(@class, instance);
            return true;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (!string.IsNullOrEmpty(decl.TranslationUnit.Module.OutputNamespace))
                return $"{decl.TranslationUnit.Module.OutputNamespace}::{decl.QualifiedName}";

            return decl.QualifiedName;
        }

        public void WriteClassInstance(Class @class, string instance)
        {
            if (@class.CompleteDeclaration != null)
            {
                WriteClassInstance(@class.CompleteDeclaration as Class, instance);
                return;
            }

            if (!Context.ReturnType.Type.Desugar().IsPointer())
            {
                Context.Return.Write($"{instance}");
                return;
            }

            if (@class.IsRefType)
                Context.Return.Write($"({instance} == nullptr) ? nullptr : {MemoryAllocOperator} ");

            Context.Return.Write($"{QualifiedIdentifier(@class)}(");
            Context.Return.Write($"(::{@class.QualifiedOriginalName}*)");
            Context.Return.Write($"{instance})");
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
            var retName = Generator.GeneratedIdentifier(Context.ReturnVarName);
            Context.Before.WriteLine($"JSValue {retName} = JS_NewInt32(ctx, (int32_t) {Context.ReturnVarName});");
            Context.Return.Write(retName);

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

    public class QuickJSMarshalManagedToNativePrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public readonly TextGenerator VarPrefix;
        public readonly TextGenerator ArgumentPrefix;

        public QuickJSMarshalManagedToNativePrinter(MarshalContext ctx)
            : base(ctx)
        {
            VarPrefix = new TextGenerator();
            ArgumentPrefix = new TextGenerator();

            Context.MarshalToNative = this;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            if (Context.Context.TypeMaps.FindTypeMap(type, out var typeMap) && typeMap.DoesMarshalling)
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

            var pointee = pointer.Pointee.Desugar();

            if (pointee is FunctionType)
            {
                typePrinter.PushContext(TypePrinterContextKind.Managed);
                var cppTypeName = pointer.Visit(typePrinter, quals);
                typePrinter.PopContext();

                return VisitDelegateType(cppTypeName);
            }

            if (pointee.TryGetEnum(out var @enum))
            {
                var isRef = Context.Parameter.Usage == ParameterUsage.Out ||
                    Context.Parameter.Usage == ParameterUsage.InOut;

                ArgumentPrefix.Write("&");
                Context.Return.Write($"(::{@enum.QualifiedOriginalName}){0}{Context.Parameter.Name}",
                    isRef ? string.Empty : "*");
                return true;
            }

            if (pointee.TryGetClass(out var @class) && @class.IsValueType)
            {
                if (Context.Function == null)
                    Context.Return.Write("&");
                return pointer.QualifiedPointee.Visit(this);
            }

            if (pointer.IsConstCharString())
            {
                var genName = Generator.GeneratedIdentifier(Context.Parameter.Name);
                Context.Before.WriteLine($"auto {genName} = JS_ToCString(ctx, argv[{Context.ParameterIndex}]);");
                Context.Before.WriteLine($"if ({genName} == NULL)");
                Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                Context.Return.Write($"{genName}");
                Context.Cleanup.WriteLine($"JS_FreeCString(ctx, {genName});");
                return true;
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

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            var type = typePrinter.VisitPrimitiveType(primitive);

            var argName = Context.Parameter.Name;
            Context.Before.WriteLine($"{type} {argName};");

            (uint width, uint _alignment) =
            primitive.GetInfo(Context.Context.TargetInfo, out bool _signed);

            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;

                case PrimitiveType.Bool:
                    Context.Before.WriteLine($"{argName} = JS_ToBool(ctx, argv[{Context.ParameterIndex}]);");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                    Context.Before.WriteLine($"int32_t _{argName};");
                    Context.Before.WriteLine($"if (JS_ToInt32(ctx, &_{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Before.WriteLine($"{argName} = ({type})_{argName};");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                    Context.Before.WriteLine($"int32_t _{argName};");
                    Context.Before.WriteLine($"if (JS_ToInt32(ctx, &_{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Before.WriteLine($"{argName} = ({type})_{argName};");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    if (width == 64)
                    {
                        Context.Before.WriteLine($"if (JS_ToBigInt64(ctx, (int64_t*)&{argName}, argv[{Context.ParameterIndex}]))");
                        Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    }
                    else
                    {
                        Context.Before.WriteLine($"if (JS_ToInt32(ctx, &{argName}, argv[{Context.ParameterIndex}]))");
                        Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    }
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                    if (width == 64)
                    {
                        Context.Before.WriteLine($"if (JS_ToBigInt64(ctx, (int64_t*)&{argName}, argv[{Context.ParameterIndex}]))");
                        Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    }
                    else
                    {
                        Context.Before.WriteLine($"if (JS_ToUint32(ctx, &{argName}, argv[{Context.ParameterIndex}]))");
                        Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    }
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.LongLong:
                    Context.Before.WriteLine($"if (JS_ToBigInt64(ctx, (int64_t*)&{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.ULongLong:
                    Context.Before.WriteLine($"if (JS_ToBigUint64(ctx, (uint64_t*)&{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Float:
                    Context.Before.WriteLine($"double _{argName};");
                    Context.Before.WriteLine($"if (JS_ToFloat64(ctx, &_{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Before.WriteLine($"{argName} = ({type})_{argName};");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Double:
                    Context.Before.WriteLine($"if (JS_ToFloat64(ctx, &{argName}, argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.Null:
                    Context.Before.WriteLine($"if (!JS_IsNull(argv[{Context.ParameterIndex}]))");
                    Context.Before.WriteLineIndent("return JS_EXCEPTION;");
                    Context.Return.Write($"{argName}");
                    return true;

                case PrimitiveType.WideChar:
                default:
                    throw new NotImplementedException();
            }
        }

        public override bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (Context.Context.TypeMaps.FindTypeMap(decl.Type, out var typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
                return typeMap.IsValueType;
            }

            if (decl.Type.IsPointerTo(out FunctionType _))
            {
                typePrinter.PushContext(TypePrinterContextKind.Native);
                var declName = decl.Visit(typePrinter);
                typePrinter.PopContext();

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

            if (decl.Type.IsPrimitiveType(out _))
            {
                Context.Return.Write($"(::{typedef.Declaration.QualifiedOriginalName})");
            }

            return decl.Type.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            if (Context.Context.TypeMaps.FindTypeMap(template, out var typeMap) && typeMap.DoesMarshalling)
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
            if (Context.Context.TypeMaps.FindTypeMap(type, out var typeMap) &&
                typeMap.DoesMarshalling)
            {
                typeMap.MarshalToNative(Context);
                return;
            }

            Context.Return.Write($"({@class.QualifiedOriginalName}*) JS_GetOpaque(argv[{Context.ParameterIndex}], 0)");
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

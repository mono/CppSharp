using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates C/C++ source files.
    /// </summary>
    public class CppSources : CCodeGenerator
    {
        public CppSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override string FileExtension { get { return "cpp"; } }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            var file = Path.GetFileNameWithoutExtension(TranslationUnit.FileName)
                .Replace('\\', '/');

            if (Context.Options.GenerateName != null)
                file = Context.Options.GenerateName(TranslationUnit);

            PushBlock(BlockKind.Includes);
            WriteLine("#include \"{0}.h\"", file);
            GenerateForwardReferenceHeaders();

            NewLine();
            PopBlock();

            VisitNamespace(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateForwardReferenceHeaders()
        {
            PushBlock(BlockKind.IncludesForwardReferences);

            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps, Context.Options);
            typeReferenceCollector.Process(TranslationUnit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                if (typeRef.Include.File == TranslationUnit.FileName)
                    continue;

                var include = typeRef.Include;
                if(!string.IsNullOrEmpty(include.File) && !include.InHeader)
                    includes.Add(include.ToString());
            }

            foreach (var include in includes)
                WriteLine(include);

            PopBlock();
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            PushBlock(BlockKind.Namespace);
            foreach (var @class in context.Classes)
            {
                if (!@class.IsGenerated || @class.IsDependent)
                    continue;

                if (@class.IsOpaque || @class.IsIncomplete)
                    continue;

                @class.Visit(this);
            }

            // Generate all the function declarations for the module.
            foreach (var function in context.Functions.Where(f => f.IsGenerated))
            {
                function.Visit(this);
            }

            if (Options.GenerateFunctionTemplates)
            {
                foreach (var template in context.Templates)
                {
                    if (!template.IsGenerated) continue;

                    var functionTemplate = template as FunctionTemplate;
                    if (functionTemplate == null) continue;

                    if (!functionTemplate.IsGenerated)
                        continue;

                    GenerateFunctionTemplate(functionTemplate);
                }
            }

            foreach(var childNamespace in context.Namespaces)
                VisitDeclContext(childNamespace);

            PopBlock();

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock(BlockKind.Class);

            VisitDeclContext(@class);

            GenerateClassConstructors(@class);
            GenerateClassMethods(@class);
            GenerateClassProperties(@class);

            foreach (var @event in @class.Events)
            {
                if (!@event.IsGenerated)
                    continue;

                @event.Visit(this);
            }

            foreach (var variable in @class.Variables)
            {
                if (!variable.IsGenerated)
                    continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                variable.Visit(this);
            }

            PopBlock();

            return true;
        }

        public virtual void GenerateClassConstructors(Class @class)
        {
            if (@class.IsStatic)
                return;

            // Output a default constructor that takes the native instance.
            GenerateClassConstructor(@class);

            if (@class.IsRefType)
            {
                var destructor = @class.Destructors
                    .FirstOrDefault(d => d.Parameters.Count == 0 && d.Access == AccessSpecifier.Public);

                if (destructor != null)
                {
                    GenerateClassDestructor(@class);

                    if (Options.GenerateFinalizers)
                        GenerateClassFinalizer(@class);
                }
            }
        }

        public virtual void GenerateClassMethods(Class @class)
        {
            foreach (var method in @class.Methods.Where(m => !m.IsOperator))
            {
                if (ASTUtils.CheckIgnoreMethod(method) || CppHeaders.FunctionIgnored(method))
                    continue;

                // Do not generate property getter/setter methods as they will be generated
                // as part of properties generation.
                var field = (method?.AssociatedDeclaration as Property)?.Field;
                if (field != null)
                    continue;

                method.Visit(this);
            }
        }

        public virtual void GenerateClassProperties(Class @class)
        {
            foreach (var property in @class.Properties)
            {
                if (ASTUtils.CheckIgnoreProperty(property) || CppHeaders.TypeIgnored(property.Type))
                    continue;

                property.Visit(this);
            }
        }

        public virtual void GenerateClassDestructor(Class @class)
        {
            PushBlock(BlockKind.Destructor);

            WriteLine($"{QualifiedIdentifier(@class)}::~{@class.Name}()");
            WriteOpenBraceAndIndent();
            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void GenerateClassFinalizer(Class @class)
        {

        }

        public virtual void GenerateFunctionTemplate(FunctionTemplate template)
        {
        }

        public override bool VisitProperty(Property property)
        {
            PushBlock(BlockKind.Property, property);

            if (property.HasGetter)
                GeneratePropertyGetter(property.GetMethod);

            if (property.HasSetter && property.SetMethod != null)
                GeneratePropertySetter(property.SetMethod);

            PopBlock(NewLineKind.Never);

            return true;
        }

        public override void GeneratePropertyGetter(Method method)
        {
            PushBlock(BlockKind.Method, method);

            GeneratePropertyAccessorSpecifier(method);
            NewLine();

            WriteOpenBraceAndIndent();

            GenerateFunctionCall(method);

            UnindentAndWriteCloseBrace();
            NewLine();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GeneratePropertySetter(Method method)
        {
            PushBlock(BlockKind.Method, method);

            GeneratePropertyAccessorSpecifier(method);
            NewLine();

            WriteOpenBraceAndIndent();

            GenerateFunctionCall(method);

            UnindentAndWriteCloseBrace();
            NewLine();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitEvent(Event @event)
        {
            GenerateDeclarationCommon(@event);

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            GenerateDeclarationCommon(variable);

            return true;
        }

        public virtual string ClassCtorInstanceParamIdentifier => "instance";

        public virtual void GenerateClassConstructor(Class @class)
        {
            Write($"{QualifiedIdentifier(@class)}::{@class.Name}(");

            var nativeType = $"::{@class.QualifiedOriginalName}*";
            WriteLine($"{nativeType} {ClassCtorInstanceParamIdentifier})");
            GenerateClassConstructorBase(@class);

            WriteOpenBraceAndIndent();

            WriteLine($"{Helpers.InstanceIdentifier} = {ClassCtorInstanceParamIdentifier};");

            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private bool GenerateClassConstructorBase(Class @class, Method method = null)
        {
            var hasBase = @class.HasBase && @class.Bases[0].IsClass && @class.Bases[0].Class.IsGenerated;
            if (!hasBase)
                return false;

            if (!@class.IsValueType)
            {
                Indent();

                var baseClass = @class.Bases[0].Class;
                Write($": {QualifiedIdentifier(baseClass)}(");

                // We cast the value to the base class type since otherwise there
                // could be ambiguous call to overloaded constructors.
                CTypePrinter.PushContext(TypePrinterContextKind.Native);
                var nativeTypeName = baseClass.Visit(CTypePrinter);
                CTypePrinter.PopContext();
                Write($"({nativeTypeName}*)");

                WriteLine("{0})", method != null ? "nullptr" : ClassCtorInstanceParamIdentifier);

                Unindent();
            }

            return true;
        }

        public override string GetMethodIdentifier(Function function,
            TypePrinterContextKind context = TypePrinterContextKind.Managed)
        {
            var method = function as Method;
            if (method != null)
            {
                var @class = method.Namespace as Class;
                return $"{QualifiedIdentifier(@class)}::{base.GetMethodIdentifier(method, context)}";
            }

            return base.GetMethodIdentifier(function);
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!method.IsGenerated || CppHeaders.FunctionIgnored(method))
                return false;

            PushBlock(BlockKind.Method, method);

            GenerateMethodSpecifier(method, method.Namespace as Class);
            NewLine();

            var @class = method.Namespace as Class;
            if (method.IsConstructor)
                GenerateClassConstructorBase(@class, method);

            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.MethodBody, method);

            if (method.IsConstructor && @class.IsRefType)
                WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");

            if (method.IsProxy)
                goto SkipImpl;

            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    if (!@class.IsAbstract)
                    {
                        var @params = GenerateFunctionParamsMarshal(method.Parameters, method);
                        Write($"{Helpers.InstanceIdentifier} = new ::{method.Namespace.QualifiedOriginalName}(");
                        GenerateFunctionParams(@params);
                        WriteLine(");");
                    }
                }
                else
                {
                    GenerateFunctionCall(method);
                }
            }

        SkipImpl:

            PopBlock();

            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.Always);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated || CppHeaders.FunctionIgnored(function))
                return false;

            PushBlock(BlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var returnType = function.ReturnType.Visit(CTypePrinter);

            var name = function.Visit(CTypePrinter);
            Write($"{returnType} ({name})(");

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write($"{CTypePrinter.VisitParameter(param)}");
                if (i < function.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(")");
            WriteOpenBraceAndIndent();

            GenerateFunctionCall(function);

            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateFunctionCall(Function function)
        {
            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            var needsReturn = !function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);
            if (needsReturn)
            {
                CTypePrinter.PushContext(TypePrinterContextKind.Native);
                var returnType = function.ReturnType.Visit(CTypePrinter);
                CTypePrinter.PopContext();

                Write($"{returnType} {Helpers.ReturnIdentifier} = ");
            }

            var method = function as Method;
            var @class = function.Namespace as Class;

            var property = method?.AssociatedDeclaration as Property;
            var field = property?.Field;
            if (field != null)
            {
                Write($"((::{@class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");
                Write($"{field.OriginalName}");

                var isGetter = property.GetMethod == method;
                if (isGetter)
                    WriteLine(";");
                else
                    WriteLine($" = {@params[0].Name};");
            }
            else
            {
                if (IsNativeFunctionOrStaticMethod(function))
                {
                    Write($"::{function.QualifiedOriginalName}(");
                }
                else
                {
                    if (IsNativeMethod(function))
                        Write($"((::{@class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");

                    Write($"{base.GetMethodIdentifier(function, TypePrinterContextKind.Native)}(");
                }

                GenerateFunctionParams(@params);
                WriteLine(");");
            }

            foreach(var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if(param.Usage != ParameterUsage.Out && param.Usage != ParameterUsage.InOut)
                    continue;

                if (param.Type.IsPointer() && !param.Type.GetFinalPointee().IsPrimitiveType())
                    param.QualifiedType = new QualifiedType(param.Type.GetFinalPointee());

                var nativeVarName = paramInfo.Name;

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = nativeVarName,
                    ReturnVarName = nativeVarName,
                    ReturnType = param.QualifiedType
                };

                var marshal = new CppMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);
                
                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine($"{param.Name} = {marshal.Context.Return};");
            }

            if (needsReturn)
            {
                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = Helpers.ReturnIdentifier,
                    ReturnVarName = Helpers.ReturnIdentifier,
                    ReturnType = function.ReturnType
                };

                var marshal = new CppMarshalNativeToManagedPrinter(ctx);
                function.ReturnType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine($"return {marshal.Context.Return};");
            }
        }

        public static bool IsNativeMethod(Function function)
        {
            var method = function as Method;
            if (method == null) 
                return false;

            return method.Conversion == MethodConversionKind.None;
        }

        public bool IsNativeFunctionOrStaticMethod(Function function)
        {
            var method = function as Method;
            if (method == null) 
                return true;

            if (!IsCLIGenerator && method.IsOperator)
                return false;

            if (method.IsOperator && Operators.IsBuiltinOperator(method.OperatorKind))
                return true;

            return method.IsStatic || method.Conversion != MethodConversionKind.None;
        }

        public struct ParamMarshal
        {
            public string Name;
            public string Prefix;

            public Parameter Param;
        } 

        public List<ParamMarshal> GenerateFunctionParamsMarshal(IEnumerable<Parameter> @params,
            Function function = null)
        {
            var marshals = new List<ParamMarshal>();

            var paramIndex = 0;
            foreach (var param in @params)
            {
                marshals.Add(GenerateFunctionParamMarshal(param, paramIndex, function));
                paramIndex++;
            }

            return marshals;
        }

        private ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex,
            Function function = null)
        {
            var paramMarshal = new ParamMarshal { Name = param.Name, Param = param };

            if (param.Type is BuiltinType)
                return paramMarshal;

            var argName = Generator.GeneratedIdentifier("arg") + paramIndex.ToString(CultureInfo.InvariantCulture);

            Parameter effectiveParam = param;
            var isRef = param.IsOut || param.IsInOut;
            var paramType = param.Type;

            var ctx = new MarshalContext(Context, CurrentIndentation)
            {
                Parameter = effectiveParam,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            var marshal = new CppMarshalManagedToNativePrinter(ctx);
            effectiveParam.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception($"Cannot marshal argument of function '{function.QualifiedOriginalName}'");

            if (isRef)
            {
                var type = paramType.Visit(CTypePrinter);

                if (param.IsInOut)
                {
                    if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                        Write(marshal.Context.Before);

                    WriteLine($"{type} {argName} = {marshal.Context.Return};");
                }
                else
                    WriteLine($"{type} {argName};");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine($"auto {marshal.VarPrefix}{argName} = {marshal.Context.Return};");
                paramMarshal.Prefix = marshal.ArgumentPrefix;
            }

            paramMarshal.Name = argName;
            return paramMarshal;
        }

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param =>
                string.IsNullOrWhiteSpace(param.Prefix) ? param.Name : (param.Prefix + param.Name))
                .ToList();

            Write(string.Join(", ", names));
        }
    }
}

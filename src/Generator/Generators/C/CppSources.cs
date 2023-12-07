using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;
using CppSharp.Types;

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
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override string FileExtension { get { return "cpp"; } }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            var file = Context.Options.GetIncludePath(TranslationUnit);
            WriteLine($"#include \"{file}\"");
            GenerateForwardReferenceHeaders();

            NewLine();
            PopBlock();

            GenerateMain();

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public virtual void GenerateMain()
        {
            VisitNamespace(TranslationUnit);
        }

        public virtual void GenerateForwardReferenceHeaders()
        {
            GenerateForwardReferenceHeaders(TranslationUnit);
        }

        public virtual void GenerateForwardReferenceHeaders(TranslationUnit unit)
        {
            PushBlock(BlockKind.IncludesForwardReferences);

            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps, Context.Options);
            typeReferenceCollector.Process(unit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                var filename = Context.Options.GenerateName != null ? $"{Context.Options.GenerateName(TranslationUnit)}{Path.GetExtension(TranslationUnit.FileName)}" : TranslationUnit.FileName;
                if (typeRef.Include.File == filename)
                    continue;

                var include = typeRef.Include;
                if (!string.IsNullOrEmpty(include.File) && !include.InHeader)
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

            foreach (var childNamespace in context.Namespaces)
                VisitDeclContext(childNamespace);

            PopBlock();

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock(BlockKind.Class, @class);

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
            GenerateClassConstructor(@class, withOwnNativeInstanceParam: true);

            if (@class.IsRefType)
            {
                var destructor = @class.Destructors
                    .FirstOrDefault(d => d.Parameters.Count == 0 && d.Access == AccessSpecifier.Public);

                if (destructor != null)
                {
                    GenerateClassDestructor(@class);

                    if (Options.GenerateFinalizerFor(@class))
                        GenerateClassFinalizer(@class);
                }
            }
        }

        public virtual void GenerateClassMethods(Class @class)
        {
            foreach (var method in @class.Methods)
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

        public virtual void GenerateClassConstructor(Class @class, bool withOwnNativeInstanceParam = false)
        {
            Write($"{QualifiedIdentifier(@class)}::{@class.Name}(");

            var nativeType = $"{typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*";
            //WriteLine($"{nativeType} {ClassCtorInstanceParamIdentifier})");
            WriteLine(!withOwnNativeInstanceParam ? $"{nativeType} {ClassCtorInstanceParamIdentifier})" :
                $"{nativeType} {ClassCtorInstanceParamIdentifier}, bool ownNativeInstance)");

            var hasBase = GenerateClassConstructorBase(@class, null, withOwnNativeInstanceParam);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                Indent();
                Write(hasBase ? "," : ":");
                Unindent();

                WriteLine(!withOwnNativeInstanceParam ? " {0}(false)" : " {0}(ownNativeInstance)",
                    Helpers.OwnsNativeInstanceIdentifier);
            }

            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.ConstructorBody, @class);

            WriteLine($"{Helpers.InstanceIdentifier} = {ClassCtorInstanceParamIdentifier};");

            PopBlock();

            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private bool GenerateClassConstructorBase(Class @class, Method method = null,
            bool withOwnNativeInstanceParam = false)
        {
            if (@class.IsValueType)
                return true;

            if (!@class.NeedsBase)
                return false;

            Indent();

            Write($": {QualifiedIdentifier(@class.BaseClass)}(");

            // We cast the value to the base class type since otherwise there
            // could be ambiguous call to overloaded constructors.
            var nativeTypeName = @class.BaseClass.Visit(typePrinter);

            Write($"({nativeTypeName}*)");
            WriteLine("{0}{1})", method != null ? "nullptr" : ClassCtorInstanceParamIdentifier,
                !withOwnNativeInstanceParam ? "" : ", ownNativeInstance");

            Unindent();

            return true;
        }

        public override string GetMethodIdentifier(Function function,
            TypePrinterContextKind context = TypePrinterContextKind.Managed)
        {
            string id;
            if (function is Method method)
            {
                var @class = method.Namespace as Class;
                CTypePrinter.PushScope(TypePrintScopeKind.Qualified);
                id = $"{QualifiedIdentifier(@class)}::{base.GetMethodIdentifier(method, context)}";
                CTypePrinter.PopScope();
                return id;
            }

            CTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            id = base.GetMethodIdentifier(function);
            CTypePrinter.PopScope();
            return id;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!method.IsGenerated || CppHeaders.FunctionIgnored(method))
                return false;

            if (method.IsPure)
                return false;

            PushBlock(BlockKind.Method, method);

            GenerateMethodSpecifier(method);
            NewLine();

            var @class = method.Namespace as Class;
            if (method.IsConstructor)
                GenerateClassConstructorBase(@class, method);

            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.MethodBody, method);

            if (Context.DeclMaps.FindDeclMap(method, out DeclMap declMap))
            {
                declMap.Declaration = method;
                declMap.DeclarationContext = @class;
                declMap.Generate(this);
                goto SkipImpl;
            }

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
                        PushBlock(BlockKind.ConstructorBody, @class);

                        var @params = GenerateFunctionParamsMarshal(method.Parameters, method);
                        Write($@"{Helpers.InstanceIdentifier} = new {typePrinter.PrintTag(@class)}::{
                            method.Namespace.QualifiedOriginalName}(");
                        GenerateFunctionParams(@params);
                        WriteLine(");");

                        PopBlock();
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
                Write($@"(({typePrinter.PrintTag(@class)}::{
                    @class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");
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
                    if (function.IsNativeMethod())
                        Write($@"(({typePrinter.PrintTag(@class)}::{
                            @class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");

                    Write($"{base.GetMethodIdentifier(function, TypePrinterContextKind.Native)}(");
                }

                GenerateFunctionParams(@params);
                WriteLine(");");
            }

            foreach (var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if (param.Usage != ParameterUsage.Out && param.Usage != ParameterUsage.InOut)
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
                GenerateFunctionCallReturnMarshal(function);
            }
        }

        public virtual void GenerateFunctionCallReturnMarshal(Function function)
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

        public virtual ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex,
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

                WriteLine($"auto {marshal.Context.VarPrefix}{argName} = {marshal.Context.Return};");
                paramMarshal.Prefix = marshal.Context.ArgumentPrefix;
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

    public class OverridesClassGenerator : CCodeGenerator
    {
        public enum GenerationMode
        {
            Declaration,
            Definition
        }

        HashSet<Method> UniqueMethods;
        Class Class;
        readonly Func<Method, bool> Filter;
        GenerationMode Mode;

        public OverridesClassGenerator(BindingContext context,
            GenerationMode mode, Func<Method, bool> filter = null)
            : base(context)
        {
            Mode = mode;
            Filter = filter;
        }

        public virtual bool ShouldVisitMethod(Method method)
        {
            return Filter != null ? Filter(method) : true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Native);
            var typeName = @class.Visit(CTypePrinter);

            WriteLine($"class _{@class.Name} : public {typeName}");
            WriteLine("{");
            WriteLine("public:");
            NewLine();
            Indent();

            Class = @class;
            UniqueMethods = new HashSet<Method>();

            foreach (var component in @class.Layout.Layout.Components)
            {
                var method = component.Method;
                if (method == null)
                    continue;

                if (!method.IsVirtual || (method.GenerationKind == GenerationKind.None))
                    continue;

                if (!UniqueMethods.Add(method))
                    continue;

                if (!ShouldVisitMethod(method))
                    continue;

                method = new Method(component.Method)
                {
                    IsOverride = true
                };

                if (method.IsConstructor || method.IsDestructor)
                    continue;

                UniqueMethods.Add(method);

                method.Visit(this);
            }

            Unindent();
            WriteLine("};");

            CTypePrinter.PopContext();

            Class = null;
            UniqueMethods = null;

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock(BlockKind.Method, method);

            var isDeclaration = Mode == GenerationMode.Declaration;
            GenerateMethodSpecifier(method, isDeclaration ?
                MethodSpecifierKind.Declaration : MethodSpecifierKind.Definition);

            if (isDeclaration)
            {
                WriteLine(";");
            }
            else
            {
                NewLine();
                WriteOpenBraceAndIndent();

                if (Context.DeclMaps.FindDeclMap(method, out DeclMap declMap))
                {
                    declMap.Declaration = method;
                    declMap.DeclarationContext = Class;
                    declMap.Generate(this);
                }
                else
                {
                    var needsReturn = !method.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);
                    if (needsReturn)
                    {
                        var returnType = method.ReturnType.Visit(CTypePrinter);
                        Write($"{returnType} {Helpers.ReturnIdentifier} = ");
                    }

                    var parameters = string.Join(", ", method.Parameters.Select(p => p.Name));
                    WriteLine($"this->{method.OriginalName}({parameters});");

                    if (needsReturn)
                        WriteLine($"return {Helpers.ReturnIdentifier};");
                }

                UnindentAndWriteCloseBrace();
            }

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}

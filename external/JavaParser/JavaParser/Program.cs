using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using IKVM.Internal;
using ArrayType = CppSharp.AST.ArrayType;
using Parameter = CppSharp.AST.Parameter;
using PrimitiveType = CppSharp.AST.PrimitiveType;
using Type = CppSharp.AST.Type;

namespace JavaParser.AST
{
    public class Options : DriverOptions
    {
        /// <summary>
        /// The name of the library to be bound.
        /// </summary>
        public Module Module;
    }

    public class JavaJarASTGenerator
    {
        public static Dictionary<TranslationUnit, ClassFile> JavaClassFiles
            = new Dictionary<TranslationUnit, ClassFile>();

        ASTContext ASTContext { get; set; }
        Options Options { get; set; }

        public JavaJarASTGenerator(ASTContext context, Options options)
        {
            ASTContext = context;
            Options = options;
        }

        public TranslationUnit VisitClassFile(ClassFile classFile)
        {
            var unit = GetTranslationUnit(classFile);

            var components = classFile.Name.Split(".").ToArray();
            var namespaces = components.SkipLast(1).ToArray();

            Namespace currentNamespace = unit;
            bool flattenNamespaces = true;
            if (!flattenNamespaces)
            {
                foreach (var @namespace in namespaces)
                    currentNamespace = currentNamespace.FindCreateNamespace(@namespace);
            }

            DeclarationContext decl = classFile.IsEnum ? (DeclarationContext)VisitEnum(classFile) : 
                (DeclarationContext)VisitClass(classFile);

            decl.Namespace = currentNamespace;
            currentNamespace.Declarations.Add(decl);

            return unit;
        }

        public Enumeration VisitEnum(ClassFile classFile)
        {
            var components = classFile.Name.Split(".").ToArray();

            var @class = new Enumeration
            {
                Name = components.Last(),
            };

            return @class;
        }

        public static AccessSpecifier ConvertMethodAccess(ClassFile.Method method)
        {
            if (method.IsInternal)
                return AccessSpecifier.Internal;
            if (method.IsPrivate)
                return AccessSpecifier.Private;
            if (method.IsProtected)
                return AccessSpecifier.Protected;    
            if (method.IsPublic)
                return AccessSpecifier.Public;

            return AccessSpecifier.Public;
        }
        
        public Class VisitClass(ClassFile classFile)
        {
            var components = classFile.Name.Split(".").ToArray();

            var @class = new Class
            {
                Name = components.Last(),
                Type = classFile.IsInterface ? ClassType.Interface : ClassType.RefType
            };

            foreach (var javaMethod in classFile.Methods)
            {
                var method = new Method
                {
                    Name = javaMethod.Name,
                    Access = ConvertMethodAccess(javaMethod)
                };

                var (retType, paramTypes) = GetTypeFromMethodSig(javaMethod.Signature);

                method.ReturnType = new QualifiedType(retType);

                var paramIndex = 0;
                foreach (var paramType in paramTypes)
                {
                    var param = new CppSharp.AST.Parameter
                    {
                        Namespace = method,
                        QualifiedType = new QualifiedType(paramType)
                    };

                    if (javaMethod.MethodParameters != null)
                        if (paramIndex < javaMethod.MethodParameters.Length)
                            param.Name = javaMethod.MethodParameters[paramIndex].name;

                    method.Parameters.Add(param);
                    paramIndex++;
                }

                @class.Methods.Add(method);
            }

            return @class;
        }

        public static CppSharp.AST.PrimitiveType GetPrimitiveTypeFromJavaSig(char sig)
        {
            switch (sig)
            {
                case 'B':
                    return CppSharp.AST.PrimitiveType.Char;
                case 'C':
                    return CppSharp.AST.PrimitiveType.WideChar;
                case 'D':
                    return CppSharp.AST.PrimitiveType.Double;
                case 'F':
                    return CppSharp.AST.PrimitiveType.Float;
                case 'I':
                    return CppSharp.AST.PrimitiveType.Int;
                case 'J':
                    return CppSharp.AST.PrimitiveType.Long;
                case 'S':
                    return CppSharp.AST.PrimitiveType.Short;
                case 'Z':
                    return CppSharp.AST.PrimitiveType.Bool;
                case 'V':
                    return CppSharp.AST.PrimitiveType.Void;
                default:
                    throw new NotImplementedException();
            }
        }

        public static (CppSharp.AST.Type, List<CppSharp.AST.Type>) GetTypeFromMethodSig(string sig)
        {
            var types = new List<CppSharp.AST.Type>();

            int i = 1;
            while(sig[i] != ')')
                types.Add(GetTypeFromSig(sig, ref i));
            i++;

            var retType = GetTypeFromSig(sig, ref i);

            return (retType, types);
        }

        public static CppSharp.AST.TagType GetTagTypeFromSig(string sig, ref int index)
        {
            int pos = index;
            index = sig.IndexOf(';', index) + 1;
            var className = sig.Substring(pos, index - pos - 1);
            var @class = new TagType();
            return @class;
        }

        public static CppSharp.AST.Type GetTypeFromSig(string sig, ref int index)
        {
            var @char = sig[index++];
            switch(@char)
            {
                case 'B':
                case 'C':
                case 'D':
                case 'F':
                case 'I':
                case 'J':
                case 'S':
                case 'Z':
                case 'V':
                    return new BuiltinType(GetPrimitiveTypeFromJavaSig(@char));
                case 'L':
                {
                    int pos = index;
                    index = sig.IndexOf(';', index) + 1;
                    var className = sig.Substring(pos, index - pos - 1);
                    var @class = new TagType();
                    return @class;
                }
                case '[':
                {
                    var rootArrayType = new CppSharp.AST.ArrayType
                    {
                        SizeType = CppSharp.AST.ArrayType.ArraySize.Variable
                    };
                    var arrayType = rootArrayType;

                    while(sig[index] == '[')
                    {
                        index++;
                        var newArrayType = new CppSharp.AST.ArrayType
                        {
                            SizeType = CppSharp.AST.ArrayType.ArraySize.Variable
                        };
                        arrayType.QualifiedType = new QualifiedType(newArrayType);
                        arrayType = newArrayType;
                    }

                    var innerType = GetTypeFromSig(sig, ref index);
                    arrayType.QualifiedType = new QualifiedType(innerType);

                    return rootArrayType;
                }
                default:
                    throw new InvalidOperationException(sig.Substring(index));
            }
        }

        TranslationUnit GetTranslationUnit(ClassFile classFile)
        {
            var unitName = Options.Module.LibraryName ?? Path.GetFileName (classFile.Name);

            var unit = ASTContext.TranslationUnits.Find(m => m.FileName.Equals(unitName));
            if (unit != null)
                return unit;

            unit = ASTContext.FindOrCreateTranslationUnit(unitName);
            unit.FilePath = unitName;

            JavaClassFiles[unit] = classFile;

            return unit;
        }
    }

    public class JavaJsonASTGenerator
    {
        ASTContext ASTContext { get; set; }
        Options Options { get; set; }

        public JavaJsonASTGenerator(ASTContext context, Options options)
        {
            ASTContext = context;
            Options = options;
        }

        public TranslationUnit CurrentUnit;

        public void VisitCompilationUnits(List<CompilationUnit> compilationUnits)
        {
            foreach (var unit in compilationUnits)
            {
                CurrentUnit = GetTranslationUnit(unit);
                CurrentUnit.Module = Options.Module;

                foreach (var type in unit.Types)
                {
                    var decl = VisitTypeDeclaration(type);
                    if (decl == null)
                        continue;

                    decl.Namespace = CurrentUnit;
                    if (CurrentUnit.Declarations.All(d => d.Name != decl.Name))
                        CurrentUnit.Declarations.Add(decl);
                }
            }
        }

        public TranslationUnit GetTranslationUnit(CompilationUnit compilationUnit)
        {
            var unitName = Options.Module.LibraryName ?? Path.GetFileName (compilationUnit.FileName);

            var unit = ASTContext.TranslationUnits.Find(m => m.FileName.Equals(unitName));
            if (unit != null)
                return unit;

            unit = ASTContext.FindOrCreateTranslationUnit(unitName);
            unit.FilePath = unitName;

            return unit;
        }

        public Declaration VisitTypeDeclaration(TypeDeclaration type)
        {
            switch (type)
            {
                case AnnotationDeclaration annotationDeclaration:
                    return null;
                case ClassOrInterfaceDeclaration classOrInterfaceDeclaration:
                    return VisitClassOrInterfaceDeclaration(classOrInterfaceDeclaration);
                case EnumDeclaration enumDeclaration:
                    return VisitEnumDeclaration(enumDeclaration);
                default:
                    throw new NotImplementedException();
            }
        }

        public Declaration VisitEnumDeclaration(EnumDeclaration enumDeclaration)
        {
            var @class = HandleTypeDeclaration(enumDeclaration);
            if (!@class.IsIncomplete)
                return @class;

            foreach (var entry in enumDeclaration.Entries)
            {
                
            }

            @class.IsIncomplete = false;

            return @class;
        }

        public Class VisitClassOrInterfaceDeclaration(ClassOrInterfaceDeclaration classOrInterfaceDeclaration)
        {
            var @class = HandleTypeDeclaration(classOrInterfaceDeclaration);
            if (!@class.IsIncomplete)
                return @class;

            @class.Type = classOrInterfaceDeclaration.IsInterface ? ClassType.Interface : ClassType.RefType;
            @class.IsIncomplete = false;

            if (classOrInterfaceDeclaration.TypeParameters.Length > 0)
            {
                @class.IsDependent = true;

                foreach (var typeParam in classOrInterfaceDeclaration.TypeParameters)
                {
                    var templateParam = new TypeTemplateParameter { Name = typeParam.Name.Identifier };
                    if (typeParam.TypeBound.Length > 0)
                        templateParam.Constraint = ConvertType(typeParam.TypeBound.First()).ToString();

                    @class.TemplateParameters.Add(templateParam);
                }
            }

            foreach (var extendedType in classOrInterfaceDeclaration.ExtendedTypes)
            {
                var extendedClassType = ConvertType(extendedType);
                @class.Bases.Add(new BaseClassSpecifier
                {
                   Access = AccessSpecifier.Public,
                   IsVirtual = false,
                   Type = extendedClassType
                });
            }
/*
            foreach (var implementedType in classOrInterfaceDeclaration.ImplementedTypes)
            {
                var implementedClassType = ConvertType(implementedType);
                @class.Bases.Add(new BaseClassSpecifier
                {
                    Access = AccessSpecifier.Public,
                    IsVirtual = false,
                    Type = implementedClassType
                });
            }
*/
            return @class;
        }

        public Class HandleTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            var @class = ASTContext.FindClass(typeDeclaration.Name.Identifier).FirstOrDefault();
            if (@class == null)
            {
                @class = new Class {Name = typeDeclaration.Name.Identifier, IsIncomplete = true};
                CurrentUnit.Declarations.Add(@class);
            }

            if (!@class.IsIncomplete)
                return @class;

            foreach (var member in typeDeclaration.Members)
            {
                var decl = VisitBodyDeclaration(member);
                if (decl == null)
                    continue;

                decl.Namespace = @class;

                switch (decl)
                {
                    case Method method:
                        @class.Methods.Add(method);
                        break;
                    case Field field:
                        @class.Fields.Add(field);
                        break;
                    default:
                        @class.Declarations.Add(decl);
                        break;
                }
            }

            return @class;
        }

        public Declaration VisitBodyDeclaration(BodyDeclaration body)
        {
            switch (body)
            {
                case ClassOrInterfaceDeclaration classOrInterfaceDeclaration:
                    return VisitClassOrInterfaceDeclaration(classOrInterfaceDeclaration);
                case ConstructorDeclaration constructorDeclaration:
                    return VisitCallableDeclaration(constructorDeclaration);
                case EnumDeclaration enumDeclaration:
                    return VisitEnumDeclaration(enumDeclaration);
                case InitializerDeclaration initializerDeclaration:
                    return VisitInitializerDeclaration(initializerDeclaration);
                case FieldDeclaration fieldDeclaration:
                    return VisitFieldDeclaration(fieldDeclaration);
                case MethodDeclaration methodDeclaration:
                    return VisitCallableDeclaration(methodDeclaration);
                default:
                    throw new NotImplementedException();
            }
        }

        private Declaration VisitInitializerDeclaration(InitializerDeclaration initializerDeclaration)
        {
            return null;
        }

        public Declaration VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            return null;
        }

        public Method VisitCallableDeclaration(CallableDeclaration callableDeclaration)
        {
            var method = new Method
            {
                Name = callableDeclaration.Name.Identifier,
                Access = AccessSpecifier.Internal
            };

            if (callableDeclaration is ConstructorDeclaration)
                method.Kind = CXXMethodKind.Constructor;

            if (callableDeclaration.Modifiers.Length > 0)
                method.Access = ConvertModifier(callableDeclaration.Modifiers);

            if (callableDeclaration is MethodDeclaration methodDecl)
                method.ReturnType = new QualifiedType(ConvertType(methodDecl.Type));

            foreach (var parameter in callableDeclaration.Parameters)
            {
                var paramType = ConvertType(parameter.Type);
                var param = new CppSharp.AST.Parameter
                {
                    Name = parameter.Name.Identifier,
                    Namespace = method,
                    QualifiedType = new QualifiedType(paramType)
                };

                method.Parameters.Add(@param);
            }

            return method;
        }

        public static CppSharp.AST.PrimitiveType ConvertToPrimitiveType(string type)
        {
            return type switch
            {
                "BOOLEAN" => CppSharp.AST.PrimitiveType.Bool,
                "CHAR" => CppSharp.AST.PrimitiveType.Char,
                "BYTE" => CppSharp.AST.PrimitiveType.UChar,
                "SHORT" => CppSharp.AST.PrimitiveType.Short,
                "INT" => CppSharp.AST.PrimitiveType.Int,
                "LONG" => CppSharp.AST.PrimitiveType.Long,
                "FLOAT" => CppSharp.AST.PrimitiveType.Float,
                "DOUBLE" => CppSharp.AST.PrimitiveType.Double,
                _ => throw new NotImplementedException()
            };
        }

        public CppSharp.AST.Type ConvertType(Type type)
        {
            switch (type)
            {
                case ArrayType arrayType:
                    return new CppSharp.AST.ArrayType(new QualifiedType(ConvertType(arrayType.ComponentType)))
                    {
                        SizeType = CppSharp.AST.ArrayType.ArraySize.Variable
                    };
                case PrimitiveType primitiveType:
                    return new BuiltinType(ConvertToPrimitiveType(primitiveType.Type));
                case ClassOrInterfaceType classOrInterfaceType:
                {
                    var @class = ASTContext.FindClass(classOrInterfaceType.Name.Identifier).FirstOrDefault();
                    if (@class == null)
                    {
                        @class = new Class
                        {
                            Name = classOrInterfaceType.Name.Identifier,
                            Namespace = CurrentUnit,
                            IsIncomplete = true
                        };
                        CurrentUnit.Declarations.Add(@class);
                    }

                    if (classOrInterfaceType.TypeArguments == null || classOrInterfaceType.TypeArguments.Length == 0)
                        return new TagType(@class);

                    return new TemplateSpecializationType
                    {
                        Arguments = classOrInterfaceType.TypeArguments.Select(ConvertType).Select(t =>
                                new TemplateArgument
                                {
                                    Kind = TemplateArgument.ArgumentKind.Type,
                                    Type = new QualifiedType(t),
                                }).ToList(),
                        Desugared = new QualifiedType(new TagType(@class)),
                        Template = new ClassTemplate(@class)
                    };
                }
                case VoidType voidType:
                    return new BuiltinType(CppSharp.AST.PrimitiveType.Void);
                case WildcardType wildcardType:
                {
                    return new TemplateSpecializationType();
                }
                default:
                    throw new NotImplementedException();
            }
        }

        public static AccessSpecifier ConvertModifier(Modifier[] modifiers)
        {
            foreach (var modifier in modifiers)
            {
                switch (modifier.Keyword)
                {
                    case "PUBLIC":
                        return AccessSpecifier.Public;
                    case "PROTECTED":
                        return AccessSpecifier.Protected;
                    case "PRIVATE":
                        return AccessSpecifier.Private;
                    case "DEFAULT":
                        return AccessSpecifier.Internal;
                }
            }

            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options
            {
                GeneratorKind = GeneratorKind.TypeScript,
                OutputDir = "ts"
            };

            var module = new Module("runelite-api");
            options.Module = module;
            options.Modules.Add(module);

            var driver = new Driver(options);
            driver.Setup();
            driver.SetupTypeMaps();

            driver.Context.ASTContext = new ASTContext();
            var astContext = driver.Context.ASTContext;

            //var jarAstGenerator = new JavaJarASTGenerator(astContext, options);
            //var jarFile = "runelite-api-3.5.4.jar";
            //ReadJar(jarFile, astGenerator);

            var compilationUnits = ReadJSON();
            var jsonAstGenerator = new JavaJsonASTGenerator(astContext, options);
            jsonAstGenerator.VisitCompilationUnits(compilationUnits);

            // Sort declarations alphabetically
            var translationUnit = astContext.TranslationUnits.First();
            var decls = new List<Declaration>(translationUnit.Declarations);
            decls = decls.OrderBy(d => d.Name).ToList();
            translationUnit.Declarations.Clear();
            translationUnit.Declarations.AddRange(decls);

            driver.ProcessCode();
            var outputs = driver.GenerateCode();
            driver.SaveCode(outputs);
        }

        private static List<CompilationUnit> ReadJSON()
        {
            var compilationUnits = new List<CompilationUnit>();

            var jsonDir = "/home/joao/dev/rs/flower/javaparser-gradle-sample/out";
            var jsonFiles = Directory.EnumerateFiles(jsonDir, "*.json",
                SearchOption.AllDirectories);

            Parallel.ForEach(jsonFiles, (file) =>
            {
                if (Path.GetFileNameWithoutExtension(file).StartsWith("class"))
                    return;

                var jsonText = File.ReadAllText(file);
                var compilationUnit = CompilationUnit.FromJson(jsonText);
                compilationUnit.FileName = Path.GetFileNameWithoutExtension(file);
                compilationUnits.Add(compilationUnit);
            });

            return compilationUnits;
        }

        private static void ReadJar(string jarFile, JavaJarASTGenerator jarAstGenerator)
        {
            // TODO: Read from jar file directly.
            var classDir = @"/home/joao/dev/CppSharp/tests2/jar";
            foreach (var file in Directory.EnumerateFiles(classDir, "*.class",
                SearchOption.AllDirectories))
            {
                if (Path.GetFileNameWithoutExtension(file).StartsWith("class"))
                    continue;

                var data = File.ReadAllBytes(file);
                var parseOptions = ClassFileParseOptions.RemoveAssertions;
                var classFile = new ClassFile(data, 0, data.Length, file, parseOptions, null);

                jarAstGenerator.VisitClassFile(classFile);
            }
        }
    }
}
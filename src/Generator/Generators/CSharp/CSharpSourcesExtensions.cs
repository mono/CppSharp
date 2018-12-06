using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpSourcesExtensions
    {
        public static void DisableTypeMap(this CSharpSources gen, Class @class)
        {
            var mapped = @class.OriginalClass ?? @class;
            DisableSingleTypeMap(mapped, gen.Context);
            if (mapped.IsDependent)
                foreach (var specialization in mapped.Specializations)
                    DisableSingleTypeMap(specialization, gen.Context);
        }

        public static void GenerateNativeConstructorsByValue(
            this CSharpSources gen, Class @class)
        {
            var printedClass = @class.Visit(gen.TypePrinter);
            if (@class.IsDependent)
                foreach (var specialization in @class.GetSpecializedClassesToGenerate(
                    ).Where(s => s.IsGenerated))
                    gen.GenerateNativeConstructorByValue(specialization, printedClass.Type);
            else
                gen.GenerateNativeConstructorByValue(@class, printedClass.Type);
        }

        public static void GenerateField(this CSharpSources gen, Class @class,
            Field field, Action<Field, Class, QualifiedType> generate, bool isVoid)
        {
            if (@class.IsDependent)
            {
                if (@class.Fields.Any(f => f.Type.IsDependent))
                {
                    foreach (var parameter in @class.TemplateParameters)
                        gen.WriteLine($"var __{parameter.Name} = typeof({parameter.Name});");

                    foreach (var specialization in @class.Specializations.Where(s => s.IsGenerated))
                    {
                        WriteTemplateSpecializationCheck(gen, @class, specialization);
                        gen.WriteOpenBraceAndIndent();
                        var specializedField = specialization.Fields.First(
                            f => f.OriginalName == field.OriginalName);
                        generate(specializedField, specialization, field.QualifiedType);
                        if (isVoid)
                            gen.WriteLine("return;");
                        gen.UnindentAndWriteCloseBrace();
                    }
                    ThrowException(gen, @class);
                }
                else
                {
                    var specialization = @class.Specializations[0];
                    var specializedField = specialization.Fields.First(
                        f => f.OriginalName == field.OriginalName);
                    generate(specializedField, specialization, field.QualifiedType);
                }
            }
            else
            {
                generate(field, @class.IsDependent ? @class.Specializations[0] : @class,
                    field.QualifiedType);
            }
        }

        public static void GenerateMember(this CSharpSources gen,
            Class @class, Action<Class> generate, bool isVoid = false)
        {
            if (@class != null && @class.IsDependent)
            {
                foreach (var parameter in @class.TemplateParameters)
                    gen.WriteLine($"var __{parameter.Name} = typeof({parameter.Name});");

                foreach (var specialization in @class.Specializations.Where(s => s.IsGenerated))
                {
                    WriteTemplateSpecializationCheck(gen, @class, specialization);
                    gen.WriteOpenBraceAndIndent();
                    generate(specialization);
                    if (isVoid)
                        gen.WriteLine("return;");
                    gen.UnindentAndWriteCloseBrace();
                }
                ThrowException(gen, @class);
            }
            else
            {
                generate(@class);
            }
        }

        private static void DisableSingleTypeMap(Class mapped, BindingContext context)
        {
            var names = new List<string> { mapped.OriginalName };
            foreach (TypePrintScopeKind kind in Enum.GetValues(typeof(TypePrintScopeKind)))
            {
                var cppTypePrinter = new CppTypePrinter { PrintScopeKind = kind };
                names.Add(mapped.Visit(cppTypePrinter));
            }
            foreach (var name in names.Where(context.TypeMaps.TypeMaps.ContainsKey))
                context.TypeMaps.TypeMaps[name].IsEnabled = false;
        }

        private static void WriteTemplateSpecializationCheck(CSharpSources gen,
            Class @class, ClassTemplateSpecialization specialization)
        {
            gen.WriteLine("if ({0})", string.Join(" && ",
                Enumerable.Range(0, @class.TemplateParameters.Count).Select(
                i => string.Format("__{0}.IsAssignableFrom(typeof({1}))",
                    @class.TemplateParameters[i].Name,
                    specialization.Arguments[i].Type.Type.Desugar()))));
        }

        private static void ThrowException(CSharpSources gen, Class @class)
        {
            var typePrinter = new CSharpTypePrinter(gen.Context);
            var supportedTypes = string.Join(", ",
                @class.Specializations.Where(s => !s.Ignore).Select(s => $@"<{string.Join(", ",
                    s.Arguments.Select(typePrinter.VisitTemplateArgument))}>"));
            var typeArguments = string.Join(", ", @class.TemplateParameters.Select(p => p.Name));
            var managedTypes = string.Join(", ", @class.TemplateParameters.Select(p => $"typeof({p.Name}).FullName"));

            if (managedTypes.Length > 0)
                managedTypes = $@"string.Join("", "", new[] {{ {managedTypes} }})";
            else
                managedTypes = "\"\"";

            gen.WriteLine($"throw new ArgumentOutOfRangeException(\"{typeArguments}\", "
                + $@"{managedTypes}, "
                + $"\"{@class.Visit(typePrinter)} maps a C++ template class and therefore it only supports a limited set of types and their subclasses: {supportedTypes}.\");");
        }
    }
}

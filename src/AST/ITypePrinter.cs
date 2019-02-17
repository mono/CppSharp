﻿using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public enum TypePrinterContextKind
    {
        Normal,
        Template,
        Native,
        Managed
    }

    public enum TypePrintScopeKind
    {
        Local,
        Qualified,
        GlobalQualified
    }

    public enum MarshalKind
    {
        Unknown,
        NativeField,
        GenericDelegate,
        DefaultExpression,
        VTableReturnValue,
        Variable,
        ReturnVariableArray
    }

    public class TypePrinterContext
    {
        public TypePrinterContextKind Kind;
        public MarshalKind MarshalKind;
        public Declaration Declaration;
        public Parameter Parameter;
        public Type Type;

        public TypePrinterContext() : this(TypePrinterContextKind.Normal)
        {
        }

        public TypePrinterContext(TypePrinterContextKind kind)
        {
            Kind = kind;
            MarshalKind = MarshalKind.Unknown;
        }

        public string GetTemplateParameterList()
        {
            if (Kind == TypePrinterContextKind.Template)
            {
                var template = (Template) Declaration;
                return string.Join(", ", template.Parameters.Select(p => p.Name));
            }

            var type = Type.Desugar();
            IEnumerable<TemplateArgument> templateArgs;
            var templateSpecializationType = type as TemplateSpecializationType;
            if (templateSpecializationType != null)
                templateArgs = templateSpecializationType.Arguments;
            else
            {
                var declaration = ((TagType) type).Declaration;
                var specialization = declaration as ClassTemplateSpecialization;
                if (specialization == null)
                    return string.Join(", ",
                        ((Class) declaration).TemplateParameters.Select(t => t.Name));
                templateArgs = ((ClassTemplateSpecialization) declaration).Arguments;
            }

            var paramsList = new List<string>();
            foreach (var arg in templateArgs.Where(a => a.Kind == TemplateArgument.ArgumentKind.Type))
            {
                var argType = arg.Type.Type.IsPointerToPrimitiveType()
                    ? new CILType(typeof(System.IntPtr))
                    : arg.Type.Type;
                paramsList.Add(argType.ToString());
            }

            return string.Join(", ", paramsList);
        }
    }

    public interface ITypePrinter
    {
        string ToString(Type type);
    }

    public interface ITypePrinter<out T> : ITypePrinter, ITypeVisitor<T>
    {
        T VisitParameters(IEnumerable<Parameter> @params, bool hasNames = true);
        T VisitParameter(Parameter param, bool hasName = true);

        T VisitDelegate(FunctionType function);
    }
}

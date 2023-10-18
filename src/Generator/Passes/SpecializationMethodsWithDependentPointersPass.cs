using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass binds methods of class template specialisations
    /// which use dependent pointers as parameters or returned types.
    /// </summary>
    /// <remarks>
    /// <para>If we have this code:</para>
    /// <para>template &lt;typename T&gt;</para>
    /// <para>class Template</para>
    /// <para>{</para>
    /// <para>public:</para>
    /// <para>    const T* function();</para>
    /// <para>};</para>
    /// <para />
    /// <para>the respective C# wrapper returns just T
    /// because C# does not support pointers to type parameters.</para>
    /// <para>This creates mismatching types -
    /// for example, char and const char* which is mapped to string.</para>
    /// </remarks>
    public class SpecializationMethodsWithDependentPointersPass : TranslationUnitPass
    {
        public SpecializationMethodsWithDependentPointersPass()
            => VisitOptions.ResetFlags(VisitFlags.Default);

        public override bool VisitASTContext(ASTContext context)
        {
            base.VisitASTContext(context);
            foreach (var extension in extensions)
            {
                var index = extension.Namespace.Declarations.IndexOf(extension.OriginalClass);
                extension.Namespace.Declarations.Insert(index, extension);
            }
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsDependent || @class.IsInterface ||
                @class.Specializations.All(s => !s.IsGenerated))
                return false;

            var methodsWithDependentPointers = @class.Methods.Where(
                m => !m.Ignore && @class.Properties.All(p => p.SetMethod != m) &&
                    (m.OriginalReturnType.Type.IsDependentPointer() ||
                     m.Parameters.Any(p => p.Type.IsDependentPointer()))).ToList();
            if (!methodsWithDependentPointers.Any())
                return false;

            var hasMethods = false;
            var classExtensions = new Class { Name = $"{@class.Name}Extensions", IsStatic = true };
            foreach (var specialization in @class.Specializations.Where(s => s.IsGenerated))
                foreach (var method in methodsWithDependentPointers.Where(
                    m => m.SynthKind == FunctionSynthKind.None))
                {
                    var specializedMethod = specialization.Methods.FirstOrDefault(
                        m => m.InstantiatedFrom == method);
                    if (specializedMethod == null || specializedMethod.IsOperator)
                        continue;

                    hasMethods = true;

                    Method extensionMethod = GetExtensionMethodForDependentPointer(specializedMethod);
                    classExtensions.Methods.Add(extensionMethod);
                    extensionMethod.Namespace = classExtensions;
                    specializedMethod.ExplicitlyIgnore();
                    method.ExplicitlyIgnore();
                    var property = @class.Properties.FirstOrDefault(
                        p => p.GetMethod == method || p.SetMethod == method);
                    if (property != null)
                    {
                        property.ExplicitlyIgnore();
                        extensionMethod.GenerationKind = GenerationKind.Generate;
                    }
                }

            if (!hasMethods)
                return false;

            classExtensions.Namespace = @class.Namespace;
            classExtensions.OriginalClass = @class;
            extensions.Add(classExtensions);
            return true;
        }

        private static Method GetExtensionMethodForDependentPointer(Method specializedMethod)
        {
            var extensionMethod = new Method(specializedMethod);

            foreach (var parameter in extensionMethod.Parameters)
            {
                var qualType = parameter.QualifiedType;
                RemoveTemplateSubstitution(ref qualType);
                parameter.QualifiedType = qualType;
            }

            if (!specializedMethod.IsStatic)
            {
                if (specializedMethod.IsConstructor)
                {
                    extensionMethod.Name = specializedMethod.Namespace.Name;
                    if (extensionMethod.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
                        extensionMethod.OriginalReturnType = new QualifiedType(new PointerType(
                            new QualifiedType(new TagType(specializedMethod.Namespace))));
                }
                else
                {
                    var thisParameter = new Parameter();
                    thisParameter.QualifiedType = new QualifiedType(new PointerType(
                        new QualifiedType(new TagType(specializedMethod.Namespace))));
                    thisParameter.Name = "this";
                    thisParameter.Kind = ParameterKind.Extension;
                    thisParameter.Namespace = extensionMethod;
                    extensionMethod.Parameters.Insert(0, thisParameter);
                }
            }

            specializedMethod.Name = specializedMethod.OriginalName;
            extensionMethod.Name = extensionMethod.OriginalName;
            extensionMethod.OriginalFunction = specializedMethod;
            extensionMethod.Kind = CXXMethodKind.Normal;
            extensionMethod.IsStatic = true;
            if (extensionMethod.IsPure)
            {
                extensionMethod.IsPure = false;
                extensionMethod.SynthKind = FunctionSynthKind.AbstractImplCall;
            }

            var qualReturnType = extensionMethod.OriginalReturnType;
            RemoveTemplateSubstitution(ref qualReturnType);
            extensionMethod.OriginalReturnType = qualReturnType;

            return extensionMethod;
        }

        private static void RemoveTemplateSubstitution(ref QualifiedType qualType)
        {
            var substitution = qualType.Type as TemplateParameterSubstitutionType;
            if (substitution != null)
                qualType.Type = substitution.Replacement.Type;
            else
            {
                var type = qualType.Type.Desugar();
                while (type.IsAddress())
                {
                    var pointee = ((PointerType)type).Pointee.Desugar(
                        resolveTemplateSubstitution: false);
                    if (pointee.IsAddress())
                        type = pointee;
                    else
                    {
                        substitution = pointee as TemplateParameterSubstitutionType;
                        if (substitution != null)
                            ((PointerType)type).QualifiedPointee.Type = substitution.Replacement.Type;
                        break;
                    }
                }
            }
        }

        private List<Class> extensions = new List<Class>();
    }
}

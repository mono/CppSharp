using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        private int uniqueName;

        string CheckName(string name)
        {
            // Generate a new name if the decl still does not have a name
            if (string.IsNullOrWhiteSpace(name))
                return string.Format("_{0}", uniqueName++);

            // Clean up the item name if the first digit is not a valid name.
            if (char.IsNumber(name[0]))
                return '_' + name;

            if (Driver.Options.IsCLIGenerator)
                return CLITextTemplate.SafeIdentifier(name);
            return Helpers.SafeIdentifier(name);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            // Do not clean up namespace names since it can mess up with the
            // names of anonymous or the global namespace.
            if (decl is Namespace)
                return true;

            // types with empty names are assumed to be private
            if (decl is Class && string.IsNullOrWhiteSpace(decl.Name))
            {
                decl.Name = decl.Namespace.Name == "_" ? "__" : "_";
                decl.ExplicitlyIgnore();
                return true;
            }

            Function function = decl as Function;
            if ((function == null || !function.IsOperator) && !(decl is Enumeration))
                decl.Name = CheckName(decl.Name);

            StringHelpers.CleanupText(ref decl.DebugText);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return VisitDeclaration(parameter);
        }

        public override bool VisitClassDecl(Class @class)
        {
            var currentUniqueName = uniqueName;
            uniqueName = 0;
            base.VisitClassDecl(@class);
            uniqueName = currentUniqueName;

            if (@class.Namespace.Classes.Any(d => d != @class && d.Name == @class.Name))
            {
                StringBuilder str = new StringBuilder();
                str.Append(@class.Name);
                do
                {
                    str.Append('_');
                } while (@class.Classes.Any(d => d != @class && d.Name == str.ToString()));
                @class.Name = str.ToString();
            }

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            var currentUniqueName = uniqueName;
            uniqueName = 0;
            var ret = base.VisitFunctionDecl(function);
            uniqueName = currentUniqueName;

            return ret;
        }

        public override bool VisitEvent(Event @event)
        {
            var currentUniqueName = uniqueName;
            uniqueName = 0;
            var ret = base.VisitEvent(@event);
            uniqueName = currentUniqueName;

            return ret;
        }

        public override bool VisitFunctionType(FunctionType type,
            TypeQualifiers quals)
        {
            var currentUniqueName = this.uniqueName;
            this.uniqueName = 0;
            var ret = base.VisitFunctionType(type, quals);
            this.uniqueName = currentUniqueName;

            return ret;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (base.VisitTypedefDecl(typedef))
                return false;

            var @class = typedef.Namespace.FindClass(typedef.Name);

            // Clang will walk the typedef'd tag decl and the typedef decl,
            // so we ignore the class and process just the typedef.

            if (@class != null)
                typedef.ExplicitlyIgnore();

            if (typedef.Type == null)
                typedef.ExplicitlyIgnore();

            return true;
        }

        private void CheckEnumName(Enumeration @enum)
        {
            // If we still do not have a valid name, then try to guess one
            // based on the enum value names.

            if (!string.IsNullOrWhiteSpace(@enum.Name))
                return;

            var prefix = @enum.Items.Select(item => item.Name)
                .ToArray().CommonPrefix();

            // Try a simple heuristic to make sure we end up with a valid name.
            if (prefix.Length < 3)
            {
                @enum.Name = CheckName(@enum.Name);
                return;
            }

            prefix = prefix.Trim().Trim('_');
            @enum.Name = prefix;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            CheckEnumName(@enum);
            return true;
        }

        public override bool VisitEnumItem(Enumeration.Item item)
        {
            if (!base.VisitEnumItem(item))
                return false;

            item.Name = CheckName(item.Name);
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!base.VisitFieldDecl(field))
                return false;

            if (field.Class.Fields.Count(c => c.Name.Equals(field.Name)) > 1)
            {
                StringBuilder str = new StringBuilder();
                str.Append(field.Name);
                do
                {
                    str.Append('_');
                } while (field.Class.Fields.Any(c => c.Name.Equals(str.ToString())));
                field.Name = str.ToString();
            }
            return true;
        }
    }
}


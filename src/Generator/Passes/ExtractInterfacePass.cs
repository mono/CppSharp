using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ExtractInterfacePass : MultipleInheritancePass
    {
        /// <summary>
        /// Creates interface from generated classes
        /// </summary>

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated)
                return false;

            if (@class.IsInterface)
            {
                return false;
            }

            classesWithSecondaryBases.Add(@class);
            GetInterface(@class);
            return true;
        }
    }
}


namespace CppSharp.AST
{
	public enum DeclarationNameKind
	{
        Identifier,
        ObjCZeroArgSelector,
        ObjCOneArgSelector,
        CXXConstructorName,
        CXXDestructorName,
        CXXConversionFunctionName,
        CXXOperatorName,
        CXXDeductionGuideName,
        CXXLiteralOperatorName,
        CXXUsingDirective,
        ObjCMultiArgSelector,
    }

	public class DeclarationName
	{
        public DeclarationNameKind Kind { get; set; }
        public string Identifier { get; set; }
    }
}

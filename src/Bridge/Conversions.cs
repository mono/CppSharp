namespace Cxxi
{
    public enum TypeConversionKind
    {
        None,
        RawPtrToIntPtr,
        ConstCharPtrToString,
    }

    public enum MethodConversionKind
    {
        None,
        FunctionToInstanceMethod,
        FunctionToStaticMethod
    }
}

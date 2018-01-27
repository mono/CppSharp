# CppSharp 0.8.17 25.12.2017



Do not generate wrappers for template specializations if original method in template class is ignored.

Add one more include path which Linux usually expects.

Evaluate expressions for enums generated using GenerateEnumFromMacros

Evaluate expressions when generating enum from macros - ExpressionEvaluator taken from https://github.com/codingseb/ExpressionEvaluator

Set the name-space for enums generated from macros.

Preliminary script for building 32-bit Nuget package

Field property getter returns non-value types by reference instead of by copy.

Update VS check when downloading pre-compiled LLVM packages.

Add `IgnoreConversionToProperty(pattern)` and `ForceConversionToProperty(pattern)`.

Add `UsePropertyDetectionHeuristics` option to `DriverOptions`.

Add "run" to verbs.txt

Added support for 16-bit wide characters (char16_t).

Fixed the generated C++ for symbols when protected classes need them.

Removed the possibility for conflicts between overloads when generating C++ for symbols.
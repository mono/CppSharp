/*===- TableGen'erated file -------------------------------------*- C++ -*-===*\
|*                                                                            *|
|* List of AST nodes of a particular kind                                     *|
|*                                                                            *|
|* Automatically generated file, do not edit!                                 *|
|*                                                                            *|
\*===----------------------------------------------------------------------===*/

#ifndef ABSTRACT_STMT
#  define ABSTRACT_STMT(Type) Type
#endif
#ifndef STMT_RANGE
#  define STMT_RANGE(Base, First, Last)
#endif

#ifndef LAST_STMT_RANGE
#  define LAST_STMT_RANGE(Base, First, Last) STMT_RANGE(Base, First, Last)
#endif

#ifndef ASMSTMT
#  define ASMSTMT(Type, Base) STMT(Type, Base)
#endif
ABSTRACT_STMT(ASMSTMT(AsmStmt, Stmt))
#ifndef GCCASMSTMT
#  define GCCASMSTMT(Type, Base) ASMSTMT(Type, Base)
#endif
GCCASMSTMT(GCCAsmStmt, AsmStmt)
#undef GCCASMSTMT

#ifndef MSASMSTMT
#  define MSASMSTMT(Type, Base) ASMSTMT(Type, Base)
#endif
MSASMSTMT(MSAsmStmt, AsmStmt)
#undef MSASMSTMT

STMT_RANGE(AsmStmt, GCCAsmStmt, MSAsmStmt)

#undef ASMSTMT

#ifndef BREAKSTMT
#  define BREAKSTMT(Type, Base) STMT(Type, Base)
#endif
BREAKSTMT(BreakStmt, Stmt)
#undef BREAKSTMT

#ifndef CXXCATCHSTMT
#  define CXXCATCHSTMT(Type, Base) STMT(Type, Base)
#endif
CXXCATCHSTMT(CXXCatchStmt, Stmt)
#undef CXXCATCHSTMT

#ifndef CXXFORRANGESTMT
#  define CXXFORRANGESTMT(Type, Base) STMT(Type, Base)
#endif
CXXFORRANGESTMT(CXXForRangeStmt, Stmt)
#undef CXXFORRANGESTMT

#ifndef CXXTRYSTMT
#  define CXXTRYSTMT(Type, Base) STMT(Type, Base)
#endif
CXXTRYSTMT(CXXTryStmt, Stmt)
#undef CXXTRYSTMT

#ifndef CAPTUREDSTMT
#  define CAPTUREDSTMT(Type, Base) STMT(Type, Base)
#endif
CAPTUREDSTMT(CapturedStmt, Stmt)
#undef CAPTUREDSTMT

#ifndef COMPOUNDSTMT
#  define COMPOUNDSTMT(Type, Base) STMT(Type, Base)
#endif
COMPOUNDSTMT(CompoundStmt, Stmt)
#undef COMPOUNDSTMT

#ifndef CONTINUESTMT
#  define CONTINUESTMT(Type, Base) STMT(Type, Base)
#endif
CONTINUESTMT(ContinueStmt, Stmt)
#undef CONTINUESTMT

#ifndef CORETURNSTMT
#  define CORETURNSTMT(Type, Base) STMT(Type, Base)
#endif
CORETURNSTMT(CoreturnStmt, Stmt)
#undef CORETURNSTMT

#ifndef COROUTINEBODYSTMT
#  define COROUTINEBODYSTMT(Type, Base) STMT(Type, Base)
#endif
COROUTINEBODYSTMT(CoroutineBodyStmt, Stmt)
#undef COROUTINEBODYSTMT

#ifndef DECLSTMT
#  define DECLSTMT(Type, Base) STMT(Type, Base)
#endif
DECLSTMT(DeclStmt, Stmt)
#undef DECLSTMT

#ifndef DOSTMT
#  define DOSTMT(Type, Base) STMT(Type, Base)
#endif
DOSTMT(DoStmt, Stmt)
#undef DOSTMT

#ifndef FORSTMT
#  define FORSTMT(Type, Base) STMT(Type, Base)
#endif
FORSTMT(ForStmt, Stmt)
#undef FORSTMT

#ifndef GOTOSTMT
#  define GOTOSTMT(Type, Base) STMT(Type, Base)
#endif
GOTOSTMT(GotoStmt, Stmt)
#undef GOTOSTMT

#ifndef IFSTMT
#  define IFSTMT(Type, Base) STMT(Type, Base)
#endif
IFSTMT(IfStmt, Stmt)
#undef IFSTMT

#ifndef INDIRECTGOTOSTMT
#  define INDIRECTGOTOSTMT(Type, Base) STMT(Type, Base)
#endif
INDIRECTGOTOSTMT(IndirectGotoStmt, Stmt)
#undef INDIRECTGOTOSTMT

#ifndef MSDEPENDENTEXISTSSTMT
#  define MSDEPENDENTEXISTSSTMT(Type, Base) STMT(Type, Base)
#endif
MSDEPENDENTEXISTSSTMT(MSDependentExistsStmt, Stmt)
#undef MSDEPENDENTEXISTSSTMT

#ifndef NULLSTMT
#  define NULLSTMT(Type, Base) STMT(Type, Base)
#endif
NULLSTMT(NullStmt, Stmt)
#undef NULLSTMT

// Removed OMP nodes
// Removed OBJC nodes

#ifndef RETURNSTMT
#  define RETURNSTMT(Type, Base) STMT(Type, Base)
#endif
RETURNSTMT(ReturnStmt, Stmt)
#undef RETURNSTMT

#ifndef SEHEXCEPTSTMT
#  define SEHEXCEPTSTMT(Type, Base) STMT(Type, Base)
#endif
SEHEXCEPTSTMT(SEHExceptStmt, Stmt)
#undef SEHEXCEPTSTMT

#ifndef SEHFINALLYSTMT
#  define SEHFINALLYSTMT(Type, Base) STMT(Type, Base)
#endif
SEHFINALLYSTMT(SEHFinallyStmt, Stmt)
#undef SEHFINALLYSTMT

#ifndef SEHLEAVESTMT
#  define SEHLEAVESTMT(Type, Base) STMT(Type, Base)
#endif
SEHLEAVESTMT(SEHLeaveStmt, Stmt)
#undef SEHLEAVESTMT

#ifndef SEHTRYSTMT
#  define SEHTRYSTMT(Type, Base) STMT(Type, Base)
#endif
SEHTRYSTMT(SEHTryStmt, Stmt)
#undef SEHTRYSTMT

#ifndef SWITCHCASE
#  define SWITCHCASE(Type, Base) STMT(Type, Base)
#endif
ABSTRACT_STMT(SWITCHCASE(SwitchCase, Stmt))
#ifndef CASESTMT
#  define CASESTMT(Type, Base) SWITCHCASE(Type, Base)
#endif
CASESTMT(CaseStmt, SwitchCase)
#undef CASESTMT

#ifndef DEFAULTSTMT
#  define DEFAULTSTMT(Type, Base) SWITCHCASE(Type, Base)
#endif
DEFAULTSTMT(DefaultStmt, SwitchCase)
#undef DEFAULTSTMT

STMT_RANGE(SwitchCase, CaseStmt, DefaultStmt)

#undef SWITCHCASE

#ifndef SWITCHSTMT
#  define SWITCHSTMT(Type, Base) STMT(Type, Base)
#endif
SWITCHSTMT(SwitchStmt, Stmt)
#undef SWITCHSTMT

#ifndef VALUESTMT
#  define VALUESTMT(Type, Base) STMT(Type, Base)
#endif
ABSTRACT_STMT(VALUESTMT(ValueStmt, Stmt))
#ifndef ATTRIBUTEDSTMT
#  define ATTRIBUTEDSTMT(Type, Base) VALUESTMT(Type, Base)
#endif
ATTRIBUTEDSTMT(AttributedStmt, ValueStmt)
#undef ATTRIBUTEDSTMT

#ifndef EXPR
#  define EXPR(Type, Base) VALUESTMT(Type, Base)
#endif
ABSTRACT_STMT(EXPR(Expr, ValueStmt))
#ifndef ABSTRACTCONDITIONALOPERATOR
#  define ABSTRACTCONDITIONALOPERATOR(Type, Base) EXPR(Type, Base)
#endif
ABSTRACT_STMT(ABSTRACTCONDITIONALOPERATOR(AbstractConditionalOperator, Expr))
#ifndef BINARYCONDITIONALOPERATOR
#  define BINARYCONDITIONALOPERATOR(Type, Base) ABSTRACTCONDITIONALOPERATOR(Type, Base)
#endif
BINARYCONDITIONALOPERATOR(BinaryConditionalOperator, AbstractConditionalOperator)
#undef BINARYCONDITIONALOPERATOR

#ifndef CONDITIONALOPERATOR
#  define CONDITIONALOPERATOR(Type, Base) ABSTRACTCONDITIONALOPERATOR(Type, Base)
#endif
CONDITIONALOPERATOR(ConditionalOperator, AbstractConditionalOperator)
#undef CONDITIONALOPERATOR

STMT_RANGE(AbstractConditionalOperator, BinaryConditionalOperator, ConditionalOperator)

#undef ABSTRACTCONDITIONALOPERATOR

#ifndef ADDRLABELEXPR
#  define ADDRLABELEXPR(Type, Base) EXPR(Type, Base)
#endif
ADDRLABELEXPR(AddrLabelExpr, Expr)
#undef ADDRLABELEXPR

#ifndef ARRAYINITINDEXEXPR
#  define ARRAYINITINDEXEXPR(Type, Base) EXPR(Type, Base)
#endif
ARRAYINITINDEXEXPR(ArrayInitIndexExpr, Expr)
#undef ARRAYINITINDEXEXPR

#ifndef ARRAYINITLOOPEXPR
#  define ARRAYINITLOOPEXPR(Type, Base) EXPR(Type, Base)
#endif
ARRAYINITLOOPEXPR(ArrayInitLoopExpr, Expr)
#undef ARRAYINITLOOPEXPR

#ifndef ARRAYSUBSCRIPTEXPR
#  define ARRAYSUBSCRIPTEXPR(Type, Base) EXPR(Type, Base)
#endif
ARRAYSUBSCRIPTEXPR(ArraySubscriptExpr, Expr)
#undef ARRAYSUBSCRIPTEXPR

#ifndef ARRAYTYPETRAITEXPR
#  define ARRAYTYPETRAITEXPR(Type, Base) EXPR(Type, Base)
#endif
ARRAYTYPETRAITEXPR(ArrayTypeTraitExpr, Expr)
#undef ARRAYTYPETRAITEXPR

#ifndef ASTYPEEXPR
#  define ASTYPEEXPR(Type, Base) EXPR(Type, Base)
#endif
ASTYPEEXPR(AsTypeExpr, Expr)
#undef ASTYPEEXPR

#ifndef ATOMICEXPR
#  define ATOMICEXPR(Type, Base) EXPR(Type, Base)
#endif
ATOMICEXPR(AtomicExpr, Expr)
#undef ATOMICEXPR

#ifndef BINARYOPERATOR
#  define BINARYOPERATOR(Type, Base) EXPR(Type, Base)
#endif
BINARYOPERATOR(BinaryOperator, Expr)
#ifndef COMPOUNDASSIGNOPERATOR
#  define COMPOUNDASSIGNOPERATOR(Type, Base) BINARYOPERATOR(Type, Base)
#endif
COMPOUNDASSIGNOPERATOR(CompoundAssignOperator, BinaryOperator)
#undef COMPOUNDASSIGNOPERATOR

STMT_RANGE(BinaryOperator, BinaryOperator, CompoundAssignOperator)

#undef BINARYOPERATOR

#ifndef BLOCKEXPR
#  define BLOCKEXPR(Type, Base) EXPR(Type, Base)
#endif
BLOCKEXPR(BlockExpr, Expr)
#undef BLOCKEXPR

#ifndef CXXBINDTEMPORARYEXPR
#  define CXXBINDTEMPORARYEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXBINDTEMPORARYEXPR(CXXBindTemporaryExpr, Expr)
#undef CXXBINDTEMPORARYEXPR

#ifndef CXXBOOLLITERALEXPR
#  define CXXBOOLLITERALEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXBOOLLITERALEXPR(CXXBoolLiteralExpr, Expr)
#undef CXXBOOLLITERALEXPR

#ifndef CXXCONSTRUCTEXPR
#  define CXXCONSTRUCTEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXCONSTRUCTEXPR(CXXConstructExpr, Expr)
#ifndef CXXTEMPORARYOBJECTEXPR
#  define CXXTEMPORARYOBJECTEXPR(Type, Base) CXXCONSTRUCTEXPR(Type, Base)
#endif
CXXTEMPORARYOBJECTEXPR(CXXTemporaryObjectExpr, CXXConstructExpr)
#undef CXXTEMPORARYOBJECTEXPR

STMT_RANGE(CXXConstructExpr, CXXConstructExpr, CXXTemporaryObjectExpr)

#undef CXXCONSTRUCTEXPR

#ifndef CXXDEFAULTARGEXPR
#  define CXXDEFAULTARGEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXDEFAULTARGEXPR(CXXDefaultArgExpr, Expr)
#undef CXXDEFAULTARGEXPR

#ifndef CXXDEFAULTINITEXPR
#  define CXXDEFAULTINITEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXDEFAULTINITEXPR(CXXDefaultInitExpr, Expr)
#undef CXXDEFAULTINITEXPR

#ifndef CXXDELETEEXPR
#  define CXXDELETEEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXDELETEEXPR(CXXDeleteExpr, Expr)
#undef CXXDELETEEXPR

#ifndef CXXDEPENDENTSCOPEMEMBEREXPR
#  define CXXDEPENDENTSCOPEMEMBEREXPR(Type, Base) EXPR(Type, Base)
#endif
CXXDEPENDENTSCOPEMEMBEREXPR(CXXDependentScopeMemberExpr, Expr)
#undef CXXDEPENDENTSCOPEMEMBEREXPR

#ifndef CXXFOLDEXPR
#  define CXXFOLDEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXFOLDEXPR(CXXFoldExpr, Expr)
#undef CXXFOLDEXPR

#ifndef CXXINHERITEDCTORINITEXPR
#  define CXXINHERITEDCTORINITEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXINHERITEDCTORINITEXPR(CXXInheritedCtorInitExpr, Expr)
#undef CXXINHERITEDCTORINITEXPR

#ifndef CXXNEWEXPR
#  define CXXNEWEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXNEWEXPR(CXXNewExpr, Expr)
#undef CXXNEWEXPR

#ifndef CXXNOEXCEPTEXPR
#  define CXXNOEXCEPTEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXNOEXCEPTEXPR(CXXNoexceptExpr, Expr)
#undef CXXNOEXCEPTEXPR

#ifndef CXXNULLPTRLITERALEXPR
#  define CXXNULLPTRLITERALEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXNULLPTRLITERALEXPR(CXXNullPtrLiteralExpr, Expr)
#undef CXXNULLPTRLITERALEXPR

#ifndef CXXPARENLISTINITEXPR
#  define CXXPARENLISTINITEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXPARENLISTINITEXPR(CXXParenListInitExpr, Expr)
#undef CXXPARENLISTINITEXPR

#ifndef CXXPSEUDODESTRUCTOREXPR
#  define CXXPSEUDODESTRUCTOREXPR(Type, Base) EXPR(Type, Base)
#endif
CXXPSEUDODESTRUCTOREXPR(CXXPseudoDestructorExpr, Expr)
#undef CXXPSEUDODESTRUCTOREXPR

#ifndef CXXREWRITTENBINARYOPERATOR
#  define CXXREWRITTENBINARYOPERATOR(Type, Base) EXPR(Type, Base)
#endif
CXXREWRITTENBINARYOPERATOR(CXXRewrittenBinaryOperator, Expr)
#undef CXXREWRITTENBINARYOPERATOR

#ifndef CXXSCALARVALUEINITEXPR
#  define CXXSCALARVALUEINITEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXSCALARVALUEINITEXPR(CXXScalarValueInitExpr, Expr)
#undef CXXSCALARVALUEINITEXPR

#ifndef CXXSTDINITIALIZERLISTEXPR
#  define CXXSTDINITIALIZERLISTEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXSTDINITIALIZERLISTEXPR(CXXStdInitializerListExpr, Expr)
#undef CXXSTDINITIALIZERLISTEXPR

#ifndef CXXTHISEXPR
#  define CXXTHISEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXTHISEXPR(CXXThisExpr, Expr)
#undef CXXTHISEXPR

#ifndef CXXTHROWEXPR
#  define CXXTHROWEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXTHROWEXPR(CXXThrowExpr, Expr)
#undef CXXTHROWEXPR

#ifndef CXXTYPEIDEXPR
#  define CXXTYPEIDEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXTYPEIDEXPR(CXXTypeidExpr, Expr)
#undef CXXTYPEIDEXPR

#ifndef CXXUNRESOLVEDCONSTRUCTEXPR
#  define CXXUNRESOLVEDCONSTRUCTEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXUNRESOLVEDCONSTRUCTEXPR(CXXUnresolvedConstructExpr, Expr)
#undef CXXUNRESOLVEDCONSTRUCTEXPR

#ifndef CXXUUIDOFEXPR
#  define CXXUUIDOFEXPR(Type, Base) EXPR(Type, Base)
#endif
CXXUUIDOFEXPR(CXXUuidofExpr, Expr)
#undef CXXUUIDOFEXPR

#ifndef CALLEXPR
#  define CALLEXPR(Type, Base) EXPR(Type, Base)
#endif
CALLEXPR(CallExpr, Expr)
#ifndef CUDAKERNELCALLEXPR
#  define CUDAKERNELCALLEXPR(Type, Base) CALLEXPR(Type, Base)
#endif
CUDAKERNELCALLEXPR(CUDAKernelCallExpr, CallExpr)
#undef CUDAKERNELCALLEXPR

#ifndef CXXMEMBERCALLEXPR
#  define CXXMEMBERCALLEXPR(Type, Base) CALLEXPR(Type, Base)
#endif
CXXMEMBERCALLEXPR(CXXMemberCallExpr, CallExpr)
#undef CXXMEMBERCALLEXPR

#ifndef CXXOPERATORCALLEXPR
#  define CXXOPERATORCALLEXPR(Type, Base) CALLEXPR(Type, Base)
#endif
CXXOPERATORCALLEXPR(CXXOperatorCallExpr, CallExpr)
#undef CXXOPERATORCALLEXPR

#ifndef USERDEFINEDLITERAL
#  define USERDEFINEDLITERAL(Type, Base) CALLEXPR(Type, Base)
#endif
USERDEFINEDLITERAL(UserDefinedLiteral, CallExpr)
#undef USERDEFINEDLITERAL

STMT_RANGE(CallExpr, CallExpr, UserDefinedLiteral)

#undef CALLEXPR

#ifndef CASTEXPR
#  define CASTEXPR(Type, Base) EXPR(Type, Base)
#endif
ABSTRACT_STMT(CASTEXPR(CastExpr, Expr))
#ifndef EXPLICITCASTEXPR
#  define EXPLICITCASTEXPR(Type, Base) CASTEXPR(Type, Base)
#endif
ABSTRACT_STMT(EXPLICITCASTEXPR(ExplicitCastExpr, CastExpr))
#ifndef BUILTINBITCASTEXPR
#  define BUILTINBITCASTEXPR(Type, Base) EXPLICITCASTEXPR(Type, Base)
#endif
BUILTINBITCASTEXPR(BuiltinBitCastExpr, ExplicitCastExpr)
#undef BUILTINBITCASTEXPR

#ifndef CSTYLECASTEXPR
#  define CSTYLECASTEXPR(Type, Base) EXPLICITCASTEXPR(Type, Base)
#endif
CSTYLECASTEXPR(CStyleCastExpr, ExplicitCastExpr)
#undef CSTYLECASTEXPR

#ifndef CXXFUNCTIONALCASTEXPR
#  define CXXFUNCTIONALCASTEXPR(Type, Base) EXPLICITCASTEXPR(Type, Base)
#endif
CXXFUNCTIONALCASTEXPR(CXXFunctionalCastExpr, ExplicitCastExpr)
#undef CXXFUNCTIONALCASTEXPR

#ifndef CXXNAMEDCASTEXPR
#  define CXXNAMEDCASTEXPR(Type, Base) EXPLICITCASTEXPR(Type, Base)
#endif
ABSTRACT_STMT(CXXNAMEDCASTEXPR(CXXNamedCastExpr, ExplicitCastExpr))
#ifndef CXXADDRSPACECASTEXPR
#  define CXXADDRSPACECASTEXPR(Type, Base) CXXNAMEDCASTEXPR(Type, Base)
#endif
CXXADDRSPACECASTEXPR(CXXAddrspaceCastExpr, CXXNamedCastExpr)
#undef CXXADDRSPACECASTEXPR

#ifndef CXXCONSTCASTEXPR
#  define CXXCONSTCASTEXPR(Type, Base) CXXNAMEDCASTEXPR(Type, Base)
#endif
CXXCONSTCASTEXPR(CXXConstCastExpr, CXXNamedCastExpr)
#undef CXXCONSTCASTEXPR

#ifndef CXXDYNAMICCASTEXPR
#  define CXXDYNAMICCASTEXPR(Type, Base) CXXNAMEDCASTEXPR(Type, Base)
#endif
CXXDYNAMICCASTEXPR(CXXDynamicCastExpr, CXXNamedCastExpr)
#undef CXXDYNAMICCASTEXPR

#ifndef CXXREINTERPRETCASTEXPR
#  define CXXREINTERPRETCASTEXPR(Type, Base) CXXNAMEDCASTEXPR(Type, Base)
#endif
CXXREINTERPRETCASTEXPR(CXXReinterpretCastExpr, CXXNamedCastExpr)
#undef CXXREINTERPRETCASTEXPR

#ifndef CXXSTATICCASTEXPR
#  define CXXSTATICCASTEXPR(Type, Base) CXXNAMEDCASTEXPR(Type, Base)
#endif
CXXSTATICCASTEXPR(CXXStaticCastExpr, CXXNamedCastExpr)
#undef CXXSTATICCASTEXPR

STMT_RANGE(CXXNamedCastExpr, CXXAddrspaceCastExpr, CXXStaticCastExpr)

#undef CXXNAMEDCASTEXPR

STMT_RANGE(ExplicitCastExpr, BuiltinBitCastExpr, ObjCBridgedCastExpr)

#undef EXPLICITCASTEXPR

#ifndef IMPLICITCASTEXPR
#  define IMPLICITCASTEXPR(Type, Base) CASTEXPR(Type, Base)
#endif
IMPLICITCASTEXPR(ImplicitCastExpr, CastExpr)
#undef IMPLICITCASTEXPR

STMT_RANGE(CastExpr, BuiltinBitCastExpr, ImplicitCastExpr)

#undef CASTEXPR

#ifndef CHARACTERLITERAL
#  define CHARACTERLITERAL(Type, Base) EXPR(Type, Base)
#endif
CHARACTERLITERAL(CharacterLiteral, Expr)
#undef CHARACTERLITERAL

#ifndef CHOOSEEXPR
#  define CHOOSEEXPR(Type, Base) EXPR(Type, Base)
#endif
CHOOSEEXPR(ChooseExpr, Expr)
#undef CHOOSEEXPR

#ifndef COMPOUNDLITERALEXPR
#  define COMPOUNDLITERALEXPR(Type, Base) EXPR(Type, Base)
#endif
COMPOUNDLITERALEXPR(CompoundLiteralExpr, Expr)
#undef COMPOUNDLITERALEXPR

#ifndef CONCEPTSPECIALIZATIONEXPR
#  define CONCEPTSPECIALIZATIONEXPR(Type, Base) EXPR(Type, Base)
#endif
CONCEPTSPECIALIZATIONEXPR(ConceptSpecializationExpr, Expr)
#undef CONCEPTSPECIALIZATIONEXPR

#ifndef CONVERTVECTOREXPR
#  define CONVERTVECTOREXPR(Type, Base) EXPR(Type, Base)
#endif
CONVERTVECTOREXPR(ConvertVectorExpr, Expr)
#undef CONVERTVECTOREXPR

#ifndef COROUTINESUSPENDEXPR
#  define COROUTINESUSPENDEXPR(Type, Base) EXPR(Type, Base)
#endif
ABSTRACT_STMT(COROUTINESUSPENDEXPR(CoroutineSuspendExpr, Expr))
#ifndef COAWAITEXPR
#  define COAWAITEXPR(Type, Base) COROUTINESUSPENDEXPR(Type, Base)
#endif
COAWAITEXPR(CoawaitExpr, CoroutineSuspendExpr)
#undef COAWAITEXPR

#ifndef COYIELDEXPR
#  define COYIELDEXPR(Type, Base) COROUTINESUSPENDEXPR(Type, Base)
#endif
COYIELDEXPR(CoyieldExpr, CoroutineSuspendExpr)
#undef COYIELDEXPR

STMT_RANGE(CoroutineSuspendExpr, CoawaitExpr, CoyieldExpr)

#undef COROUTINESUSPENDEXPR

#ifndef DECLREFEXPR
#  define DECLREFEXPR(Type, Base) EXPR(Type, Base)
#endif
DECLREFEXPR(DeclRefExpr, Expr)
#undef DECLREFEXPR

#ifndef DEPENDENTCOAWAITEXPR
#  define DEPENDENTCOAWAITEXPR(Type, Base) EXPR(Type, Base)
#endif
DEPENDENTCOAWAITEXPR(DependentCoawaitExpr, Expr)
#undef DEPENDENTCOAWAITEXPR

#ifndef DEPENDENTSCOPEDECLREFEXPR
#  define DEPENDENTSCOPEDECLREFEXPR(Type, Base) EXPR(Type, Base)
#endif
DEPENDENTSCOPEDECLREFEXPR(DependentScopeDeclRefExpr, Expr)
#undef DEPENDENTSCOPEDECLREFEXPR

#ifndef DESIGNATEDINITEXPR
#  define DESIGNATEDINITEXPR(Type, Base) EXPR(Type, Base)
#endif
DESIGNATEDINITEXPR(DesignatedInitExpr, Expr)
#undef DESIGNATEDINITEXPR

#ifndef DESIGNATEDINITUPDATEEXPR
#  define DESIGNATEDINITUPDATEEXPR(Type, Base) EXPR(Type, Base)
#endif
DESIGNATEDINITUPDATEEXPR(DesignatedInitUpdateExpr, Expr)
#undef DESIGNATEDINITUPDATEEXPR

#ifndef EXPRESSIONTRAITEXPR
#  define EXPRESSIONTRAITEXPR(Type, Base) EXPR(Type, Base)
#endif
EXPRESSIONTRAITEXPR(ExpressionTraitExpr, Expr)
#undef EXPRESSIONTRAITEXPR

#ifndef EXTVECTORELEMENTEXPR
#  define EXTVECTORELEMENTEXPR(Type, Base) EXPR(Type, Base)
#endif
EXTVECTORELEMENTEXPR(ExtVectorElementExpr, Expr)
#undef EXTVECTORELEMENTEXPR

#ifndef FIXEDPOINTLITERAL
#  define FIXEDPOINTLITERAL(Type, Base) EXPR(Type, Base)
#endif
FIXEDPOINTLITERAL(FixedPointLiteral, Expr)
#undef FIXEDPOINTLITERAL

#ifndef FLOATINGLITERAL
#  define FLOATINGLITERAL(Type, Base) EXPR(Type, Base)
#endif
FLOATINGLITERAL(FloatingLiteral, Expr)
#undef FLOATINGLITERAL

#ifndef FULLEXPR
#  define FULLEXPR(Type, Base) EXPR(Type, Base)
#endif
ABSTRACT_STMT(FULLEXPR(FullExpr, Expr))
#ifndef CONSTANTEXPR
#  define CONSTANTEXPR(Type, Base) FULLEXPR(Type, Base)
#endif
CONSTANTEXPR(ConstantExpr, FullExpr)
#undef CONSTANTEXPR

#ifndef EXPRWITHCLEANUPS
#  define EXPRWITHCLEANUPS(Type, Base) FULLEXPR(Type, Base)
#endif
EXPRWITHCLEANUPS(ExprWithCleanups, FullExpr)
#undef EXPRWITHCLEANUPS

STMT_RANGE(FullExpr, ConstantExpr, ExprWithCleanups)

#undef FULLEXPR

#ifndef FUNCTIONPARMPACKEXPR
#  define FUNCTIONPARMPACKEXPR(Type, Base) EXPR(Type, Base)
#endif
FUNCTIONPARMPACKEXPR(FunctionParmPackExpr, Expr)
#undef FUNCTIONPARMPACKEXPR

#ifndef GNUNULLEXPR
#  define GNUNULLEXPR(Type, Base) EXPR(Type, Base)
#endif
GNUNULLEXPR(GNUNullExpr, Expr)
#undef GNUNULLEXPR

#ifndef GENERICSELECTIONEXPR
#  define GENERICSELECTIONEXPR(Type, Base) EXPR(Type, Base)
#endif
GENERICSELECTIONEXPR(GenericSelectionExpr, Expr)
#undef GENERICSELECTIONEXPR

#ifndef IMAGINARYLITERAL
#  define IMAGINARYLITERAL(Type, Base) EXPR(Type, Base)
#endif
IMAGINARYLITERAL(ImaginaryLiteral, Expr)
#undef IMAGINARYLITERAL

#ifndef IMPLICITVALUEINITEXPR
#  define IMPLICITVALUEINITEXPR(Type, Base) EXPR(Type, Base)
#endif
IMPLICITVALUEINITEXPR(ImplicitValueInitExpr, Expr)
#undef IMPLICITVALUEINITEXPR

#ifndef INITLISTEXPR
#  define INITLISTEXPR(Type, Base) EXPR(Type, Base)
#endif
INITLISTEXPR(InitListExpr, Expr)
#undef INITLISTEXPR

#ifndef INTEGERLITERAL
#  define INTEGERLITERAL(Type, Base) EXPR(Type, Base)
#endif
INTEGERLITERAL(IntegerLiteral, Expr)
#undef INTEGERLITERAL

#ifndef LAMBDAEXPR
#  define LAMBDAEXPR(Type, Base) EXPR(Type, Base)
#endif
LAMBDAEXPR(LambdaExpr, Expr)
#undef LAMBDAEXPR

#ifndef MSPROPERTYREFEXPR
#  define MSPROPERTYREFEXPR(Type, Base) EXPR(Type, Base)
#endif
MSPROPERTYREFEXPR(MSPropertyRefExpr, Expr)
#undef MSPROPERTYREFEXPR

#ifndef MSPROPERTYSUBSCRIPTEXPR
#  define MSPROPERTYSUBSCRIPTEXPR(Type, Base) EXPR(Type, Base)
#endif
MSPROPERTYSUBSCRIPTEXPR(MSPropertySubscriptExpr, Expr)
#undef MSPROPERTYSUBSCRIPTEXPR

#ifndef MATERIALIZETEMPORARYEXPR
#  define MATERIALIZETEMPORARYEXPR(Type, Base) EXPR(Type, Base)
#endif
MATERIALIZETEMPORARYEXPR(MaterializeTemporaryExpr, Expr)
#undef MATERIALIZETEMPORARYEXPR

#ifndef MATRIXSUBSCRIPTEXPR
#  define MATRIXSUBSCRIPTEXPR(Type, Base) EXPR(Type, Base)
#endif
MATRIXSUBSCRIPTEXPR(MatrixSubscriptExpr, Expr)
#undef MATRIXSUBSCRIPTEXPR

#ifndef MEMBEREXPR
#  define MEMBEREXPR(Type, Base) EXPR(Type, Base)
#endif
MEMBEREXPR(MemberExpr, Expr)
#undef MEMBEREXPR

#ifndef NOINITEXPR
#  define NOINITEXPR(Type, Base) EXPR(Type, Base)
#endif
NOINITEXPR(NoInitExpr, Expr)
#undef NOINITEXPR

// Removed OMP nodes
// Removed OBJC nodes

#ifndef OFFSETOFEXPR
#  define OFFSETOFEXPR(Type, Base) EXPR(Type, Base)
#endif
OFFSETOFEXPR(OffsetOfExpr, Expr)
#undef OFFSETOFEXPR

#ifndef OPAQUEVALUEEXPR
#  define OPAQUEVALUEEXPR(Type, Base) EXPR(Type, Base)
#endif
OPAQUEVALUEEXPR(OpaqueValueExpr, Expr)
#undef OPAQUEVALUEEXPR

#ifndef OVERLOADEXPR
#  define OVERLOADEXPR(Type, Base) EXPR(Type, Base)
#endif
ABSTRACT_STMT(OVERLOADEXPR(OverloadExpr, Expr))
#ifndef UNRESOLVEDLOOKUPEXPR
#  define UNRESOLVEDLOOKUPEXPR(Type, Base) OVERLOADEXPR(Type, Base)
#endif
UNRESOLVEDLOOKUPEXPR(UnresolvedLookupExpr, OverloadExpr)
#undef UNRESOLVEDLOOKUPEXPR

#ifndef UNRESOLVEDMEMBEREXPR
#  define UNRESOLVEDMEMBEREXPR(Type, Base) OVERLOADEXPR(Type, Base)
#endif
UNRESOLVEDMEMBEREXPR(UnresolvedMemberExpr, OverloadExpr)
#undef UNRESOLVEDMEMBEREXPR

STMT_RANGE(OverloadExpr, UnresolvedLookupExpr, UnresolvedMemberExpr)

#undef OVERLOADEXPR

#ifndef PACKEXPANSIONEXPR
#  define PACKEXPANSIONEXPR(Type, Base) EXPR(Type, Base)
#endif
PACKEXPANSIONEXPR(PackExpansionExpr, Expr)
#undef PACKEXPANSIONEXPR

#ifndef PARENEXPR
#  define PARENEXPR(Type, Base) EXPR(Type, Base)
#endif
PARENEXPR(ParenExpr, Expr)
#undef PARENEXPR

#ifndef PARENLISTEXPR
#  define PARENLISTEXPR(Type, Base) EXPR(Type, Base)
#endif
PARENLISTEXPR(ParenListExpr, Expr)
#undef PARENLISTEXPR

#ifndef PREDEFINEDEXPR
#  define PREDEFINEDEXPR(Type, Base) EXPR(Type, Base)
#endif
PREDEFINEDEXPR(PredefinedExpr, Expr)
#undef PREDEFINEDEXPR

#ifndef PSEUDOOBJECTEXPR
#  define PSEUDOOBJECTEXPR(Type, Base) EXPR(Type, Base)
#endif
PSEUDOOBJECTEXPR(PseudoObjectExpr, Expr)
#undef PSEUDOOBJECTEXPR

#ifndef RECOVERYEXPR
#  define RECOVERYEXPR(Type, Base) EXPR(Type, Base)
#endif
RECOVERYEXPR(RecoveryExpr, Expr)
#undef RECOVERYEXPR

#ifndef REQUIRESEXPR
#  define REQUIRESEXPR(Type, Base) EXPR(Type, Base)
#endif
REQUIRESEXPR(RequiresExpr, Expr)
#undef REQUIRESEXPR

#ifndef SYCLUNIQUESTABLENAMEEXPR
#  define SYCLUNIQUESTABLENAMEEXPR(Type, Base) EXPR(Type, Base)
#endif
SYCLUNIQUESTABLENAMEEXPR(SYCLUniqueStableNameExpr, Expr)
#undef SYCLUNIQUESTABLENAMEEXPR

#ifndef SHUFFLEVECTOREXPR
#  define SHUFFLEVECTOREXPR(Type, Base) EXPR(Type, Base)
#endif
SHUFFLEVECTOREXPR(ShuffleVectorExpr, Expr)
#undef SHUFFLEVECTOREXPR

#ifndef SIZEOFPACKEXPR
#  define SIZEOFPACKEXPR(Type, Base) EXPR(Type, Base)
#endif
SIZEOFPACKEXPR(SizeOfPackExpr, Expr)
#undef SIZEOFPACKEXPR

#ifndef SOURCELOCEXPR
#  define SOURCELOCEXPR(Type, Base) EXPR(Type, Base)
#endif
SOURCELOCEXPR(SourceLocExpr, Expr)
#undef SOURCELOCEXPR

#ifndef STMTEXPR
#  define STMTEXPR(Type, Base) EXPR(Type, Base)
#endif
STMTEXPR(StmtExpr, Expr)
#undef STMTEXPR

#ifndef STRINGLITERAL
#  define STRINGLITERAL(Type, Base) EXPR(Type, Base)
#endif
STRINGLITERAL(StringLiteral, Expr)
#undef STRINGLITERAL

#ifndef SUBSTNONTYPETEMPLATEPARMEXPR
#  define SUBSTNONTYPETEMPLATEPARMEXPR(Type, Base) EXPR(Type, Base)
#endif
SUBSTNONTYPETEMPLATEPARMEXPR(SubstNonTypeTemplateParmExpr, Expr)
#undef SUBSTNONTYPETEMPLATEPARMEXPR

#ifndef SUBSTNONTYPETEMPLATEPARMPACKEXPR
#  define SUBSTNONTYPETEMPLATEPARMPACKEXPR(Type, Base) EXPR(Type, Base)
#endif
SUBSTNONTYPETEMPLATEPARMPACKEXPR(SubstNonTypeTemplateParmPackExpr, Expr)
#undef SUBSTNONTYPETEMPLATEPARMPACKEXPR

#ifndef TYPETRAITEXPR
#  define TYPETRAITEXPR(Type, Base) EXPR(Type, Base)
#endif
TYPETRAITEXPR(TypeTraitExpr, Expr)
#undef TYPETRAITEXPR

#ifndef TYPOEXPR
#  define TYPOEXPR(Type, Base) EXPR(Type, Base)
#endif
TYPOEXPR(TypoExpr, Expr)
#undef TYPOEXPR

#ifndef UNARYEXPRORTYPETRAITEXPR
#  define UNARYEXPRORTYPETRAITEXPR(Type, Base) EXPR(Type, Base)
#endif
UNARYEXPRORTYPETRAITEXPR(UnaryExprOrTypeTraitExpr, Expr)
#undef UNARYEXPRORTYPETRAITEXPR

#ifndef UNARYOPERATOR
#  define UNARYOPERATOR(Type, Base) EXPR(Type, Base)
#endif
UNARYOPERATOR(UnaryOperator, Expr)
#undef UNARYOPERATOR

#ifndef VAARGEXPR
#  define VAARGEXPR(Type, Base) EXPR(Type, Base)
#endif
VAARGEXPR(VAArgExpr, Expr)
#undef VAARGEXPR

STMT_RANGE(Expr, BinaryConditionalOperator, VAArgExpr)

#undef EXPR

#ifndef LABELSTMT
#  define LABELSTMT(Type, Base) VALUESTMT(Type, Base)
#endif
LABELSTMT(LabelStmt, ValueStmt)
#undef LABELSTMT

STMT_RANGE(ValueStmt, AttributedStmt, LabelStmt)

#undef VALUESTMT

#ifndef WHILESTMT
#  define WHILESTMT(Type, Base) STMT(Type, Base)
#endif
WHILESTMT(WhileStmt, Stmt)
#undef WHILESTMT

LAST_STMT_RANGE(Stmt, GCCAsmStmt, WhileStmt)

#undef STMT
#undef STMT_RANGE
#undef LAST_STMT_RANGE
#undef ABSTRACT_STMT

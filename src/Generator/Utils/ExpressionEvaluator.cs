using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.RegularExpressions;

/// <summary>
/// This class allow to evaluate a string math or pseudo C# expression 
/// </summary>
internal class ExpressionEvaluator
{
    private static Regex varOrFunctionRegEx = new Regex(@"^(?<inObject>(?<nullConditional>[?])?\.)?(?<name>[a-zA-Z_][a-zA-Z0-9_]*)\s*(?<isfunction>[(])?", RegexOptions.IgnoreCase);
    private static Regex numberRegex = new Regex(@"^(?<sign>[+-])?\d+(?<hasdecimal>\.?\d+(e[+-]?\d+)?)?(?<type>ul|[fdulm])?", RegexOptions.IgnoreCase);
    private static Regex stringBeginningRegex = new Regex("^(?<interpolated>[$])?(?<escaped>[@])?[\"]");
    private static Regex castRegex = new Regex(@"^\(\s*(?<typeName>[a-zA-Z_][a-zA-Z0-9_\.\[\]<>]*[?]?)\s*\)");
    private static Regex indexingBeginningRegex = new Regex(@"^[?]?\[");
    private static Regex primaryTypesRegex = new Regex(@"(?<=^|[^a-zA-Z_])(?<primaryType>object|string|bool[?]?|byte[?]?|char[?]?|decimal[?]?|double[?]?|short[?]?|int[?]?|long[?]?|sbyte[?]?|float[?]?|ushort[?]?|uint[?]?|void)(?=[^a-zA-Z_]|$)");
    private static Regex endOfStringWithDollar = new Regex("^[^\"{]*[\"{]");
    private static Regex endOfStringWithoutDollar = new Regex("^[^\"]*[\"]");
    private static Regex endOfStringInterpolationRegex = new Regex("^[^}\"]*[}\"]");
    private static Regex stringBeginningForEndBlockRegex = new Regex("[$]?[@]?[\"]$");
    private static Regex lambdaExpressionRegex = new Regex(@"^\s*(?<args>(\s*[(]\s*([a-zA-Z_][a-zA-Z0-9_]*\s*([,]\s*[a-zA-Z_][a-zA-Z0-9_]*\s*)*)?[)])|[a-zA-Z_][a-zA-Z0-9_]*)\s*=>(?<expression>.*)$");
    private static Regex lambdaArgRegex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_]*");

    private static BindingFlags instanceBindingFlag = (BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    private static BindingFlags staticBindingFlag = (BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static);

    private static Dictionary<string, Type> PrimaryTypesDict = new Dictionary<string, Type>()
    {
        { "object", typeof(object) },
        { "string", typeof(string) },
        { "bool", typeof(bool) },
        { "bool?", typeof(bool?) },
        { "byte", typeof(byte) },
        { "byte?", typeof(byte?) },
        { "char", typeof(char) },
        { "char?", typeof(char?) },
        { "decimal", typeof(decimal) },
        { "decimal?", typeof(decimal?) },
        { "double", typeof(double) },
        { "double?", typeof(double?) },
        { "short", typeof(short) },
        { "short?", typeof(short?) },
        { "int", typeof(int) },
        { "int?", typeof(int?) },
        { "long", typeof(long) },
        { "long?", typeof(long?) },
        { "sbyte", typeof(sbyte) },
        { "sbyte?", typeof(sbyte?) },
        { "float", typeof(float) },
        { "float?", typeof(float?) },
        { "ushort", typeof(ushort) },
        { "ushort?", typeof(ushort?) },
        { "uint", typeof(uint) },
        { "uint?", typeof(uint?) },
        { "ulong", typeof(ulong) },
        { "ulong?", typeof(ulong?) },
        { "void", typeof(void) }
    };

    private static Dictionary<string, Func<string, object>> numberSuffixToParse = new Dictionary<string, Func<string, object>>()
    {
        { "f", number => float.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) },
        { "d", number => double.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) },
        { "u", number => uint.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) },
        { "l", number => long.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) },
        { "ul", number => ulong.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) },
        { "m", number => decimal.Parse(number, NumberStyles.Any, CultureInfo.InvariantCulture) }
    };

    private static Dictionary<char, string> stringEscapedCharDict = new Dictionary<char, string>()
    {
        { '\\', @"\" },
        { '"', "\"" },
        { '0', "\0" },
        { 'a', "\a" },
        { 'b', "\b" },
        { 'f', "\f" },
        { 'n', "\n" },
        { 'r', "\r" },
        { 't', "\t" },
        { 'v', "\v" }
    };

    private enum ExpressionOperator
    {
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        Lower,
        Greater,
        Equal,
        LowerOrEqual,
        GreaterOrEqual,
        Is,
        NotEqual,
        LogicalNegation,
        ConditionalAnd,
        ConditionalOr,
        LogicalAnd,
        LogicalOr,
        LogicalXor,
        ShiftBitsLeft,
        ShiftBitsRight,
        NullCoalescing,
        Cast,
        Indexing,
        IndexingWithNullConditional,
    }

    private static Dictionary<string, ExpressionOperator> operatorsDictionary = new Dictionary<string, ExpressionOperator>(StringComparer.OrdinalIgnoreCase)
    {
        { "+", ExpressionOperator.Plus },
        { "-", ExpressionOperator.Minus },
        { "*", ExpressionOperator.Multiply },
        { "/", ExpressionOperator.Divide },
        { "%", ExpressionOperator.Modulo },
        { "<", ExpressionOperator.Lower },
        { ">", ExpressionOperator.Greater },
        { "<=", ExpressionOperator.LowerOrEqual },
        { ">=", ExpressionOperator.GreaterOrEqual },
        { "is", ExpressionOperator.Is },
        { "==", ExpressionOperator.Equal },
        { "<>", ExpressionOperator.NotEqual },
        { "!=", ExpressionOperator.NotEqual },
        { "&&", ExpressionOperator.ConditionalAnd },
        { "||", ExpressionOperator.ConditionalOr },
        { "!", ExpressionOperator.LogicalNegation },
        { "&", ExpressionOperator.LogicalAnd },
        { "|", ExpressionOperator.LogicalOr },
        { "^", ExpressionOperator.LogicalXor },
        { "<<", ExpressionOperator.ShiftBitsLeft },
        { ">>", ExpressionOperator.ShiftBitsRight },
        { "??", ExpressionOperator.NullCoalescing },
    };

    private static Dictionary<ExpressionOperator, bool> leftOperandOnlyOperatorsEvaluationDictionary = new Dictionary<ExpressionOperator, bool>()
    {
    };

    private static Dictionary<ExpressionOperator, bool> rightOperandOnlyOperatorsEvaluationDictionary = new Dictionary<ExpressionOperator, bool>()
    {
        {ExpressionOperator.LogicalNegation, true }
    };

    private static List<Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>> operatorsEvaluations =
        new List<Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>>()
    {
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.Indexing, (dynamic left, dynamic right) => left[right] },
            {ExpressionOperator.IndexingWithNullConditional, (dynamic left, dynamic right) => left?[right] },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.LogicalNegation, (dynamic left, dynamic right) => !right },
            {ExpressionOperator.Cast, (dynamic left, dynamic right) => ChangeType(right, left) },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.Multiply, (dynamic left, dynamic right) => left * right },
            {ExpressionOperator.Divide, (dynamic left, dynamic right) => left / right },
            {ExpressionOperator.Modulo, (dynamic left, dynamic right) => left % right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.Plus, (dynamic left, dynamic right) => left + right  },
            {ExpressionOperator.Minus, (dynamic left, dynamic right) => left - right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.ShiftBitsLeft, (dynamic left, dynamic right) => left << right },
            {ExpressionOperator.ShiftBitsRight, (dynamic left, dynamic right) => left >> right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.Lower, (dynamic left, dynamic right) => left < right },
            {ExpressionOperator.Greater, (dynamic left, dynamic right) => left > right },
            {ExpressionOperator.LowerOrEqual, (dynamic left, dynamic right) => left <= right },
            {ExpressionOperator.GreaterOrEqual, (dynamic left, dynamic right) => left >= right },
            {ExpressionOperator.Is, (dynamic left, dynamic right) => ((Type)right).IsAssignableFrom(left.GetType()) },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.Equal, (dynamic left, dynamic right) => left == right },
            {ExpressionOperator.NotEqual, (dynamic left, dynamic right) => left != right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.LogicalAnd, (dynamic left, dynamic right) => left & right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.LogicalXor, (dynamic left, dynamic right) => left ^ right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.LogicalOr, (dynamic left, dynamic right) => left | right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.ConditionalAnd, (dynamic left, dynamic right) => left && right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.ConditionalOr, (dynamic left, dynamic right) => left || right },
        },
        new Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>>()
        {
            {ExpressionOperator.NullCoalescing, (dynamic left, dynamic right) => left ?? right },
        },
    };

    private static Dictionary<string, object> defaultVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
        { "pi", Math.PI },
        { "e", Math.E },
        { "null", null},
        { "true", true },
        { "false", false },
    };

    private static Dictionary<string, Func<double, double>> simpleDoubleMathFuncsDictionary = new Dictionary<string, Func<double, double>>()
    {
        { "abs", Math.Abs },
        { "acos", Math.Acos },
        { "asin", Math.Asin },
        { "atan", Math.Atan },
        { "ceiling", Math.Ceiling },
        { "cos", Math.Cos },
        { "cosh", Math.Cosh },
        { "exp", Math.Exp },
        { "floor", Math.Floor },
        { "log10", Math.Log10 },
        { "sin", Math.Sin },
        { "sinh", Math.Sinh },
        { "sqrt", Math.Sqrt },
        { "tan", Math.Tan },
        { "tanh", Math.Tanh },
        { "truncate", Math.Truncate },
    };

    private static Dictionary<string, Func<double, double, double>> doubleDoubleMathFuncsDictionary = new Dictionary<string, Func<double, double, double>>()
    {
        { "atan2", Math.Atan2 },
        { "ieeeremainder", Math.IEEERemainder },
        { "log", Math.Log },
        { "pow", Math.Pow },
    };

    private static Dictionary<string, Func<ExpressionEvaluator, List<string>, object>> complexStandardFuncsDictionary = new Dictionary<string, Func<ExpressionEvaluator, List<string>, object>>()
    {
        { "array", (self, args) => args.ConvertAll(arg => self.Evaluate(arg)).ToArray() },
        { "avg", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Sum() / args.Count },
        { "default", (self, args) => { Type type = (self.Evaluate(args[0]) as Type);
            return(type != null && type.IsValueType ? Activator.CreateInstance(type): null); } },
        { "if", (self, args) => (bool)self.Evaluate(args[0]) ? self.Evaluate(args[1]) : self.Evaluate(args[2]) },
        { "in", (self, args) => args.Skip(1).ToList().ConvertAll(arg => self.Evaluate(arg)).Contains(self.Evaluate(args[0])) },
        { "list", (self, args) => args.ConvertAll(arg => self.Evaluate(arg)) },
        { "max", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Max() },
        { "min", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Min() },
        { "new", (self, args) => { List<object> cArgs = args.ConvertAll(arg => self.Evaluate(arg));
            return Activator.CreateInstance(cArgs[0] as Type, cArgs.Skip(1).ToArray());}},
        { "round", (self, args) => { return args.Count > 1 ? Math.Round(Convert.ToDouble(self.Evaluate(args[0])), (int)self.Evaluate(args[1])) : Math.Round(Convert.ToDouble(self.Evaluate(args[0]))); } },
        { "sign", (self, args) => Math.Sign(Convert.ToDouble(self.Evaluate(args[0]))) },
    };

    /// <summary>
    /// All assemblies needed to resolves Types
    /// </summary>
    public List<AssemblyName> ReferencedAssemblies { get; set; } = typeof(ExpressionEvaluator).Assembly.GetReferencedAssemblies().ToList();

    /// <summary>
    /// All Namespaces Where to find types
    /// </summary>
    public List<string> Namespaces { get; set; } = new List<string>
    {
        "System",
        "System.Linq",
        "System.IO",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.ComponentModel",
        "System.Collections",
        "System.Collections.Generic",
        "System.Collections.Specialized",
        "System.Globalization"
    };

    /// <summary>
    /// A list of statics types where to find extensions methods
    /// </summary>
    public List<Type> StaticTypesForExtensionsMethods { get; set; } = new List<Type>()
    {
        typeof(Enumerable) // For Linq extension methods
    };

    /// <summary>
    /// The Values of the variable use in the expressions
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// If <c>true</c> Evaluate function is callables in an expression. If <c>false</c> Evaluate is not callable.
    /// By default : false for security
    /// </summary>
    public bool IsEvaluateFunctionActivated { get; set; } = false;

    /// <summary>
    /// Default Constructor
    /// </summary>
    public ExpressionEvaluator()
    { }

    /// <summary>
    /// Constructor with variable initialize
    /// </summary>
    /// <param name="variables">The Values of the variable use in the expressions</param>
    public ExpressionEvaluator(Dictionary<string, object> variables)
    {
        this.Variables = variables;
    }

    /// <summary>
    /// Is Fired when no internal variable is found for a variable name.
    /// Allow to define a variable and the corresponding value on the fly.
    /// </summary>
    public event EventHandler<VariableEvaluationEventArg> EvaluateVariable;

    /// <summary>
    /// Is Fired when no internal function is found for a variable name.
    /// Allow to define a function and the corresponding value on the fly.
    /// </summary>
    public event EventHandler<FunctionEvaluationEventArg> EvaluateFunction;

    /// <summary>
    /// Evaluate the specified math or pseudo C# expression
    /// </summary>
    /// <param name="expr">the math or pseudo C# expression to evaluate</param>
    /// <returns>The result of the operation if syntax is correct</returns>
    public object Evaluate(string expr)
    {
        expr = expr.Trim();

        Stack<object> stack = new Stack<object>();

        if (GetLambdaExpression(expr, stack))
            return stack.Pop();

        for (int i = 0; i < expr.Length; i++)
        {
            string restOfExpression = expr.Substring(i, expr.Length - i);

            if (!(EvaluateCast(restOfExpression, stack, ref i)
                || EvaluateNumber(restOfExpression, stack, ref i)
                || EvaluateVarOrFunc(expr, restOfExpression, stack, ref i)
                || EvaluateTwoCharsOperators(expr, stack, ref i)))
            {
                string s = expr.Substring(i, 1);

                if (EvaluateParenthis(expr, s, stack, ref i)
                    || EvaluateIndexing(expr, s, stack, ref i)
                    || EvaluateString(expr, s, restOfExpression, stack, ref i))
                { }
                else if (operatorsDictionary.ContainsKey(s))
                {
                    stack.Push(operatorsDictionary[s]);
                }
                else if (!s.Trim().Equals(string.Empty))
                {
                    throw new ExpressionEvaluatorSyntaxErrorException("Invalid character.");
                }
            }
        }

        return ProcessStack(stack);
    }

    private bool EvaluateCast(string restOfExpression, Stack<object> stack, ref int i)
    {
        Match castMatch = castRegex.Match(restOfExpression);

        if (castMatch.Success)
        {
            string typeName = castMatch.Groups["typeName"].Value;
            Type type = GetTypeByFriendlyName(typeName);

            if (type != null)
            {
                i += castMatch.Length - 1;
                stack.Push(type);
                stack.Push(ExpressionOperator.Cast);
                return true;
            }
        }

        return false;
    }

    private bool EvaluateNumber(string restOfExpression, Stack<object> stack, ref int i)
    {
        Match numberMatch = numberRegex.Match(restOfExpression);

        if (numberMatch.Success
            && (!numberMatch.Groups["sign"].Success
        || stack.Count == 0
        || stack.Peek() is ExpressionOperator))
        {
            i += numberMatch.Length;
            i--;

            if (numberMatch.Groups["type"].Success)
            {
                string type = numberMatch.Groups["type"].Value.ToLower();
                string numberNoType = numberMatch.Value.Replace(type, string.Empty);

                Func<string, object> parseFunc = null;
                if (numberSuffixToParse.TryGetValue(type, out parseFunc))
                {
                    stack.Push(parseFunc(numberNoType));
                }
            }
            else
            {
                if (numberMatch.Groups["hasdecimal"].Success)
                {
                    stack.Push(double.Parse(numberMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture));
                }
                else
                {
                    stack.Push(int.Parse(numberMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture));
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool EvaluateVarOrFunc(string expr, string restOfExpression, Stack<object> stack, ref int i)
    {
        Match varFuncMatch = varOrFunctionRegEx.Match(restOfExpression);

        if (varFuncMatch.Success && !operatorsDictionary.ContainsKey(varFuncMatch.Value.Trim()))
        {
            i += varFuncMatch.Length;

            if (varFuncMatch.Groups["isfunction"].Success)
            {
                List<string> funcArgs = GetExpressionsBetweenParenthis(expr, ref i, true);
                object funcResult = null;
                if (varFuncMatch.Groups["inObject"].Success)
                {
                    if (stack.Count == 0 || stack.Peek() is ExpressionOperator)
                    {
                        throw new ExpressionEvaluatorSyntaxErrorException($"[{varFuncMatch.Value})] must follow an object.");
                    }
                    else
                    {
                        string func = varFuncMatch.Groups["name"].Value.ToLower();
                        object obj = stack.Pop();
                        Type objType = null;

                        try
                        {
                            if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                            {
                                stack.Push(null);
                            }
                            else
                            {
                                List<object> oArgs = funcArgs.ConvertAll(arg => Evaluate(arg));
                                BindingFlags flag = DetermineInstanceOrStatic(ref objType, ref obj);

                                // Standard Instance or public method find
                                MethodInfo methodInfo = GetRealMethod(ref objType, ref obj, func, flag, oArgs);

                                // if not found try to Find extension methods.
                                if (methodInfo == null && obj != null)
                                {
                                    oArgs.Insert(0, obj);
                                    objType = obj.GetType();
                                    obj = null;
                                    for (int e = 0; e < StaticTypesForExtensionsMethods.Count && methodInfo == null; e++)
                                    {
                                        Type type = StaticTypesForExtensionsMethods[e];
                                        methodInfo = GetRealMethod(ref type, ref obj, func, staticBindingFlag, oArgs);
                                    }
                                }

                                if (methodInfo != null)
                                {
                                    stack.Push(methodInfo.Invoke(obj, oArgs.ToArray()));
                                }
                                else
                                {
                                    throw new ExpressionEvaluatorSyntaxErrorException($"[{objType.ToString()}] object has no Method named \"{func}\".");
                                }
                            }

                        }
                        catch (ExpressionEvaluatorSyntaxErrorException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            throw new ExpressionEvaluatorSyntaxErrorException($"The call of the method \"{func}\" on type [{objType.ToString()}] generate this error : {(ex.InnerException?.Message ?? ex.Message)}", ex);
                        }

                    }
                }
                else if (DefaultFunctions(varFuncMatch.Groups["name"].Value.ToLower(), funcArgs, out funcResult))
                {
                    stack.Push(funcResult);
                }
                else
                {
                    FunctionEvaluationEventArg functionEvaluationEventArg = new FunctionEvaluationEventArg(varFuncMatch.Groups["name"].Value, Evaluate, funcArgs);

                    EvaluateFunction?.Invoke(this, functionEvaluationEventArg);

                    if (functionEvaluationEventArg.FunctionReturnedValue)
                    {
                        stack.Push(functionEvaluationEventArg.Value);
                    }
                    else
                    {
                        throw new ExpressionEvaluatorSyntaxErrorException($"Function [{varFuncMatch.Groups["name"].Value}] unknown in expression : [{expr}]");
                    }
                }
            }
            else
            {
                string completeVar = varFuncMatch.Groups["name"].Value;
                string var = completeVar.ToLower();

                object varValueToPush = null;
                object cusVarValueToPush = null;
                if (defaultVariables.TryGetValue(var, out varValueToPush))
                {
                    stack.Push(varValueToPush);
                }
                else if (Variables.TryGetValue(var, out cusVarValueToPush))
                {
                    stack.Push(cusVarValueToPush);
                }
                else
                {
                    VariableEvaluationEventArg variableEvaluationEventArg = new VariableEvaluationEventArg(completeVar);

                    EvaluateVariable?.Invoke(this, variableEvaluationEventArg);

                    if (variableEvaluationEventArg.HasValue)
                    {
                        stack.Push(variableEvaluationEventArg.Value);
                    }
                    else
                    {
                        if (varFuncMatch.Groups["inObject"].Success)
                        {
                            if (stack.Count == 0 || stack.Peek() is ExpressionOperator)
                                throw new ExpressionEvaluatorSyntaxErrorException($"[{varFuncMatch.Value}] must follow an object.");

                            object obj = stack.Pop();
                            Type objType = null;

                            try
                            {
                                if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                                {
                                    stack.Push(null);
                                }
                                else
                                {
                                    BindingFlags flag = DetermineInstanceOrStatic(ref objType, ref obj);

                                    object varValue = objType?.GetProperty(var, flag)?.GetValue(obj);
                                    if (varValue == null)
                                        varValue = objType.GetField(var, flag).GetValue(obj);

                                    stack.Push(varValue);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new ExpressionEvaluatorSyntaxErrorException($"[{objType.ToString()}] object has no public Property or Member named \"{var}\".", ex);
                            }
                        }
                        else
                        {
                            string typeName = $"{completeVar}{((i < expr.Length && expr.Substring(i)[0] == '?') ? "?" : "") }";
                            Type staticType = GetTypeByFriendlyName(typeName);

                            if (typeName.EndsWith("?") && staticType != null)
                                i++;

                            if (staticType != null)
                            {
                                stack.Push(staticType);
                            }
                            else
                            {
                                throw new ExpressionEvaluatorSyntaxErrorException($"Variable [{var}] unknown in expression : [{expr}]");
                            }
                        }
                    }
                }

                i--;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool EvaluateTwoCharsOperators(string expr, Stack<object> stack, ref int i)
    {
        if (i < expr.Length - 1)
        {
            String op = expr.Substring(i, 2);
            if (operatorsDictionary.ContainsKey(op))
            {
                stack.Push(operatorsDictionary[op]);
                i++;
                return true;
            }
        }

        return false;
    }

    private bool EvaluateParenthis(string expr, string s, Stack<object> stack, ref int i)
    {
        if (s.Equals(")"))
            throw new Exception($"To much ')' characters are defined in expression : [{expr}] : no corresponding '(' fund.");

        if (s.Equals("("))
        {
            i++;

            if (stack.Count > 0 && stack.Peek() is lambdaExpressionDelegate)
            {
                List<string> expressionsInParenthis = GetExpressionsBetweenParenthis(expr, ref i, true);

                lambdaExpressionDelegate lambdaDelegate = stack.Pop() as lambdaExpressionDelegate;

                stack.Push(lambdaDelegate(expressionsInParenthis.ConvertAll(arg => Evaluate(arg)).ToArray()));
            }
            else
            {
                List<string> expressionsInParenthis = GetExpressionsBetweenParenthis(expr, ref i, false);

                stack.Push(Evaluate(expressionsInParenthis[0]));
            }

            return true;
        }

        return false;
    }

    private bool EvaluateIndexing(string expr, string s, Stack<object> stack, ref int i)
    {
        Match indexingBeginningMatch = indexingBeginningRegex.Match(expr.Substring(i));

        if (indexingBeginningMatch.Success)
        {
            string innerExp = "";
            i += indexingBeginningMatch.Length;
            int bracketCount = 1;
            for (; i < expr.Length; i++)
            {
                Match internalStringMatch = stringBeginningRegex.Match(expr.Substring(i));

                if (internalStringMatch.Success)
                {
                    string innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expr.Substring(i + internalStringMatch.Length), internalStringMatch);
                    innerExp += innerString;
                    i += innerString.Length - 1;
                }
                else
                {
                    s = expr.Substring(i, 1);

                    if (s.Equals("[")) bracketCount++;

                    if (s.Equals("]"))
                    {
                        bracketCount--;
                        if (bracketCount == 0) break;
                    }
                    innerExp += s;
                }
            }

            if (bracketCount > 0)
            {
                string beVerb = bracketCount == 1 ? "is" : "are";
                throw new Exception($"{bracketCount} ']' character {beVerb} missing in expression : [{expr}]");
            }
            stack.Push(indexingBeginningMatch.Length == 2 ? ExpressionOperator.IndexingWithNullConditional : ExpressionOperator.Indexing);
            stack.Push(Evaluate(innerExp));

            dynamic right = stack.Pop();
            ExpressionOperator op = (ExpressionOperator)stack.Pop();
            dynamic left = stack.Pop();

            stack.Push(operatorsEvaluations[0][op](left, right));

            return true;
        }

        return false;
    }

    private bool EvaluateString(string expr, string s, string restOfExpression, Stack<object> stack, ref int i)
    {
        Match stringBeginningMatch = stringBeginningRegex.Match(restOfExpression);

        if (stringBeginningMatch.Success)
        {
            bool isEscaped = stringBeginningMatch.Groups["escaped"].Success;
            bool isInterpolated = stringBeginningMatch.Groups["interpolated"].Success;

            i += stringBeginningMatch.Length;

            Regex stringRegexPattern = new Regex($"^[^{(isEscaped ? "" : @"\\")}{(isInterpolated ? "{}" : "")}\"]*");

            bool endOfString = false;

            string resultString = string.Empty;

            while (!endOfString && i < expr.Length)
            {
                Match stringMatch = stringRegexPattern.Match(expr.Substring(i, expr.Length - i));

                resultString += stringMatch.Value;
                i += stringMatch.Length;

                if (expr.Substring(i)[0] == '"')
                {
                    endOfString = true;
                    stack.Push(resultString);
                }
                else if (expr.Substring(i)[0] == '{')
                {
                    i++;

                    if (expr.Substring(i)[0] == '{')
                    {
                        resultString += @"{";
                        i++;
                    }
                    else
                    {
                        string innerExp = "";
                        int bracketCount = 1;
                        for (; i < expr.Length; i++)
                        {
                            Match internalStringMatch = stringBeginningRegex.Match(expr.Substring(i));

                            if (internalStringMatch.Success)
                            {
                                string innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expr.Substring(i + internalStringMatch.Length), internalStringMatch);
                                innerExp += innerString;
                                i += innerString.Length - 1;
                            }
                            else
                            {

                                s = expr.Substring(i, 1);

                                if (s.Equals("{")) bracketCount++;

                                if (s.Equals("}"))
                                {
                                    bracketCount--;
                                    i++;
                                    if (bracketCount == 0) break;
                                }
                                innerExp += s;
                            }
                        }

                        if (bracketCount > 0)
                        {
                            string beVerb = bracketCount == 1 ? "is" : "are";
                            throw new Exception($"{bracketCount} '}}' character {beVerb} missing in expression : [{expr}]");
                        }
                        resultString += Evaluate(innerExp).ToString();
                    }
                }
                else if (expr.Substring(i, expr.Length - i)[0] == '}')
                {
                    i++;

                    if (expr.Substring(i, expr.Length - i)[0] == '}')
                    {
                        resultString += @"}";
                        i++;
                    }
                    else
                    {
                        throw new ExpressionEvaluatorSyntaxErrorException("A character '}' must be escaped in a interpolated string.");
                    }
                }
                else if (expr.Substring(i, expr.Length - i)[0] == '\\')
                {
                    i++;

                    string escapedString = null;
                    if (stringEscapedCharDict.TryGetValue(expr.Substring(i, expr.Length - i)[0], out escapedString))
                    {
                        resultString += escapedString;
                        i++;
                    }
                    else
                    {
                        throw new ExpressionEvaluatorSyntaxErrorException("There is no corresponding escaped character for \\" + expr.Substring(i, 1));
                    }
                }
            }

            if (!endOfString)
                throw new ExpressionEvaluatorSyntaxErrorException("A \" character is missing.");

            return true;
        }

        return false;
    }

    private object ProcessStack(Stack<object> stack)
    {
        List<object> list = stack.ToList<object>();

        operatorsEvaluations.ForEach(delegate (Dictionary<ExpressionOperator, Func<dynamic, dynamic, object>> operatorEvalutationsDict)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                for (int opi = 0; opi < operatorEvalutationsDict.Keys.ToList().Count; opi++)
                {
                    ExpressionOperator eOp = operatorEvalutationsDict.Keys.ToList()[opi];

                    if ((list[i] as ExpressionOperator?) == eOp)
                    {
                        if (rightOperandOnlyOperatorsEvaluationDictionary.ContainsKey(eOp))
                        {
                            list[i] = operatorEvalutationsDict[eOp](null, (dynamic)list[i - 1]);
                            list.RemoveAt(i - 1);
                            break;
                        }
                        else if (leftOperandOnlyOperatorsEvaluationDictionary.ContainsKey(eOp))
                        {
                            list[i] = operatorEvalutationsDict[eOp]((dynamic)list[i + 1], null);
                            list.RemoveAt(i + 1);
                            break;
                        }
                        else
                        {
                            list[i] = operatorEvalutationsDict[eOp]((dynamic)list[i + 1], (dynamic)list[i - 1]);
                            list.RemoveAt(i + 1);
                            list.RemoveAt(i - 1);
                            i -= 1;
                            break;
                        }
                    }
                }
            }
        });

        stack.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            stack.Push(list[i]);
        }

        if (stack.Count > 1)
            throw new ExpressionEvaluatorSyntaxErrorException("Syntax error. Check that no operator is missing");

        return stack.Pop();
    }

    private delegate dynamic lambdaExpressionDelegate(params dynamic[] args);
    private bool GetLambdaExpression(string expr, Stack<object> stack)
    {
        Match lambdaExpressionMatch = lambdaExpressionRegex.Match(expr);

        if (lambdaExpressionMatch.Success)
        {
            List<string> argsNames = lambdaArgRegex
                .Matches(lambdaExpressionMatch.Groups["args"].Value)
                .Cast<Match>().ToList()
                .ConvertAll(argMatch => argMatch.Value);

            stack.Push(new lambdaExpressionDelegate(delegate (object[] args)
            {
                Dictionary<string, object> vars = new Dictionary<string, object>(Variables);

                for (int a = 0; a < argsNames.Count || a < args.Length; a++)
                {
                    vars[argsNames[a]] = args[a];
                }

                ExpressionEvaluator expressionEvaluator = new ExpressionEvaluator(vars);

                return expressionEvaluator.Evaluate(lambdaExpressionMatch.Groups["expression"].Value);
            }));

            return true;
        }
        else
        {
            return false;
        }
    }

    private MethodInfo GetRealMethod(ref Type type, ref object obj, string func, BindingFlags flag, List<object> args)
    {
        MethodInfo methodInfo = null;
        List<object> modifiedArgs = new List<object>(args);

        if (func.StartsWith("fluid") || func.StartsWith("fluent"))
        {
            methodInfo = GetRealMethod(ref type, ref obj, func.Substring(func.StartsWith("fluid") ? 5 : 6), flag, modifiedArgs);
            if (methodInfo != null)
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    obj = new DelegateEncaps(obj, methodInfo);

                    methodInfo = typeof(DelegateEncaps).GetMethod("CallFluidMethod");

                    args.Clear();
                    args.Add(modifiedArgs.ToArray());
                }

                return methodInfo;
            }
        }

        if (args.Contains(null))
        {
            methodInfo = type.GetMethod(func, flag);
        }
        else
        {
            methodInfo = type.GetMethod(func, flag, null, args.ConvertAll(arg => arg.GetType()).ToArray(), null);
        }

        if (methodInfo != null)
        {
            methodInfo = MakeConcreteMethodIfGeneric(methodInfo);
        }
        else
        {
            List<MethodInfo> methodInfos = type.GetMethods(flag)
            .Where(m =>
            {
                return m.Name.ToLower().Equals(func) && m.GetParameters().Length == modifiedArgs.Count;
            })
            .ToList();

            for (int m = 0; m < methodInfos.Count && methodInfo == null; m++)
            {
                methodInfos[m] = MakeConcreteMethodIfGeneric(methodInfos[m]);

                bool parametersCastOK = true;

                modifiedArgs = new List<object>(args);

                for (int a = 0; a < modifiedArgs.Count; a++)
                {
                    Type parameterType = methodInfos[m].GetParameters()[a].ParameterType;
                    string paramTypeName = parameterType.Name;

                    if (paramTypeName.StartsWith("Predicate")
                        && modifiedArgs[a] is lambdaExpressionDelegate)
                    {
                        lambdaExpressionDelegate led = modifiedArgs[a] as lambdaExpressionDelegate;
                        modifiedArgs[a] = new Predicate<object>(o => (bool)(led(new object[] { o })));
                    }
                    else if (paramTypeName.StartsWith("Func")
                        && modifiedArgs[a] is lambdaExpressionDelegate)
                    {
                        lambdaExpressionDelegate led = modifiedArgs[a] as lambdaExpressionDelegate;
                        DelegateEncaps de = new DelegateEncaps(led);
                        MethodInfo encapsMethod = de.GetType()
                            .GetMethod($"Func{parameterType.GetGenericArguments().Length - 1}")
                            .MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, encapsMethod);
                    }
                    else if (paramTypeName.StartsWith("Converter")
                        && modifiedArgs[a] is lambdaExpressionDelegate)
                    {
                        lambdaExpressionDelegate led = modifiedArgs[a] as lambdaExpressionDelegate;
                        modifiedArgs[a] = new Converter<object, object>(o => (led(new object[] { o })));
                    }
                    else
                    {
                        try
                        {
                            if (!methodInfos[m].GetParameters()[a].ParameterType.IsAssignableFrom(modifiedArgs[a].GetType()))
                            {
                                modifiedArgs[a] = Convert.ChangeType(modifiedArgs[a], methodInfos[m].GetParameters()[a].ParameterType);
                            }
                        }
                        catch
                        {
                            parametersCastOK = false;
                        }
                    }
                }

                if (parametersCastOK)
                    methodInfo = methodInfos[m];
            }

            if (methodInfo != null)
            {
                args.Clear();
                args.AddRange(modifiedArgs);
            }
        }

        return methodInfo;
    }

    private MethodInfo MakeConcreteMethodIfGeneric(MethodInfo methodInfo)
    {
        if (methodInfo.IsGenericMethod)
        {
            return methodInfo.MakeGenericMethod(Enumerable.Repeat(typeof(object), methodInfo.GetGenericArguments().Count()).ToArray());
        }

        return methodInfo;
    }

    private BindingFlags DetermineInstanceOrStatic(ref Type objType, ref object obj)
    {
        if (obj is Type)
        {
            objType = obj as Type;
            obj = null;
            return staticBindingFlag;
        }
        else
        {
            objType = obj.GetType();
            return instanceBindingFlag;
        }
    }

    private List<string> GetExpressionsBetweenParenthis(string expr, ref int i, bool checkComas)
    {
        List<string> expressionsList = new List<string>();

        string s;
        string currentExpression = string.Empty;
        int bracketCount = 1;
        for (; i < expr.Length; i++)
        {
            Match internalStringMatch = stringBeginningRegex.Match(expr.Substring(i));

            if (internalStringMatch.Success)
            {
                string innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expr.Substring(i + internalStringMatch.Length), internalStringMatch);
                currentExpression += innerString;
                i += innerString.Length - 1;
            }
            else
            {
                s = expr.Substring(i, 1);

                if (s.Equals("(")) bracketCount++;

                if (s.Equals(")"))
                {
                    bracketCount--;
                    if (bracketCount == 0)
                    {
                        if (!currentExpression.Trim().Equals(string.Empty))
                            expressionsList.Add(currentExpression);
                        break;
                    }
                }

                if (checkComas && s.Equals(",") && bracketCount == 1)
                {
                    expressionsList.Add(currentExpression);
                    currentExpression = string.Empty;
                }
                else
                    currentExpression += s;
            }
        }

        if (bracketCount > 0)
        {
            string beVerb = bracketCount == 1 ? "is" : "are";
            throw new Exception($"{bracketCount} ')' character {beVerb} missing in expression : [{expr}]");
        }

        return expressionsList;
    }

    private bool DefaultFunctions(string name, List<string> args, out object result)
    {
        bool functionExists = true;

        Func<double, double> func = null;
        Func<double, double, double> func2 = null;
        Func<ExpressionEvaluator, List<string>, object> complexFunc = null;
        if (simpleDoubleMathFuncsDictionary.TryGetValue(name, out func))
        {
            result = func(Convert.ToDouble(Evaluate(args[0])));
        }
        else if (doubleDoubleMathFuncsDictionary.TryGetValue(name, out func2))
        {
            result = func2(Convert.ToDouble(Evaluate(args[0])), Convert.ToDouble(Evaluate(args[1])));
        }
        else if (complexStandardFuncsDictionary.TryGetValue(name, out complexFunc))
        {
            result = complexFunc(this, args);
        }
        else if (IsEvaluateFunctionActivated && name.Equals("evaluate"))
        {
            result = Evaluate((string)Evaluate(args[0]));
        }
        else
        {
            result = null;
            functionExists = false;
        }

        return functionExists;
    }

    private Type GetTypeByFriendlyName(string typeName)
    {
        Type result = null;
        try
        {
            result = Type.GetType(typeName, false, true);

            if (result == null)
            {
                typeName = primaryTypesRegex.Replace(typeName, delegate (Match match)
                {
                    return PrimaryTypesDict[match.Value].ToString();
                });

                result = Type.GetType(typeName, false, true);
            }

            for (int a = 0; a < ReferencedAssemblies.Count && result == null; a++)
            {
                for (int i = 0; i < Namespaces.Count && result == null; i++)
                {
                    result = Type.GetType($"{Namespaces[i]}.{typeName},{ReferencedAssemblies[a].FullName}", false, true);
                }
            }
        }
        catch { }

        return result;
    }

    private static object ChangeType(object value, Type conversionType)
    {
        if (conversionType == null)
        {
            throw new ArgumentNullException("conversionType");
        }
        if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }
            NullableConverter nullableConverter = new NullableConverter(conversionType);
            conversionType = nullableConverter.UnderlyingType;
        }
        return Convert.ChangeType(value, conversionType);
    }

    private string GetCodeUntilEndOfString(string subExpr, Match stringBeginningMatch)
    {
        Match codeUntilEndOfStringMatch = stringBeginningMatch.Value.Contains("$") ? endOfStringWithDollar.Match(subExpr) : endOfStringWithoutDollar.Match(subExpr);
        string result = subExpr;

        if (codeUntilEndOfStringMatch.Success)
        {
            if (codeUntilEndOfStringMatch.Value.EndsWith("\""))
            {
                result = codeUntilEndOfStringMatch.Value;
            }
            else if (codeUntilEndOfStringMatch.Value.EndsWith("{") && codeUntilEndOfStringMatch.Length < subExpr.Length)
            {
                if (subExpr[codeUntilEndOfStringMatch.Length] == '{')
                {
                    result = codeUntilEndOfStringMatch.Value + "{"
                        + GetCodeUntilEndOfString(subExpr.Substring(codeUntilEndOfStringMatch.Length + 1), stringBeginningMatch);
                }
                else
                {
                    string interpolation = GetCodeUntilEndOfStringInterpolation(subExpr.Substring(codeUntilEndOfStringMatch.Length));
                    result = codeUntilEndOfStringMatch.Value + interpolation
                        + GetCodeUntilEndOfString(subExpr.Substring(codeUntilEndOfStringMatch.Length + interpolation.Length), stringBeginningMatch);
                }
            }
        }

        return result;
    }

    private string GetCodeUntilEndOfStringInterpolation(string subExpr)
    {
        Match endOfStringInterpolationMatch = endOfStringInterpolationRegex.Match(subExpr);
        string result = subExpr;

        if (endOfStringInterpolationMatch.Success)
        {
            if (endOfStringInterpolationMatch.Value.EndsWith("}"))
            {
                result = endOfStringInterpolationMatch.Value;
            }
            else
            {
                Match stringBeginningForEndBlockMatch = stringBeginningForEndBlockRegex.Match(endOfStringInterpolationMatch.Value);

                string subString = GetCodeUntilEndOfString(subExpr.Substring(endOfStringInterpolationMatch.Length), stringBeginningForEndBlockMatch);

                result = endOfStringInterpolationMatch.Value + subString
                    + GetCodeUntilEndOfStringInterpolation(subExpr.Substring(endOfStringInterpolationMatch.Length + subString.Length));
            }
        }

        return result;
    }

    private class DelegateEncaps
    {
        private lambdaExpressionDelegate lambda;

        private MethodInfo methodInfo;
        private object target;

        public DelegateEncaps(lambdaExpressionDelegate lambda)
        {
            this.lambda = lambda;
        }
        public DelegateEncaps(object target, MethodInfo methodInfo)
        {
            this.target = target;
            this.methodInfo = methodInfo;
        }

        public object CallFluidMethod(params object[] args)
        {
            methodInfo.Invoke(target, args);
            return target;
        }

        public TResult Func0<TResult>()
        {
            return (TResult)lambda();
        }
        public TResult Func1<T, TResult>(T arg)
        {
            return (TResult)lambda(arg);
        }
        public TResult Func2<T1, T2, TResult>(T1 arg1, T2 arg2)
        {
            return (TResult)lambda(arg1, arg2);
        }
        public TResult Func3<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3)
        {
            return (TResult)lambda(arg1, arg2, arg3);
        }
        public TResult Func4<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4);
        }
        public TResult Func5<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5);
        }
        public TResult Func6<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6);
        }
        public TResult Func7<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        public TResult Func8<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        public TResult Func9<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        public TResult Func10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        public TResult Func11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
        public TResult Func12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
        public TResult Func13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
        public TResult Func14<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
        public TResult Func15<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
        public TResult Func16<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
    }
}

[Serializable]
public class ExpressionEvaluatorSyntaxErrorException : Exception
{
    public ExpressionEvaluatorSyntaxErrorException() : base()
    { }

    public ExpressionEvaluatorSyntaxErrorException(string message) : base(message)
    { }
    public ExpressionEvaluatorSyntaxErrorException(string message, Exception innerException) : base(message, innerException)
    { }

    protected ExpressionEvaluatorSyntaxErrorException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
    }
}

internal class VariableEvaluationEventArg : EventArgs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">The name of the variable to Evaluate</param>
    public VariableEvaluationEventArg(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name of the variable to Evaluate
    /// </summary>
    public string Name { get; private set; }

    private object varValue;

    /// <summary>
    /// To set a value to this variable
    /// </summary>
    public object Value
    {
        get { return varValue; }
        set
        {
            varValue = value;
            HasValue = true;
        }
    }

    /// <summary>
    /// if <c>true</c> the variable is affected, if <c>false</c> it means that the variable does not exist.
    /// </summary>
    public bool HasValue { get; set; } = false;
}
internal class FunctionEvaluationEventArg : EventArgs
{
    private Func<string, object> evaluateFunc = null;

    public FunctionEvaluationEventArg(string name, Func<string, object> evaluateFunc, List<string> args = null)
    {
        Name = name;
        Args = args ?? new List<string>();
        this.evaluateFunc = evaluateFunc;
    }

    /// <summary>
    /// The not evaluated args of the function
    /// </summary>
    public List<string> Args { get; private set; } = new List<string>();

    /// <summary>
    /// Get the values of the function's args.
    /// </summary>
    /// <returns></returns>
    public object[] EvaluateArgs()
    {
        return Args.ConvertAll(arg => evaluateFunc(arg)).ToArray();
    }

    /// <summary>
    /// Get the value of the function's arg at the specified index
    /// </summary>
    /// <param name="index">The index of the function's arg to evaluate</param>
    /// <returns>The evaluated arg</returns>
    public object EvaluateArg(int index)
    {
        return evaluateFunc(Args[index]);
    }

    /// <summary>
    /// The name of the variable to Evaluate
    /// </summary>
    public string Name { get; private set; }

    private object returnValue = null;

    /// <summary>
    /// To set the return value of the function
    /// </summary>
    public object Value
    {
        get { return returnValue; }
        set
        {
            returnValue = value;
            FunctionReturnedValue = true;
        }
    }

    /// <summary>
    /// if <c>true</c> the function evaluation has been done, if <c>false</c> it means that the function does not exist.
    /// </summary>
    public bool FunctionReturnedValue { get; set; } = false;
}

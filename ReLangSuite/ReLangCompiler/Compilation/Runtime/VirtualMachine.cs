using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Parsing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Runtime {
    public class VirtualMachine {
        enum ReturnOption {
            None,
            Continue,
            Break,
            Return,
        }


        private FrameMachine frameMachine;
        private List<FunctionData> functions;
        private object functionValue;
        private bool wasReturnValueSet;
        private ReturnOption returnOption;

        //public System.IO.TextWriter VmOut { get; }
        public System.IO.TextWriter ProgramOut { get; }
        public System.IO.TextWriter ProgramErr { get; }


        public VirtualMachine(System.IO.TextWriter programOut, System.IO.TextWriter programErr) {
            //VmOut = vmOut;
            ProgramOut = programOut;
            ProgramErr = programErr;
        }


        public int Execute(ParsedProgram program, string[] commandLineArguments) {
            frameMachine = new FrameMachine();
            functions = program.Functions;
            functionValue = null;
            LogInfo("Executing main()...");

            // Converting command line arguments to external representation
            var arguments = ConvertArguments(commandLineArguments);

            try {
                var maybe = EvaluateCustomFunction(program.MainFunctionNumber, arguments);
                var result = 0;
                if (maybe != null) {
                    result = (int)maybe;
                }
                LogInfo($"Process finished with exit code: {result}");
                return result;

            } catch (ProgramException e) {
                HandleProgramError(e);
                return 1;

            } catch (VirtualMachineException e) {
                HandleVirtualMachineError(e);
                return 1;
            }
        }


        private void HandleProgramError(ProgramException e) {
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            ProgramErr.WriteLine("Error has occured during program's execution:");
            ProgramErr.WriteLine(ErrorToString(e));

            Console.ForegroundColor = oldForeground;

            LogError("Aborted");
        }


        private void HandleVirtualMachineError(VirtualMachineException e) {
            LogError("Virtual machine's internal error:");
            LogError(e.Message);
            LogError(e.StackTrace);
        }


        private List<object> ConvertArguments(string[] commandLineArguments) {
            var arguments = new ListAdapter(commandLineArguments);
            return new List<object> { arguments };
        }


        private object EvaluateCustomFunction(int number, List<object> arguments) {
            frameMachine.EnterFrame();

            // Push arguments
            foreach (var argument in arguments) {
                frameMachine.CreateVariable(argument, false);
            }

            // Execute body
            functionValue = null;
            wasReturnValueSet = false;
            returnOption = ReturnOption.None;
            var function = functions[number];
            object result = null;
            bool wasSet = false;

            try {
                ExecuteStatementList(function.Body);
            } finally {
                // Leave frame and return function value
                frameMachine.LeaveFrame();
                returnOption = ReturnOption.None;
                wasSet = wasReturnValueSet;
                wasReturnValueSet = false;
                result = functionValue;
                functionValue = null;  // No one will see this value outside evaluation anymore
            }

            if (!function.IsProcedure && !wasSet) {
                var fullName = $"{function.Definition.FullQualification}.{function.Definition.ShortName}";
                throw ProgramException.CreateNoReturnValueError(fullName);
            }

            return result;
        }


        private void ExecuteStatementList(List<IStatement> statements) {
            foreach (var statement in statements) {
                if (returnOption == ReturnOption.None) {
                    ExecuteStatement(statement);
                } else {
                    break;
                }
            }
        }


        private void ExecuteStatement(IStatement statement) {
            List<IStatement> statements;
            object value;

            switch (statement) {
                case ConditionalStatement conditional:
                    statements = conditional.IfStatements;
                    if (!(bool)EvaluateExpression(conditional.Condition)) {
                        statements = conditional.ElseStatements;
                    }

                    if (statements != null) {
                        frameMachine.EnterScope();
                        try {
                            ExecuteStatementList(statements);
                        } finally {
                            frameMachine.LeaveScope();
                        }
                    }
                    break;

                case ForEachStatement forEach:
                    //Log("Executing for-each...");
                    statements = forEach.Statements;
                    var iterable = ConvertToEnumerable(EvaluateExpression(forEach.Iterable));

                    foreach (var item in iterable) {
                        frameMachine.EnterScope();
                        try {
                            frameMachine.CreateVariable(item, false);
                            ExecuteStatementList(statements);

                            if (returnOption <= ReturnOption.Continue) {
                                returnOption = ReturnOption.None;
                            } else if (returnOption == ReturnOption.Break) {
                                returnOption = ReturnOption.None;
                                break;
                            } else {
                                break;
                            }
                        } finally {
                            frameMachine.LeaveScope();
                        }
                    }
                    break;

                case WhileStatement whileStatement:
                    statements = whileStatement.Statements;
                    while ((bool)EvaluateExpression(whileStatement.Condition)) {
                        frameMachine.EnterScope();
                        try {
                            ExecuteStatementList(statements);

                            if (returnOption <= ReturnOption.Continue) {
                                returnOption = ReturnOption.None;
                            } else if (returnOption == ReturnOption.Break) {
                                returnOption = ReturnOption.None;
                                break;
                            } else {
                                break;
                            }
                        } finally {
                            frameMachine.LeaveScope();
                        }
                    }
                    break;

                case DoWhileStatement doWhileStatement:
                    statements = doWhileStatement.Statements;
                    do {
                        frameMachine.EnterScope();
                        try {
                            ExecuteStatementList(statements);

                            if (returnOption <= ReturnOption.Continue) {
                                returnOption = ReturnOption.None;
                            } else if (returnOption == ReturnOption.Break) {
                                returnOption = ReturnOption.None;
                                break;
                            } else {
                                break;
                            }
                        } finally {
                            frameMachine.LeaveScope();
                        }
                    } while ((bool)EvaluateExpression(doWhileStatement.Condition));
                    break;

                case ExpressionStatement expression:
                    EvaluateExpression(expression.Expression);
                    break;

                case VariableDeclarationStatement variableDeclaration:
                    value = EvaluateExpression(variableDeclaration.Value);
                    frameMachine.CreateVariable(value, (variableDeclaration.Qualifier & VariableQualifier.Disposable) != 0);
                    break;

                case AssignmentStatement assignment:
                    value = EvaluateExpression(assignment.Value);
                    frameMachine.SetVariable(assignment.Number, value);
                    break;

                case ReturnStatement returnStatement:
                    functionValue = EvaluateExpression(returnStatement.Operand);
                    wasReturnValueSet = true;
                    returnOption = ReturnOption.Return;
                    break;

                case BreakStatement breakStatement:
                    if (breakStatement.IsContinue) {
                        returnOption = ReturnOption.Continue;
                    } else {
                        returnOption = ReturnOption.Break;
                    }
                    break;

                case TryCatchStatement tryCatch:
                    try {
                        frameMachine.EnterScope();
                        try {
                            ExecuteStatementList(tryCatch.TryBlock);
                        } finally {
                            frameMachine.LeaveScope();
                        }
                    } catch (ProgramException e) {
                        foreach (var (option, instanceName, catchStatements) in tryCatch.CatchBlocks) {
                            if (option == e.ErrorOption || option == ErrorTypeInfo.Option.Error) {
                                frameMachine.EnterScope();
                                if (instanceName != "_") {
                                    frameMachine.CreateVariable(e, false);
                                }
                                try {
                                    ExecuteStatementList(catchStatements);
                                } finally {
                                    frameMachine.LeaveScope();
                                }
                                return;
                            }
                        }
                        throw;
                    }
                    break;

                case CompoundStatement compound:
                    ExecuteStatementList(compound.Statements);
                    break;

                case NopeStatement nope:
                    break;

                default:
                    throw new VirtualMachineException($"Unsupported statement: {statement}");
            }
        }


        private IEnumerable<object> ConvertToEnumerable(object value) {
            if (value is string str) {
                return GetStringEnumerable(str);
            } else if (value is FileStream stream) {
                return GetFileStreamEnumerable(stream);
            } else {
                return (IEnumerable<object>)value;
            }
        }


        private IEnumerable<object> GetFileStreamEnumerable(FileStream stream) {
            StreamReader reader = null;
            try {
                reader = new StreamReader(stream);
            } catch (ArgumentException e) {
                throw new ProgramException(ErrorTypeInfo.Option.IOError, e.Message, null);
            }
            string line;

            while (true) {
                try {
                    line = reader.ReadLine();
                } catch (IOException e) {
                    throw new ProgramException(ErrorTypeInfo.Option.IOError, e.Message, null);
                } catch (NotSupportedException e) {
                    throw new ProgramException(ErrorTypeInfo.Option.NotSupportedError, e.Message, null);
                }

                if (line == null) {
                    yield break;
                }

                yield return line;
            }
        }


        private IEnumerable<object> GetStringEnumerable(string str) {
            foreach (var ch in str) {
                yield return ch;
            }
        }


        private object EvaluateExpression(IExpression expression) {
            switch (expression) {
                case FunctionCallExpression functionCall:
                    var values = new List<object>();
                    foreach (var argument in functionCall.Arguments) {
                        values.Add(EvaluateExpression(argument));
                    }

                    try {
                        return EvaluateFunction(functionCall.FunctionDefinition, values);
                    } catch (ProgramException e) {
                        e.AddLocation(expression.MainLocation);
                        throw e;
                    }                   

                case IOperatorExpression operatorExpression:
                    switch (operatorExpression) {
                        case BinaryOperatorExpression binary:
                            return EvaluateBinaryOperator(binary);

                        case UnaryOperatorExpression unary:
                            return EvaluateUnaryOperator(unary);

                        default:
                            throw new VirtualMachineException($"Unsupported operator expression: {operatorExpression}");
                    }

                case ConversionExpression conversion:
                    return EvaluateConversion(conversion);

                case FormatStringExpression formatString:
                    return EvaluateFormatString(formatString);

                case ILiteralExpression literal:
                    switch (literal) {
                        case PrimitiveLiteralExpression primitiveLiteral:
                            return primitiveLiteral.Value;

                        case NullLiteralExpression nullLiteral:
                            return null;

                        case ListLiteralExpression listLiteral:
                            return new ListAdapter(listLiteral.Items.Select(item => EvaluateExpression(item)));

                        case SetLiteralExpression setLiteral:
                            return new HashSet<object>(setLiteral.Items.Select(item => EvaluateExpression(item)));

                        case DictionaryLiteralExpression dictionaryLiteral:
                            var pairs = dictionaryLiteral.Pairs.Select(
                                pair => (EvaluateExpression(pair.Item1), EvaluateExpression(pair.Item2))
                            );
                            return new DictionaryAdapter(pairs);

                        case RangeLiteralExpression rangeLiteral:
                            var start = (int)EvaluateExpression(rangeLiteral.Start);
                            var end = (int)EvaluateExpression(rangeLiteral.End);
                            return new RangeAdapter(start, end);

                        case TupleLiteralExpression tupleLiteral:
                            var tuple = new object[tupleLiteral.Items.Count];
                            for (var i = 0; i < tuple.Length; i++) {
                                tuple[i] = EvaluateExpression(tupleLiteral.Items[i]);
                            }
                            return new TupleAdapter(tuple);

                        default:
                            throw new VirtualMachineException($"Unknown literal expression: {literal}");
                    } 

                case VariableExpression variable:
                    return frameMachine.GetVariable(variable.Number);

                default:
                    throw new VirtualMachineException($"Unsupported expression: {expression}");
            }
        }


        private object EvaluateFormatString(FormatStringExpression formatString) {
            var builder = new StringBuilder(formatString.Pieces[0]);

            var pieces = formatString.Pieces;
            var expressions = formatString.Expressions;
            var number = expressions.Count;

            for (var i = 0; i < number; i++) {
                var value = EvaluateExpression(expressions[i]);
                builder.Append(ObjectToString(value, false, false));
                builder.Append(pieces[i + 1]);
            }

            return builder.ToString();
        }


        private object EvaluateFunction(IFunctionDefinition definition, List<object> arguments) {
            switch (definition) {
                case BuiltinFunctionDefinition builtin:
                    return EvaluateBuiltinFunction(builtin.BuiltinOption, arguments);

                case CustomFunctionDefinition custom:
                    return EvaluateCustomFunction(custom.Number, arguments);

                default:
                    throw new NotImplementedException($"Unknown functional type: {definition}");
            }
        }


        private object EvaluateBinaryOperator(BinaryOperatorExpression binary) {
            // Short-circuit evaluation for booleans and maybes
            var left = EvaluateExpression(binary.LeftOperand);

            switch (binary.OperatorOption) {
                case BinaryOperatorExpression.Option.Or:
                    if ((bool)left) {
                        return true;
                    } else {
                        return (bool)EvaluateExpression(binary.RightOperang);
                    }

                case BinaryOperatorExpression.Option.And:
                    if (!(bool)left) {
                        return false;
                    } else {
                        return (bool)EvaluateExpression(binary.RightOperang);
                    }

                case BinaryOperatorExpression.Option.ValueOrDefault:
                    if (left != null) {
                        return left;
                    } else {
                        return EvaluateExpression(binary.RightOperang);
                    }
            }

            // Long-circuit for other types
            var right = EvaluateExpression(binary.RightOperang);

            switch (binary.OperatorOption) {
                case BinaryOperatorExpression.Option.AddInteger:
                    return (int)left + (int)right;

                case BinaryOperatorExpression.Option.AddFloating:
                    return (double)left + (double)right;

                case BinaryOperatorExpression.Option.AddString:
                    return (string)left + (string)right;

                case BinaryOperatorExpression.Option.AddList:
                    return CallListExtended((ListAdapter)left, (ListAdapter)right);

                case BinaryOperatorExpression.Option.SubtractInteger:
                    return (int)left - (int)right;

                case BinaryOperatorExpression.Option.SubtractFloating:
                    return (double)left - (double)right;

                case BinaryOperatorExpression.Option.MultiplyInteger:
                    return (int)left * (int)right;

                case BinaryOperatorExpression.Option.MultiplyFloating:
                    return (double)left * (double)right;

                case BinaryOperatorExpression.Option.DivideInteger:
                    return (int)left / (int)right;

                case BinaryOperatorExpression.Option.DivideFloating:
                    return (double)left / (double)right;

                case BinaryOperatorExpression.Option.Modulo:
                    return (int)left % (int)right;
                
                case BinaryOperatorExpression.Option.EqualBoolean:
                    return (bool)left == (bool)right;

                case BinaryOperatorExpression.Option.EqualInteger:
                    return (int)left == (int)right;

                case BinaryOperatorExpression.Option.EqualFloating:
                    return AboutEqual((double)left, (double)right);

                case BinaryOperatorExpression.Option.EqualString:
                    return ((string)left).Equals(right);

                case BinaryOperatorExpression.Option.EqualObject:
                    return left == right;

                case BinaryOperatorExpression.Option.NotEqualBoolean:
                    return (bool)left != (bool)right;

                case BinaryOperatorExpression.Option.NotEqualInteger:
                    return (int)left != (int)right;

                case BinaryOperatorExpression.Option.NotEqualFloating:
                    return !AboutEqual((double)left, (double)right);

                case BinaryOperatorExpression.Option.NotEqualString:
                    return !((string)left).Equals(right);

                case BinaryOperatorExpression.Option.NotEqualObject:
                    return left != right;

                case BinaryOperatorExpression.Option.LessInteger:
                    return (int)left < (int)right;

                case BinaryOperatorExpression.Option.LessFloating:
                    return (double)left < (double)right;

                case BinaryOperatorExpression.Option.LessOrEqualInteger:
                    return (int)left <= (int)right;

                case BinaryOperatorExpression.Option.LessOrEqualFloating:
                    return (double)left <= (double)right;

                case BinaryOperatorExpression.Option.MoreInteger:
                    return (int)left > (int)right;

                case BinaryOperatorExpression.Option.MoreFloating:
                    return (double)left > (double)right;

                case BinaryOperatorExpression.Option.MoreOrEqualInteger:
                    return (int)left >= (int)right;

                case BinaryOperatorExpression.Option.MoreOrEqualFloating:
                    return (double)left >= (double)right;

                default:
                    throw new NotImplementedException($"Not implemented binary: {binary.OperatorOption}");
            }
        }


        private object EvaluateUnaryOperator(UnaryOperatorExpression unary) {
            var value = EvaluateExpression(unary.Expression);
            switch (unary.OperatorOption) {
                case UnaryOperatorExpression.Option.Not:
                    return !(bool)value;

                case UnaryOperatorExpression.Option.NegateInteger:
                    return -(int)value;

                case UnaryOperatorExpression.Option.NegateFloating:
                    return -(double)value;

                case UnaryOperatorExpression.Option.FromMaybe:
                    if (value != null) {
                        return value;
                    } else {
                        throw ProgramException.CreateNullError(unary.MainLocation);
                    }

                case UnaryOperatorExpression.Option.TestNull:
                    return value == null;

                case UnaryOperatorExpression.Option.TestNotNull:
                    return value != null;

                default:
                    throw new VirtualMachineException($"Unsupported unary operator: {unary.OperatorOption}");
            }
        }


        private object EvaluateConversion(ConversionExpression conversion) {
            var value = EvaluateExpression(conversion.Operand);

            switch (conversion.ConversionOption) {
                case ConversionExpression.Option.Char2Int:
                    return (int)(char)value;

                case ConversionExpression.Option.Char2String:
                    return ((char)value).ToString();

                case ConversionExpression.Option.Int2Float:
                    return (double)(int)value;

                case ConversionExpression.Option.Int2Char:
                    var integer = (int)value;
                    if (integer >= 0 && integer <= char.MaxValue) {
                        return (char)integer;
                    } else {
                        throw new ProgramException(
                            ErrorTypeInfo.Option.FormatError, 
                            $"Cannot convert {integer} to a valid character code", 
                            conversion.MainLocation);
                    }

                case ConversionExpression.Option.Bool2String:
                    return (bool)value ? "true" : "false";

                case ConversionExpression.Option.Float2Int:
                    return (int)(double)value;

                case ConversionExpression.Option.Int2String:
                    return ((int)value).ToString();

                case ConversionExpression.Option.Float2String:
                    return ((double)value).ToString(new CultureInfo("en-US"));

                case ConversionExpression.Option.String2Int:
                    if (int.TryParse((string)value, out int resultInt)) {
                        return resultInt;
                    } else {
                        throw new ProgramException(
                            ErrorTypeInfo.Option.FormatError, 
                            $"Cannot convert \"{value}\" to integer", 
                            conversion.MainLocation);
                    }

                case ConversionExpression.Option.String2Float:
                    if (double.TryParse((string)value, NumberStyles.Number, new CultureInfo("en-US"), out double resultFloat)) {
                        return resultFloat;
                    } else {
                        throw new ProgramException(
                            ErrorTypeInfo.Option.FormatError, 
                            $"Cannot convert \"{value}\" to floating", 
                            conversion.MainLocation);
                    }

                case ConversionExpression.Option.String2Bool:
                    switch ((string)value) {
                        case "true":
                            return true;

                        case "false":
                            return false;

                        default:
                            throw new ProgramException(
                                ErrorTypeInfo.Option.FormatError, 
                                $"Cannot convert \"{value}\" to boolean", 
                                conversion.MainLocation);
                    }

                case ConversionExpression.Option.Iterable2List:
                    return new ListAdapter(ConvertToEnumerable(value));

                case ConversionExpression.Option.Iterable2Set:
                    return new HashSet<object>(ConvertToEnumerable(value));

                case ConversionExpression.Option.Iterable2Dictionary:
                    return new DictionaryAdapter(
                        ConvertToEnumerable(value).Select(
                            obj => {
                                var tuple = (TupleAdapter)obj;
                                return (tuple.Items[0], tuple.Items[1]);
                            }
                        )
                    );

                default:
                    throw new VirtualMachineException($"Unsupported conversion: {conversion.ConversionOption}");
            }
        }


        private object EvaluateBuiltinFunction(BuiltinFunctionDefinition.Option option, List<object> arguments) {
            switch (option) {
                case BuiltinFunctionDefinition.Option.Print:
                    return CallPrint(arguments[0], (string)arguments[1]);

                case BuiltinFunctionDefinition.Option.Enumerate:
                    return CallEnumerate(ConvertToEnumerable(arguments[0]));

                case BuiltinFunctionDefinition.Option.Zip:
                    return CallZip(ConvertToEnumerable(arguments[0]), ConvertToEnumerable(arguments[1]));

                case BuiltinFunctionDefinition.Option.Open:
                    return CallOpen((string)arguments[0], (string)arguments[1]);

                case BuiltinFunctionDefinition.Option.Maxi:
                    return Math.Max((int)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.Mini:
                    return Math.Min((int)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.Maxf:
                    return Math.Max((double)arguments[0], (double)arguments[1]);

                case BuiltinFunctionDefinition.Option.Minf:
                    return Math.Min((double)arguments[0], (double)arguments[1]);

                case BuiltinFunctionDefinition.Option.TupleGet:
                    return CallTupleGet((TupleAdapter)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.TupleGetFirst:
                    return CallTupleGet((TupleAdapter)arguments[0], 0);

                case BuiltinFunctionDefinition.Option.TupleGetSecond:
                    return CallTupleGet((TupleAdapter)arguments[0], 1);

                case BuiltinFunctionDefinition.Option.TupleGetThird:
                    return CallTupleGet((TupleAdapter)arguments[0], 2);

                case BuiltinFunctionDefinition.Option.ListGet:
                    return CallListGet((ListAdapter)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.ListGetLength:
                    return CallListGetLength((ListAdapter)arguments[0]);

                case BuiltinFunctionDefinition.Option.ListGetSlice:
                    return CallListGetSlice((ListAdapter)arguments[0], (int)arguments[1], (int?)arguments[2], (int)arguments[3]);

                case BuiltinFunctionDefinition.Option.ListSet:
                    return CallListSet((ListAdapter)arguments[0], (int)arguments[1], arguments[2]);

                case BuiltinFunctionDefinition.Option.ListAppend:
                    return CallListAppend((ListAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.ListExtend:
                    return CallListExtend((ListAdapter)arguments[0], (ListAdapter)arguments[1]);

                case BuiltinFunctionDefinition.Option.ListContains:
                    return CallListContains((ListAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.ListCopy:
                    return CallListCopy((ListAdapter)arguments[0]);

                case BuiltinFunctionDefinition.Option.SetGetLength:
                    return CallSetGetLength((ISet<object>)arguments[0]);

                case BuiltinFunctionDefinition.Option.SetAdd:
                    return CallSetAdd((ISet<object>)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.SetRemove:
                    return CallSetRemove((ISet<object>)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.SetUnion:
                    return CallSetUnion((ISet<object>)arguments[0], (ISet<object>)arguments[1]);

                case BuiltinFunctionDefinition.Option.SetIntersection:
                    return CallSetIntersection((ISet<object>)arguments[0], (ISet<object>)arguments[1]);

                case BuiltinFunctionDefinition.Option.SetDifference:
                    return CallSetDifference((ISet<object>)arguments[0], (ISet<object>)arguments[1]);

                case BuiltinFunctionDefinition.Option.SetContains:
                    return CallSetContains((ISet<object>)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.SetCopy:
                    return CallSetCopy((ISet<object>)arguments[0]);

                case BuiltinFunctionDefinition.Option.DictionaryGet:
                    return CallDictionaryGet((DictionaryAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.DictionaryGetLength:
                    return CallDictionaryGetLength((DictionaryAdapter)arguments[0]);

                case BuiltinFunctionDefinition.Option.DictionarySet:
                    return CallDictionarySet((DictionaryAdapter)arguments[0], arguments[1], arguments[2]);

                case BuiltinFunctionDefinition.Option.DictionaryTryGet:
                    return CallDictionaryTryGet((DictionaryAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.DictionaryContains:
                    return CallDictionaryContains((DictionaryAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.DictionaryCopy:
                    return CallDictionaryCopy((DictionaryAdapter)arguments[0]);

                case BuiltinFunctionDefinition.Option.RangeContains:
                    return CallRangeContains((RangeAdapter)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.IterableContains:
                    return CallIterableContains(ConvertToEnumerable(arguments[0]), arguments[1]);

                case BuiltinFunctionDefinition.Option.StringGet:
                    return CallStringGet((string)arguments[0], (int)arguments[1]);

                case BuiltinFunctionDefinition.Option.StringGetLength:
                    return ((string)arguments[0]).Length;

                case BuiltinFunctionDefinition.Option.StringToLower:
                    return ((string)arguments[0]).ToLower();

                case BuiltinFunctionDefinition.Option.StringToUpper:
                    return ((string)arguments[0]).ToUpper();

                case BuiltinFunctionDefinition.Option.StringSplit:
                    return CallStringSplit((string)arguments[0]);

                case BuiltinFunctionDefinition.Option.StringContains:
                    return ((string)arguments[0]).Contains((string)arguments[1]);

                case BuiltinFunctionDefinition.Option.StringJoin:
                    return CallStringJoin((string)arguments[0], ConvertToEnumerable(arguments[1]));

                case BuiltinFunctionDefinition.Option.StringGetSlice:
                    return CallStringGetSlice((string)arguments[0], (int)arguments[1], (int?)arguments[2], (int)arguments[3]);

                case BuiltinFunctionDefinition.Option.StringReversed:
                    return CallStringReversed((string)arguments[0]);

                case BuiltinFunctionDefinition.Option.StringFind:
                    return CallStringFind((string)arguments[0], (string)arguments[1]);

                case BuiltinFunctionDefinition.Option.StringFindLast:
                    return CallStringFindLast((string)arguments[0], (string)arguments[1]);

                case BuiltinFunctionDefinition.Option.StringEndsWith:
                    return ((string)arguments[0]).EndsWith((string)arguments[1]);

                case BuiltinFunctionDefinition.Option.StringStartsWith:
                    return ((string)arguments[0]).StartsWith((string)arguments[1]);

                case BuiltinFunctionDefinition.Option.FileReadLine:
                    return CallFileReadLine((FileStream)arguments[0]);

                case BuiltinFunctionDefinition.Option.FileWrite:
                    return CallFileWrite((FileStream)arguments[0], (string)arguments[1]);

                case BuiltinFunctionDefinition.Option.FileReset:
                    ((FileStream)arguments[0]).Seek(0, SeekOrigin.Begin);
                    return null;

                case BuiltinFunctionDefinition.Option.FileClose:
                    ((FileStream)arguments[0]).Close();
                    return null;

                case BuiltinFunctionDefinition.Option.ErrorGetMessage:
                    return ((ProgramException)arguments[0]).Message;

                case BuiltinFunctionDefinition.Option.ErrorGetStackTrace:
                    return ErrorStackTraceToString((ProgramException)arguments[0]);

                default:
                    throw new VirtualMachineException($"Unsupported built-in function call: {option}");
            }
        }


        private object CallListAppend(ListAdapter list, object item) {
            if (!list.IsSlice) {
                list.Add(item);
                return null;
            } else {
                throw ProgramException.CreateNotSupportedError("Slice", "append", null);
            }
        }


        private object CallListExtend(ListAdapter listA, ListAdapter listB) {
            if (!listA.IsSlice) {
                listA.Extend(listB);
                return null;
            } else {
                throw ProgramException.CreateNotSupportedError("Slice", "extend", null);
            }
        }


        private object CallListExtended(ListAdapter listA, ListAdapter listB) {
            var copy = (ListAdapter)listA.Clone();
            copy.Extend(listB);
            return copy;
        }


        private object CallListGet(ListAdapter list, int index) {
            var adjusted = GetAdjustedListIndex(index, list.Count);
            return list[adjusted];
        }


        private object CallListSet(ListAdapter list, int index, object item) {
            var adjusted = GetAdjustedListIndex(index, list.Count);
            list[adjusted] = item;
            return null;
        }


        private object CallListGetLength(ListAdapter list) {
            return list.Count;
        }


        private object CallListGetSlice(ListAdapter list, int start, int? end, int step) {
            var maximum = list.Count;
            var startAdjusted = GetAdjustedListIndex(start, maximum);

            var endAdjusted = maximum;
            if (end != null && end.Value != maximum) {
                endAdjusted = GetAdjustedListIndex(end.Value, maximum);
            }

            // Still no support for negative slices
            if (step <= 0) {
                throw new ProgramException(ErrorTypeInfo.Option.ValueError, $"Slice's step must be positive (got {step})", null);
            }

            return list.GetSlice(startAdjusted, endAdjusted, step);
        }


        private object CallListContains(ListAdapter list, object item) {
            return list.Contains(item);
        }


        private object CallListCopy(ListAdapter list) {
            return list.Clone();
        }


        private int GetAdjustedListIndex(int index, int maximum) {
            if (index >= -maximum && index < maximum) {
                return index >= 0 ? index : maximum + index;
            } else {
                throw ProgramException.CreateRangeError(index, maximum, null);
            }
        }


        private object CallSetAdd(ISet<object> set, object item) {
            set.Add(item);
            return null;
        }


        private object CallSetGetLength(ISet<object> set) {
            return set.Count;
        }


        private object CallSetRemove(ISet<object> set, object item) {
            return set.Remove(item);
        }


        private object CallSetContains(ISet<object> set, object item) {
            return set.Contains(item);
        }


        private object CallSetUnion(ISet<object> a, ISet<object> b) {
            return new HashSet<object>(a.Union(b));
        }


        private object CallSetIntersection(ISet<object> a, ISet<object> b) {
            return new HashSet<object>(a.Intersect(b));
        }


        private object CallSetDifference(ISet<object> a, ISet<object> b) {
            return new HashSet<object>(a.Except(b));
        }


        private object CallSetCopy(ISet<object> set) {
            return new HashSet<object>(set);
        }


        private object CallDictionaryGet(DictionaryAdapter dictionary, object key) {
            if (dictionary.TryGetValue(key, out object value)) {
                return value;
            } else {
                throw ProgramException.CreateKeyError(ObjectToString(key, true, false), null);
            }
        }


        private object CallDictionaryTryGet(DictionaryAdapter dictionary, object key) {
            if (dictionary.TryGetValue(key, out object value)) {
                return value;
            } else {
                return null;
            }
        }


        private object CallDictionarySet(DictionaryAdapter dictionary, object key, object value) {
            dictionary[key] = value;
            return null;
        }


        private object CallDictionaryGetLength(DictionaryAdapter dictionary) {
            return dictionary.Count;
        }


        private object CallDictionaryContains(DictionaryAdapter dictionary, object key) {
            return dictionary.ContainsKey(key);
        }


        private object CallDictionaryCopy(DictionaryAdapter dictionary) {
            return dictionary.Clone();
        }


        private object CallTupleGet(TupleAdapter tuple, int index) {
            return tuple.Items[index];
        }


        private object CallRangeContains(RangeAdapter range, int value) {
            return value >= range.Start && value < range.End;
        }


        private object CallIterableContains(IEnumerable<object> sequence, object item) {
            return sequence.Contains(item);
        }


        private object CallStringGet(string s, int index) {
            var adjusted = GetAdjustedListIndex(index, s.Length);
            return s[adjusted];
        }


        private object CallStringSplit(string s) {
            return new ListAdapter(s.Split());
        }


        private object CallStringJoin(string s, IEnumerable<object> items) {
            return string.Join(s, items.Select(item => ObjectToString(item, false, false)));
        }


        private object CallStringGetSlice(string s, int start, int? end, int step) {
            var maximum = s.Length;
            var startAdjusted = GetAdjustedListIndex(start, maximum);

            var endAdjusted = maximum;
            if (end != null && end.Value != maximum) {
                endAdjusted = GetAdjustedListIndex(end.Value, maximum);
            }

            // Still no support for negative slices
            if (step <= 0) {
                throw new ProgramException(ErrorTypeInfo.Option.ValueError, $"Slice's step must be positive (got {step})", null);
            }

            // Build slice
            var builder = new StringBuilder();
            for (var i = startAdjusted; i < endAdjusted; i += step) {
                builder.Append(s[i]);
            }

            return builder.ToString();
        }


        private object CallStringReversed(string s) {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }


        private object CallStringFind(string s, string substring) {
            var index = s.IndexOf(substring);
            if (index == -1) {
                return null;
            } else {
                return index;
            }
        }


        private object CallStringFindLast(string s, string substring) {
            var index = s.LastIndexOf(substring);
            if (index == -1) {
                return null;
            } else {
                return index;
            }
        }


        private object CallFileReadLine(FileStream stream) {
            try {
                var reader = new StreamReader(stream);
                var line = reader.ReadLine();  // maybe null
                                               // TODO: looks like stream reader reads lots of lines at a time
                                               // Next calls of `readLine()` can't see anything
                return line;
            } catch (IOException e) {
                throw new ProgramException(ErrorTypeInfo.Option.IOError, e.Message, null);
            } catch (NotSupportedException e) {
                throw new ProgramException(ErrorTypeInfo.Option.NotSupportedError, e.Message, null);
            }
        }


        private object CallFileWrite(FileStream stream, string line) {
            try {
                var writer = new StreamWriter(stream);
                writer.Write(line);
                writer.Flush();
            } catch (Exception e) {
                throw new ProgramException(ErrorTypeInfo.Option.IOError, e.Message, null);
            }
            return null;
        }


        private object CallOpen(string path, string mode) {
            // TODO: implement modes
            try {
                switch (mode) {
                    case "r":
                        return new FileStream(path, FileMode.Open, FileAccess.Read);

                    case "w":
                        return new FileStream(path, FileMode.Create, FileAccess.Write);

                    case "a":
                        return new FileStream(path, FileMode.Append, FileAccess.Write);

                    default:
                        throw new ProgramException(ErrorTypeInfo.Option.ValueError, $"Invalid mode '{mode}'", null);
                }
            } catch (IOException e) {
                throw new ProgramException(ErrorTypeInfo.Option.IOError, e.Message, null);
            }
        }


        private IEnumerable<object> CallEnumerate(IEnumerable<object> items) {
            var i = 0;
            foreach (var item in items) {
                var tuple = new object[] { i, item };
                i++;
                yield return new TupleAdapter(tuple);
            }
        } 


        private IEnumerable<object> CallZip(IEnumerable<object> itemsA, IEnumerable<object> itemsB) {
            return Enumerable.Zip(itemsA, itemsB, (first, second) => {
                var tuple = new object[] { first, second };
                return (object)new TupleAdapter(tuple);
            });
        }


        private object CallPrint(object argument, string end) {
            ProgramOut.Write(ObjectToString(argument, false, false));
            ProgramOut.Write(end);
            return null;
        }


        private string ObjectToString(object obj, bool isEscaped, bool isTuplePair) {
            if (obj == null) {
                return isEscaped ? "null" : "";
            }

            switch (obj) {
                case bool b:
                    return b ? "true" : "false";

                case char ch:
                    return isEscaped ? $"'{ch}'" : ch.ToString();

                case double d:
                    return d.ToString(new CultureInfo("en-US"));

                case string s:
                    return isEscaped ? $"\"{s}\"" : s;

                case ListAdapter list:
                    return $"[{ObjectListToString(list, true, false)}]";        

                case ISet<object> set:
                    return $"{{{ObjectListToString(set, true, false)}}}";

                case DictionaryAdapter dictionary:
                    return $"{{{ObjectListToString(dictionary, true, true)}}}";

                case RangeAdapter range:
                    return $"{range.Start}..{range.End}";

                case TupleAdapter tuple:
                    if (isTuplePair && tuple.Items.Length == 2) {
                        // Consider a pair
                        return $"{ObjectToString(tuple.Items[0], true, false)}: {ObjectToString(tuple.Items[1], true, false)}";
                    } else {
                        return $"({ObjectListToString(tuple.Items, true, false)})";
                    }

                case ProgramException e:
                    return ErrorToString(e);

                default:
                    return obj.ToString();
            }
        }


        private string ObjectListToString(IEnumerable<object> objs, bool isEscaped, bool isTuplePair) {
            return string.Join(", ", objs.Select(obj => ObjectToString(obj, isEscaped, isTuplePair)));
        }


        private string ErrorToString(ProgramException e) {
            var builder = new StringBuilder();
            builder.Append(ErrorStackTraceToString(e));
            builder.Append($"\n{e.ErrorOption}: {e.Message}");
            return builder.ToString();
        }


        private string ErrorStackTraceToString(ProgramException e) {
            var builder = new StringBuilder();
            var isFirst = true;
            foreach (var location in e.Locations) {
                if (location != null) {
                    if (!isFirst) {
                        builder.Append("\n");
                    }
                    isFirst = false;
                    builder.Append($"  at line {location.LineNumber + 1} at column {location.ColumnNumber + 1}\n");
                    builder.Append($"    {location.Line}");
                    builder.Append($"{new string(' ', location.ColumnNumber + 4)}^");
                }
            }
            return builder.ToString();
        }


        private void PrintError(string message) {
            PrintColorMessage(message, ConsoleColor.Red);
        }


        private void LogInfo(string message) {
            PrintColorMessage(message, ConsoleColor.Yellow, ConsoleColor.Blue);
        }


        private void LogError(string message) {
            PrintColorMessage(message, ConsoleColor.DarkRed, ConsoleColor.Blue);
        }


        private void PrintColorMessage(string message, ConsoleColor foreground, ConsoleColor? background = null) {
            var oldBackgroundColor = Console.BackgroundColor;
            var oldForegroundColor = Console.ForegroundColor;

            if (background != null) {
                Console.BackgroundColor = background.Value;
            }
            Console.ForegroundColor = foreground;

            Console.WriteLine(message);

            Console.BackgroundColor = oldBackgroundColor;
            Console.ForegroundColor = oldForegroundColor;
        }


        public static bool AboutEqual(double x, double y) {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1e-10;
            return Math.Abs(x - y) <= epsilon;
        }
    }
}

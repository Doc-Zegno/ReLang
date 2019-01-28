using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Parsing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Runtime {
    public class VirtualMachine {
        private List<List<object>> frames;
        private List<FunctionData> functions;
        private object functionValue;
        private bool needReturn;

        //public System.IO.TextWriter VmOut { get; }
        public System.IO.TextWriter ProgramOut { get; }
        public System.IO.TextWriter ProgramErr { get; }


        public VirtualMachine(System.IO.TextWriter programOut, System.IO.TextWriter programErr) {
            //VmOut = vmOut;
            ProgramOut = programOut;
            ProgramErr = programErr;
        }


        public int Execute(ParsedProgram program, string[] commandLineArguments) {
            frames = new List<List<object>>();
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
            foreach (var location in e.Locations) {
                if (location != null) {
                    ProgramErr.WriteLine($"  at line {location.LineNumber + 1} at column {location.ColumnNumber + 1}");
                    ProgramErr.Write($"    {location.Line}");
                    ProgramErr.WriteLine($"{new string(' ', location.ColumnNumber + 4)}^\n");
                }
            }
            ProgramErr.WriteLine($"{e.ErrorOption}: {e.Message}\n");

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
            EnterFrame();

            // Push arguments
            foreach (var argument in arguments) {
                CreateVariable(argument);
            }

            // Execute body
            functionValue = null;
            needReturn = false;
            var function = functions[number];

            try {
                ExecuteStatementList(function.Body);
            } finally {
                LeaveFrame();
            }

            // Leave frame and return function value
            needReturn = false;
            var result = functionValue;
            functionValue = null;  // No one will see this value outside evaluation anymore
            return result;
        }


        private void ExecuteStatementList(List<IStatement> statements) {
            foreach (var statement in statements) {
                if (!needReturn) {
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
                        EnterFrame();
                        try {
                            ExecuteStatementList(statements);
                        } finally {
                            LeaveFrame();
                        }
                    }
                    break;

                case ForEachStatement forEach:
                    //Log("Executing for-each...");
                    statements = forEach.Statements;
                    var iterable = ConvertToEnumerable(EvaluateExpression(forEach.Iterable));
                    EnterFrame();
                    try {
                        foreach (var item in iterable) {
                            ClearFrame();
                            CreateVariable(item);
                            ExecuteStatementList(statements);
                        }
                    } finally {
                        LeaveFrame();
                    }
                    break;

                case ExpressionStatement expression:
                    EvaluateExpression(expression.Expression);
                    break;

                case VariableDeclarationStatement variableDeclaration:
                    value = EvaluateExpression(variableDeclaration.Value);
                    CreateVariable(value);
                    break;

                case AssignmentStatement assignment:
                    value = EvaluateExpression(assignment.Value);
                    SetVariable(assignment.Number, assignment.FrameOffset, value);
                    break;

                case ReturnStatement returnStatement:
                    functionValue = EvaluateExpression(returnStatement.Operand);
                    needReturn = true;
                    break;

                case CompoundStatement compound:
                    ExecuteStatementList(compound.Statements);
                    break;

                default:
                    throw new VirtualMachineException($"Unsupported statement: {statement}");
            }
        }


        private IEnumerable<object> ConvertToEnumerable(object value) {
            if (value is string str) {
                return GetStringEnumerable(str);
            } else {
                return (IEnumerable<object>)value;
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

                case ILiteralExpression literal:
                    switch (literal) {
                        case PrimitiveLiteralExpression primitiveLiteral:
                            return primitiveLiteral.Value;

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
                    return GetVariable(variable.Number, variable.FrameOffset);

                default:
                    throw new VirtualMachineException($"Unsupported expression: {expression}");
            }
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
            // Short-circuit evaluation for booleans
            switch (binary.OperatorOption) {
                case BinaryOperatorExpression.Option.Or:
                    if ((bool)EvaluateExpression(binary.LeftOperand)) {
                        return true;
                    } else {
                        return (bool)EvaluateExpression(binary.RightOperang);
                    }

                case BinaryOperatorExpression.Option.And:
                    if (!(bool)EvaluateExpression(binary.LeftOperand)) {
                        return false;
                    } else {
                        return (bool)EvaluateExpression(binary.RightOperang);
                    }
            }

            // Long-circuit for other types
            var left = EvaluateExpression(binary.LeftOperand);
            var right = EvaluateExpression(binary.RightOperang);

            switch (binary.OperatorOption) {
                case BinaryOperatorExpression.Option.AddInteger:
                    return (int)left + (int)right;

                case BinaryOperatorExpression.Option.AddFloating:
                    return (double)left + (double)right;

                case BinaryOperatorExpression.Option.AddString:
                    return (string)left + (string)right;

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
            }
            return null;
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
                            ProgramException.Option.FormatError, 
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
                            ProgramException.Option.FormatError, 
                            $"Cannot convert \"{value}\" to integer", 
                            conversion.MainLocation);
                    }

                case ConversionExpression.Option.String2Float:
                    if (double.TryParse((string)value, NumberStyles.Number, new CultureInfo("en-US"), out double resultFloat)) {
                        return resultFloat;
                    } else {
                        throw new ProgramException(
                            ProgramException.Option.FormatError, 
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
                                ProgramException.Option.FormatError, 
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
                    return CallPrint(arguments[0]);

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

                case BuiltinFunctionDefinition.Option.ListSet:
                    return CallListSet((ListAdapter)arguments[0], (int)arguments[1], arguments[2]);

                case BuiltinFunctionDefinition.Option.ListAppend:
                    return CallListAppend((ListAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.ListExtend:
                    return CallListExtend((ListAdapter)arguments[0], (ListAdapter)arguments[1]);

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

                case BuiltinFunctionDefinition.Option.DictionaryContains:
                    return CallDictionaryContains((DictionaryAdapter)arguments[0], arguments[1]);

                case BuiltinFunctionDefinition.Option.DictionaryCopy:
                    return CallDictionaryCopy((DictionaryAdapter)arguments[0]);

                default:
                    throw new VirtualMachineException($"Unsupported built-in function call: {option}");
            }
        }


        private object CallListAppend(ListAdapter list, object item) {
            list.Add(item);
            return null;
        }


        private object CallListExtend(ListAdapter listA, ListAdapter listB) {
            listA.Extend(listB);
            return null;
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


        private object CallPrint(object argument) {
            ProgramOut.WriteLine(ObjectToString(argument, false, false));
            return null;
        }


        private string ObjectToString(object obj, bool isEscaped, bool isTuplePair) {
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

                default:
                    return obj.ToString();
            }
        }


        private string ObjectListToString(IEnumerable<object> objs, bool isEscaped, bool isTuplePair) {
            return string.Join(", ", objs.Select(obj => ObjectToString(obj, isEscaped, isTuplePair)));
        }


        private void CreateVariable(object value) {
            var frame = frames.Last();;
            frame.Add(value);
            //Log($"Added variable, total count: {frame.Count}");
        }


        private object GetVariable(int number, int frameOffset) {
            //Log($"Requested variable <{number}:{frameOffset}>");

            var index = frames.Count + frameOffset - 1;
            //Log($"Calculated frame index: {index}");

            var frame = frames[index];
            //Log($"Frame size: {frame.Count}");

            return frame[number];
        }


        private void SetVariable(int number, int frameOffset, object value) {
            var index = frames.Count + frameOffset - 1;
            frames[index][number] = value;
        }


        private void ClearFrame() {
            var frame = frames.Last();
            frame.Clear();
            //Log("Frame was cleared");
        }


        private void EnterFrame() {
            frames.Add(new List<object>());
        }


        private void LeaveFrame() {
            frames.RemoveAt(frames.Count - 1);
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

using System;
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


        public VirtualMachine(System.IO.TextWriter programOut) {
            //VmOut = vmOut;
            ProgramOut = programOut;
        }


        public int Execute(ParsedProgram program, string[] commandLineArguments) {
            frames = new List<List<object>>();
            functions = program.Functions;
            functionValue = null;
            Log("Executing main()...");

            // Converting command line arguments to external representation
            var arguments = ConvertArguments(commandLineArguments);

            var maybe = EvaluateCustomFunction(program.MainFunctionNumber, arguments);
            var result = 0;
            if (maybe != null) {
                result = (int)maybe;
            }
            Log($"Process finished with exit code: {result}");
            return result;
        }


        private List<IExpression> ConvertArguments(string[] commandLineArguments) {
            var itemType = PrimitiveTypeInfo.String;
            var items = new List<IExpression>();
            foreach (var arg in commandLineArguments) {
                items.Add(new PrimitiveLiteralExpression(arg, itemType));
            }
            var list = new ListLiteralExpression(items, itemType);
            return new List<IExpression> { list };
        }


        private object EvaluateCustomFunction(int number, List<IExpression> arguments) {
            var values = new List<object>();
            foreach (var argument in arguments) {
                values.Add(EvaluateExpression(argument));
            }

            EnterFrame();

            // Push arguments
            foreach (var value in values) {
                CreateVariable(value);
            }

            // Execute body
            functionValue = null;
            needReturn = false;
            var function = functions[number];
            ExecuteStatementList(function.Body);

            // Leave frame and return function value
            needReturn = false;
            LeaveFrame();
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
                        ExecuteStatementList(statements);
                        LeaveFrame();
                    }
                    break;

                case ForEachStatement forEach:
                    //Log("Executing for-each...");
                    statements = forEach.Statements;
                    var iterable = (IEnumerable<object>)EvaluateExpression(forEach.Iterable);
                    EnterFrame();
                    foreach (var item in iterable) {
                        ClearFrame();
                        CreateVariable(item);
                        ExecuteStatementList(statements);
                    }
                    LeaveFrame();
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


        private object EvaluateExpression(IExpression expression) {
            switch (expression) {
                case IFunctionCallExpression functionCall:
                    var arguments = functionCall.Arguments;
                    switch (functionCall) {
                        case BuiltinFunctionCallExpression builtin:
                            return EvaluateBuiltinFunction(builtin.BuiltinOption, arguments);

                        case CustomFunctionCallExpression custom:
                            return EvaluateCustomFunction(custom.Number, arguments);

                        default:
                            throw new VirtualMachineException("Unsupported function call expression");
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
                            var list = new List<object>();
                            foreach (var item in listLiteral.Items) {
                                list.Add(EvaluateExpression(item));
                            }
                            return list;

                        case SetLiteralExpression setLiteral:
                            var set = new HashSet<object>();
                            foreach (var item in setLiteral.Items) {
                                set.Add(EvaluateExpression(item));
                            }
                            return set;

                        case RangeLiteralExpression rangeLiteral:
                            var start = (int)EvaluateExpression(rangeLiteral.Start);
                            var end = (int)EvaluateExpression(rangeLiteral.End);
                            return new RangeAdapter(start, end);

                        case TupleLiteralExpression tupleLiteral:
                            var tuple = new object[tupleLiteral.Items.Count];
                            for (var i = 0; i < tuple.Length; i++) {
                                tuple[i] = EvaluateExpression(tupleLiteral.Items[i]);
                            }
                            return tuple;

                        default:
                            throw new VirtualMachineException($"Unknown literal expression: {literal}");
                    } 

                case VariableExpression variable:
                    return GetVariable(variable.Number, variable.FrameOffset);

                default:
                    throw new VirtualMachineException($"Unsupported expression: {expression}");
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
            switch (conversion.ConversionOption) {
                case ConversionExpression.Option.Int2Float:
                    return (double)(int)EvaluateExpression(conversion.Operand);

                default:
                    throw new VirtualMachineException($"Unsupported conversion: {conversion.ConversionOption}");
            }
        }


        private object EvaluateBuiltinFunction(
            BuiltinFunctionCallExpression.Option option, 
            List<IExpression> arguments) 
        {
            switch (option) {
                case BuiltinFunctionCallExpression.Option.Print:
                    CallPrint(EvaluateExpression(arguments[0]));
                    return null;

                case BuiltinFunctionCallExpression.Option.TupleGet:
                    return CallTupleGet(
                        (object[])EvaluateExpression(arguments[0]),
                        (int)EvaluateExpression(arguments[1])
                    );

                default:
                    throw new VirtualMachineException($"Unsupported built-in function call: {option}");
            }
        }


        private object CallTupleGet(object[] tuple, int index) {
            return tuple[index];
        }


        private void CallPrint(object argument) {
            PrintObject(argument, false);
            ProgramOut.WriteLine();
        }


        private void PrintObject(object obj, bool isEscaped) {
            switch (obj) {
                case bool b:
                    ProgramOut.Write(b ? "true" : "false");
                    break;

                case string s:
                    if (isEscaped) {
                        ProgramOut.Write($"\"{s}\"");
                    } else {
                        ProgramOut.Write(s);
                    }
                    break;

                case List<object> list:
                    ProgramOut.Write("[");
                    PrintObjectList(list, true);
                    ProgramOut.Write("]");
                    break;

                case ISet<object> set:
                    ProgramOut.Write("{");
                    PrintObjectList(set, true);
                    ProgramOut.Write("}");
                    break;

                case RangeAdapter range:
                    ProgramOut.Write($"{range.Start}..{range.End}");
                    break;

                case object[] tuple:
                    ProgramOut.Write("(");
                    PrintObjectList(tuple, true);
                    ProgramOut.Write(")");
                    break;

                default:
                    ProgramOut.Write(obj);
                    break;
            }
        }


        private void PrintObjectList(IEnumerable<object> objs, bool isEscaped) {
            var isFirst = true;
            foreach (var obj in objs) {
                if (!isFirst) {
                    ProgramOut.Write(", ");
                }
                isFirst = false;
                PrintObject(obj, isEscaped);
            }
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


        private void Log(string message) {
            var oldBackgroundColor = Console.BackgroundColor;
            var oldForegroundColor = Console.ForegroundColor;

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;

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

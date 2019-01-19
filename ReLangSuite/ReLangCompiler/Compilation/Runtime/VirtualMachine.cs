using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Parsing;


namespace Handmada.ReLang.Compilation.Runtime {
    class VirtualMachine {
        private List<List<object>> frames;
        private List<FunctionData> functions;
        private object functionValue;


        public void Execute(ParsedProgram program) {
            frames = new List<List<object>>();
            functions = program.Functions;
            functionValue = null;
            Log("Executing main()...");
            EvaluateCustomFunction(program.MainFunctionNumber);
            Log("Done");
        }


        private object EvaluateCustomFunction(int number) {
            EnterFrame();
            var function = functions[number];
            ExecuteStatementList(function.Body);
            LeaveFrame();
            return functionValue;
        }


        private void ExecuteStatementList(List<IStatement> statements) {
            foreach (var statement in statements) {
                ExecuteStatement(statement);
            }
        }


        private void ExecuteStatement(IStatement statement) {
            List<IStatement> statements;
            switch (statement) {
                case ConditionalStatement conditional:
                    statements = conditional.IfStatements;
                    if (!(bool)EvaluateExpression(conditional.Condition)) {
                        statements = conditional.ElseStatements;
                    }

                    EnterFrame();
                    ExecuteStatementList(statements);
                    LeaveFrame();
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
                    var value = EvaluateExpression(variableDeclaration.Value);
                    CreateVariable(value);
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
                            return EvaluateCustomFunction(custom.Number);

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

                case LiteralExpression literal:
                    return literal.Value;

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

                default:
                    throw new VirtualMachineException($"Unsupported built-in function call: {option}");
            }
        }


        private void CallPrint(object argument) {
            PrintObject(argument, false);
            Console.WriteLine();
        }


        private void PrintObject(object obj, bool isEscaped) {
            switch (obj) {
                case bool b:
                    Console.Write(b ? "true" : "false");
                    break;

                case string s:
                    if (isEscaped) {
                        Console.Write($"\"{s}\"");
                    } else {
                        Console.Write(s);
                    }
                    break;

                case List<object> list:
                    Console.Write("[");
                    PrintObjectList(list, true);
                    Console.Write("]");
                    break;

                case ISet<object> set:
                    Console.Write("{");
                    PrintObjectList(set, true);
                    Console.Write("}");
                    break;

                default:
                    Console.Write(obj);
                    break;
            }
        }


        private void PrintObjectList(IEnumerable<object> objs, bool isEscaped) {
            var isFirst = true;
            foreach (var obj in objs) {
                if (!isFirst) {
                    Console.Write(", ");
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
    }
}

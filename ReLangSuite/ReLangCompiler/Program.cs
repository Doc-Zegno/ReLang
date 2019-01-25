using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Parsing;
using Handmada.ReLang.Compilation.Runtime;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation {
    class Program {
        static void Main(string[] args) {
            var lines = new List<string>();
            using (var stream = new System.IO.StreamReader("input.txt")) {
                var line = "";
                while ((line = stream.ReadLine()) != null) {
                    lines.Add($"{line}\n");
                }
            }

            try {
                var parser = new Parser(lines);
                var program = parser.ParseProgram();

                Console.WriteLine("\n=================\n");
                Console.WriteLine($"Entry point: #{program.MainFunctionNumber}");
                for (var i = 0; i < program.Functions.Count; i++) {
                    PrintFunction(program.Functions[i], i);
                    Console.WriteLine();
                }

                Console.WriteLine("\n=================\n");
                var machine = new VirtualMachine(Console.Out);
                machine.Execute(program, args);

                /*var statements = parser.Parse();

                foreach (var statement in statements) {
                    PrintStatement(statement, 0);
                }*/
            } catch (LexerException e) {
                PrintError("Lexical", e.Message, e.Line, e.LineNumber, e.ColumnNumber);
            } catch (ParserException e) {
                PrintError("Syntax", e.Message, e.Line, e.LineNumber, e.ColumnNumber);
            }
        }


        private static void PrintError(string prefix, string message, string line, int row, int column) {
            Console.WriteLine($"{prefix} error ({row}:{column}): {message}");
            Console.Write($">>> {line}");
            Console.WriteLine($"{new string(' ', column + 4)}^");
        }


        private static void PrintFunction(FunctionData function, int number) {
            var argumentStrings = new List<string>();
            for (var i = 0; i < function.ArgumentTypes.Count; i++) {
                argumentStrings.Add($"{function.ArgumentNames[i]}: {function.ArgumentTypes[i].Name}");
            }
            var arguments = string.Join(", ", argumentStrings);

            Console.WriteLine($"func {function.FullQualification}.{function.Name}<#{number}>"
                              + $"({arguments}) -> {function.ResultType.Name} {{");
            foreach (var s in function.Body) {
                PrintStatement(s, 1);
            }
            Console.WriteLine("}");
        }


        private static void PrintStatement(IStatement statement, int shiftLevel) {
            if (statement is CompoundStatement compound) {
                foreach (var s in compound.Statements) {
                    PrintStatement(s, shiftLevel);
                }
                return;
            }

            var padding = new string(' ', 4 * shiftLevel);
            Console.Write(padding);

            switch (statement) {
                /*case FunctionDeclarationStatement functionDeclaration:
                    Console.WriteLine($"func {functionDeclaration.Name}() {{");
                    foreach (var s in functionDeclaration.Body) {
                        PrintStatement(s, shiftLevel + 1);
                    }
                    Console.WriteLine($"{padding}}}\n");
                    break;*/

                case ConditionalStatement conditional:
                    Console.Write("if ");
                    PrintExpression(conditional.Condition);
                    Console.WriteLine(" {");
                    foreach (var s in conditional.IfStatements) {
                        PrintStatement(s, shiftLevel + 1);
                    }

                    // else-clause
                    if (conditional.ElseStatements != null) {
                        Console.WriteLine($"{padding}}} else {{");
                        foreach (var s in conditional.ElseStatements) {
                            PrintStatement(s, shiftLevel + 1);
                        }
                    }

                    Console.WriteLine($"{padding}}}");
                    break;

                case ForEachStatement forEach:
                    Console.Write($"for {forEach.ItemName} in ");
                    PrintExpression(forEach.Iterable);
                    Console.WriteLine(" {");
                    foreach (var s in forEach.Statements) {
                        PrintStatement(s, shiftLevel + 1);
                    }
                    Console.WriteLine(padding + "}");
                    break;

                case VariableDeclarationStatement variableDeclaration:
                    var prefix = variableDeclaration.IsMutable ? "var" : "let";
                    var typeName = variableDeclaration.Value.TypeInfo.Name;
                    Console.Write($"{prefix} {variableDeclaration.Name}: {typeName} = ");
                    PrintExpression(variableDeclaration.Value);
                    Console.WriteLine("");
                    break;

                case AssignmentStatement assignment:
                    Console.Write($"{assignment.Name} = ");
                    PrintExpression(assignment.Value);
                    Console.WriteLine();
                    break;

                case ExpressionStatement expression:
                    PrintExpression(expression.Expression);
                    Console.WriteLine("");
                    break;

                case ReturnStatement returnStatement:
                    Console.Write("return ");
                    PrintExpression(returnStatement.Operand);
                    Console.WriteLine();
                    break;

                default:
                    Console.WriteLine("<!> Unknown statement <!>");
                    break;
            }
        }


        private static void PrintExpression(IExpression expression) {
            switch (expression) {
                case VariableExpression variable:
                    var number = variable.Number;
                    var frameOffset = variable.FrameOffset;
                    Console.Write($"{variable.Name}<{number}:{frameOffset}>");
                    break;

                case IFunctionCallExpression functionCall:
                    var fullName = "";
                    if (functionCall is BuiltinFunctionCallExpression builtin) {
                        var name = "";
                        switch (builtin.BuiltinOption) {
                            case BuiltinFunctionCallExpression.Option.Print:
                                name = "print";
                                break;

                            case BuiltinFunctionCallExpression.Option.TupleGet:
                                name = "tupleGet";
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        fullName = $"_{name}";
                    } else {
                        var custom = (CustomFunctionCallExpression)functionCall;
                        fullName = $"_customFunction{custom.Number}";
                    }

                    Console.Write($"{fullName}(");
                    var isFirst = true;
                    foreach (var argument in functionCall.Arguments) {
                        if (!isFirst) {
                            Console.Write(", ");
                        }
                        isFirst = false;
                        PrintExpression(argument);
                    }
                    Console.Write(")");
                    break;

                case IOperatorExpression operatorExpression:
                    switch (operatorExpression) {
                        case BinaryOperatorExpression binary:
                            Console.Write("(");
                            PrintExpression(binary.LeftOperand);
                            Console.Write(" ");
                            PrintBinaryOperator(binary.OperatorOption);
                            Console.Write(" ");
                            PrintExpression(binary.RightOperang);
                            Console.Write(")");
                            break;

                        case UnaryOperatorExpression unary:
                            PrintUnaryOperator(unary.OperatorOption);
                            PrintExpression(unary.Expression);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case ConversionExpression conversion:
                    PrintConversion(conversion);
                    break;

                case ILiteralExpression literal:
                    switch (literal) {
                        case PrimitiveLiteralExpression primitiveLiteral:
                            var representation = "";
                            switch (primitiveLiteral.Value) {
                                case bool value:
                                    representation = value ? "true" : "false";
                                    break;

                                case char value:
                                    representation = $"'{value}'";
                                    break;

                                case int value:
                                    representation = value.ToString();
                                    break;

                                case double value:
                                    representation = value.ToString();
                                    break;

                                case string value:
                                    representation = $"\"{value}\"";
                                    break;

                                default:
                                    throw new NotImplementedException($"Unsupported literal type: {primitiveLiteral.Value.GetType().Name}");
                            }
                            Console.Write(representation);
                            break;

                        case ListLiteralExpression listLiteral:
                            Console.Write("[");
                            PrintExpressionList(listLiteral.Items);
                            Console.Write("]");
                            break;

                        case SetLiteralExpression setLiteral:
                            Console.Write("{");
                            PrintExpressionList(setLiteral.Items);
                            Console.Write("}");
                            break;

                        case DictionaryLiteralExpression dictionaryLiteral:
                            Console.Write("{");
                            PrintExpressionPairs(dictionaryLiteral.Pairs);
                            Console.Write("}");
                            break;

                        case RangeLiteralExpression rangeLiteral:
                            PrintExpression(rangeLiteral.Start);
                            Console.Write("..");
                            PrintExpression(rangeLiteral.End);
                            break;

                        case TupleLiteralExpression tupleLiteral:
                            Console.Write("(");
                            PrintExpressionList(tupleLiteral.Items);
                            Console.Write(")");
                            break;
                    }
                    break;

                default:
                    Console.Write("<!> Unknown expression <!>");
                    break;
            }
            //Console.Write($"<{expression.TypeInfo.Name}>");
        }


        private static void PrintConversion(ConversionExpression conversion) {
            Console.Write($"{conversion.TypeInfo.Name}(");
            PrintExpression(conversion.Operand);
            Console.Write(")");
        }


        private static void PrintExpressionPairs(List<(IExpression, IExpression)> pairs) {
            var isFirst = true;
            foreach (var (key, value) in pairs) {
                if (!isFirst) {
                    Console.Write(", ");
                }
                isFirst = false;
                PrintExpression(key);
                Console.Write(": ");
                PrintExpression(value);
            }
        }


        private static void PrintExpressionList(List<IExpression> expressions) {
            var isFirst = true;
            foreach (var expression in expressions) {
                if (!isFirst) {
                    Console.Write(", ");
                }
                isFirst = false;
                PrintExpression(expression);
            }
        }


        private static void PrintBinaryOperator(BinaryOperatorExpression.Option option) {
            switch (option) {
                case BinaryOperatorExpression.Option.AddInteger:
                case BinaryOperatorExpression.Option.AddFloating:
                case BinaryOperatorExpression.Option.AddString:
                    Console.Write("+");
                    break;

                case BinaryOperatorExpression.Option.SubtractInteger:
                case BinaryOperatorExpression.Option.SubtractFloating:
                    Console.Write("-");
                    break;

                case BinaryOperatorExpression.Option.MultiplyInteger:
                case BinaryOperatorExpression.Option.MultiplyFloating:
                    Console.Write("*");
                    break;

                case BinaryOperatorExpression.Option.DivideInteger:
                    Console.Write("\\");
                    break;

                case BinaryOperatorExpression.Option.DivideFloating:
                    Console.Write("/");
                    break;

                case BinaryOperatorExpression.Option.Modulo:
                    Console.Write("%");
                    break;

                case BinaryOperatorExpression.Option.And:
                    Console.Write("&&");
                    break;

                case BinaryOperatorExpression.Option.Or:
                    Console.Write("||");
                    break;

                case BinaryOperatorExpression.Option.EqualBoolean:
                case BinaryOperatorExpression.Option.EqualInteger:
                case BinaryOperatorExpression.Option.EqualFloating:
                case BinaryOperatorExpression.Option.EqualString:
                case BinaryOperatorExpression.Option.EqualObject:
                    Console.Write("==");
                    break;

                case BinaryOperatorExpression.Option.NotEqualBoolean:
                case BinaryOperatorExpression.Option.NotEqualInteger:
                case BinaryOperatorExpression.Option.NotEqualFloating:
                case BinaryOperatorExpression.Option.NotEqualString:
                case BinaryOperatorExpression.Option.NotEqualObject:
                    Console.Write("!=");
                    break;

                case BinaryOperatorExpression.Option.LessInteger:
                case BinaryOperatorExpression.Option.LessFloating:
                    Console.Write("<");
                    break;

                case BinaryOperatorExpression.Option.LessOrEqualInteger:
                case BinaryOperatorExpression.Option.LessOrEqualFloating:
                    Console.Write("<=");
                    break;

                case BinaryOperatorExpression.Option.MoreInteger:
                case BinaryOperatorExpression.Option.MoreFloating:
                    Console.Write(">");
                    break;

                case BinaryOperatorExpression.Option.MoreOrEqualInteger:
                case BinaryOperatorExpression.Option.MoreOrEqualFloating:
                    Console.Write(">=");
                    break;

                default:
                    throw new NotImplementedException();
            }
        }


        private static void PrintUnaryOperator(UnaryOperatorExpression.Option option) {
            switch (option) {
                case UnaryOperatorExpression.Option.Not:
                    Console.Write("!");
                    break;

                case UnaryOperatorExpression.Option.NegateFloating:
                case UnaryOperatorExpression.Option.NegateInteger:
                    Console.Write("-");
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}

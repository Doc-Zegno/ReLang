using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Parsing;
using Handmada.ReLang.Compilation.Runtime;


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
                var machine = new VirtualMachine();
                machine.Execute(program);

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
            Console.WriteLine($"func {function.FullQualification}.{function.Name}<#{number}>() -> {function.ResultType.Name} {{");
            foreach (var s in function.Body) {
                PrintStatement(s, 1);
            }
            Console.WriteLine("}");
        }


        private static void PrintStatement(IStatement statement, int shiftLevel) {
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

                case VariableDeclarationStatement variableDeclaration:
                    var prefix = variableDeclaration.IsMutable ? "var" : "let";
                    var typeName = variableDeclaration.Value.TypeInfo.Name;
                    Console.Write($"{prefix} {variableDeclaration.Name}: {typeName} = ");
                    PrintExpression(variableDeclaration.Value);
                    Console.WriteLine("");
                    break;

                case ExpressionStatement expression:
                    PrintExpression(expression.Expression);
                    Console.WriteLine("");
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
                        }
                        fullName = $"<built-in function: {name}>";
                    } else {
                        var custom = (CustomFunctionCallExpression)functionCall;
                        fullName = $"<custom function #{custom.Number}>";
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
                            PrintExpression(binary.LeftOperand);
                            Console.Write(" ");
                            PrintBinaryOperator(binary.OperatorOption);
                            Console.Write(" ");
                            PrintExpression(binary.RightOperang);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case ConversionExpression conversion:
                    switch (conversion.ConversionOption) {
                        case ConversionExpression.Option.Int2Float:
                            Console.Write("Float(");
                            PrintExpression(conversion.Operand);
                            Console.Write(")");
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case LiteralExpression literal:
                    var representation = "";
                    switch (literal.Value) {
                        case bool value:
                            representation = value ? "true" : "false";
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
                    }
                    Console.Write(representation);
                    break;

                default:
                    Console.Write("<!> Unknown expression <!>");
                    break;
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

                case BinaryOperatorExpression.Option.And:
                    Console.Write("&&");
                    break;

                case BinaryOperatorExpression.Option.Or:
                    Console.Write("||");
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Parsing;


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
                var statements = parser.Parse();

                foreach (var statement in statements) {
                    PrintStatement(statement, 0);
                }
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


        private static void PrintStatement(IStatement statement, int shiftLevel) {
            var padding = new string(' ', 4 * shiftLevel);
            Console.Write(padding);

            switch (statement) {
                case FunctionDeclarationStatement functionDeclaration:
                    Console.WriteLine($"func {functionDeclaration.Name}() {{");
                    foreach (var s in functionDeclaration.Body) {
                        PrintStatement(s, shiftLevel + 1);
                    }
                    Console.WriteLine($"{padding}}}\n");
                    break;

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
                    var name = "";
                    if (functionCall is BuiltinFunctionCallExpression builtin) {
                        switch (builtin.BuiltinOption) {
                            case BuiltinFunctionCallExpression.Option.Print:
                                name = "print";
                                break;
                        }
                    }

                    Console.Write($"{name}(");
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

                case LiteralExpression literal:
                    var representation = "";
                    switch (literal.Value) {
                        case bool value:
                            representation = value ? "true" : "false";
                            break;

                        case int value:
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
    }
}

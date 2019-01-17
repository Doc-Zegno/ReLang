﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;


namespace Handmada.ReLang.Compilation.Parsing {
    class Parser {
        private List<Lexeme> lexemes;
        private IEnumerator<Lexeme> lexemeEnumerator;
        private Lexeme currentLexeme;
        private List<FunctionData> functions;
        private int? mainFunctionNumber;
        private FunctionTree functionTree;
        private ScopeStack scopeStack;


        public Parser(IEnumerable<string> lines) {
            lexemes = new List<Lexeme>();

            var lexer = new Lexer(lines);
            while (true) {
                var lexeme = lexer.GetNextLexeme();
                if (lexeme != null) {
                    lexemes.Add(lexeme);
                } else {
                    break;
                }
            }

            functions = new List<FunctionData>();
            functionTree = new FunctionTree();
            scopeStack = new ScopeStack();
        }


        public ParsedProgram ParseProgram() {
            // Collect function definitions
            ResetLexemes();
            MoveNextLexeme();
            BuildFunctionTree();
            functionTree.PrintTree();

            // Parse program
            ResetLexemes();
            MoveNextLexeme();
            Parse();

            if (mainFunctionNumber == null) {
                RaiseError("End of file was reached but 'main' function wasn't found");
            }

            return new ParsedProgram(functions, mainFunctionNumber.Value);
        }


        private void BuildFunctionTree() {
            var balance = 0;
            do {
                if (WhetherOperator(OperatorMeaning.Func)) {
                    MoveNextLexeme();
                    StepInFunction();
                } else if (WhetherOperator(OperatorMeaning.OpenBrace)) {
                    balance++;
                } else if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                    balance--;
                    if (balance < 0) {
                        break;
                    }
                }
            } while (MoveNextLexeme());
        }


        private void StepInFunction() {
            var location = currentLexeme.StartLocation;
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            var resultType = new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Void);
            var argumentTypes = new List<ITypeInfo>();
            if (!functionTree.DeclareFunction(name, resultType, argumentTypes)) {
                RaiseError($"Declaration of function '{name}' interferes with another declaration", location);
            }
            // Entered a function scope
            BuildFunctionTree();
            // Leave a function scope
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
        }


        private void Parse() {
            do {
                if (WhetherOperator(OperatorMeaning.NewLine)) {
                    MoveNextLexeme();
                    continue;
                }

                CheckOperator(OperatorMeaning.Func);
                ParseFunction();
            } while (currentLexeme != null);
        }


        private void ParseFunction() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            // Reserve place for function data
            var number = functions.Count;
            functions.Add(null);

            Console.WriteLine($"parsing function '{name}'...");

            // Parse body
            functionTree.EnterScope(name);
            scopeStack.EnterScope(isStrong: true);
            var body = GetStatementList(true);
            scopeStack.LeaveScope();
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);

            Console.WriteLine($"stopped parsing function '{name}'");

            // Add function to list
            var maybe = functionTree.GetFunctionDefinition(name);
            var definition = maybe.Value;
            functions[number] = new FunctionData(name, definition.FullQualification, definition.ResultType,
                                                 definition.ArgumentTypes, body);

            // Check for main
            if (definition.IsGlobal && name == "main") {
                mainFunctionNumber = definition.Number;
            }
        }


        private List<IStatement> GetStatementList(bool isScopeStrong) {
            var statements = new List<IStatement>();
            var isTop = true;

            while (currentLexeme != null) { 
                if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                    break;
                }

                if (WhetherOperator(OperatorMeaning.NewLine)) {
                    MoveNextLexeme();
                } else if (WhetherOperator(OperatorMeaning.Func)) {
                    if (isScopeStrong && isTop) {
                        MoveNextLexeme();
                        ParseFunction();
                    } else {
                        RaiseError("Function must be declared at the top of either global or another function's block");
                    }
                } else {
                    statements.Add(GetStatement());
                    isTop = false;
                }
            }

            Console.WriteLine("stopped parsing block");
            
            return statements;
        }


        private IStatement GetStatement() {
            switch (currentLexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        /*case OperatorMeaning.Func:
                            MoveNextLexeme();
                            return GetFunctionDeclaration();*/

                        case OperatorMeaning.If:
                            MoveNextLexeme();
                            return GetConditional();

                        case OperatorMeaning.Var:
                            MoveNextLexeme();
                            return GetVariableDeclaration(isMutable: true);

                        case OperatorMeaning.Let:
                            MoveNextLexeme();
                            return GetVariableDeclaration(isMutable: false);

                        default:
                            RaiseError("Unknown operator lexeme found");
                            break;
                    }
                    break;

                case SymbolLexeme symbolLexeme:
                    var location = currentLexeme.StartLocation;
                    MoveNextLexeme();
                    return new ExpressionStatement(GetFunctionCall(symbolLexeme.Text, location));

                default:
                    RaiseError("Statement was expected");
                    break;
            }

            return null;
        }


        private IFunctionCallExpression GetFunctionCall(string name, Location location) {
            CheckOperator(OperatorMeaning.OpenParenthesis);

            // Pick all the arguments
            var arguments = new List<IExpression>();

            if (WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                MoveNextLexeme();
            } else {
                while (currentLexeme != null) {
                    arguments.Add(GetExpression());
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }
                CheckOperator(OperatorMeaning.CloseParenthesis);
            }

            Console.WriteLine($"parsing call of '{name}'...");

            // Filter built-ins
            switch (name) {
                case "print":
                    return new BuiltinFunctionCallExpression(
                        new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Void),
                        arguments,
                        BuiltinFunctionCallExpression.Option.Print);

                default:
                    // TODO: insert syntactic checks (for arguments)
                    var maybe = functionTree.GetFunctionDefinition(name);
                    if (maybe == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    } else {
                        var definition = maybe.Value;
                        return new CustomFunctionCallExpression(definition.ResultType, arguments, definition.Number);
                    }
            }
        }


        private IExpression GetExpression() {
            switch (currentLexeme) {
                case SymbolLexeme symbol:
                    var maybe = scopeStack.GetDefinition(symbol.Text);
                    if (!maybe.HasValue) {
                        RaiseError($"Undeclared identifier '{symbol.Text}'");
                    }

                    MoveNextLexeme();
                    var definition = maybe.Value;
                    var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                    return new VariableExpression(symbol.Text, definition.Number,
                                                  frameOffset, false, definition.TypeInfo);

                case LiteralLexeme literal:
                    var typeOption = PrimitiveTypeInfo.Option.Void;
                    switch (literal.Value) {
                        case bool value:
                            typeOption = PrimitiveTypeInfo.Option.Bool;
                            break;

                        case int value:
                            typeOption = PrimitiveTypeInfo.Option.Int;
                            break;

                        case string value:
                            typeOption = PrimitiveTypeInfo.Option.String;
                            break;

                        default:
                            RaiseError("Unknown literal");
                            break;
                    }
                    MoveNextLexeme();
                    return new LiteralExpression(literal.Value, new PrimitiveTypeInfo(typeOption));

                default:
                    RaiseError("Expression was expected");
                    break;
            }

            // Sanity check
            return null;
        }



        // [let | var] name = expression
        private VariableDeclarationStatement GetVariableDeclaration(bool isMutable) {
            var name = GetSymbolText("Variable name");
            CheckOperator(OperatorMeaning.Assignment);
            var expression = GetExpression();

            // Check scope
            if (!scopeStack.DeclareVariable(name, expression.TypeInfo, isMutable)) {
                RaiseError($"Variable '{name}' has already been declared");
            }

            return new VariableDeclarationStatement(name, expression, isMutable);
        }



        // if condition {
        //     if-statements
        // } else {
        //     else-statements
        // }
        private ConditionalStatement GetConditional() {
            var condition = GetExpression();

            // if-clause
            CheckOperator(OperatorMeaning.OpenBrace);
            scopeStack.EnterScope(isStrong: false);
            var ifStatements = GetStatementList(false);
            scopeStack.LeaveScope();
            CheckOperator(OperatorMeaning.CloseBrace);

            // else-clause
            List<IStatement> elseStatements = null;
            if (WhetherOperator(OperatorMeaning.Else)) {
                MoveNextLexeme();
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                elseStatements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);
            }

            return new ConditionalStatement(condition, ifStatements, elseStatements);
        }



        // func name() {
        //     statements
        // }
        /*private FunctionDeclarationStatement GetFunctionDeclaration() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            scopeStack.EnterScope(isStrong: true);
            var body = GetStatementList(true);
            scopeStack.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
            return new FunctionDeclarationStatement(name, body);
        }*/


        private string GetSymbolText(string expected) {
            var text = "";
            if (currentLexeme is SymbolLexeme lexeme) {
                text = lexeme.Text;
            } else {
                RaiseError($"{expected} was expected");
            }
            MoveNextLexeme();
            return text;
        }


        private void CheckOperator(OperatorMeaning meaning) {
            if (!WhetherOperator(meaning)) { 
                RaiseError($"{GetOperatorName(meaning)} was expected");
            }
            MoveNextLexeme();
        }


        private bool WhetherOperator(OperatorMeaning meaning) {
            if (currentLexeme is OperatorLexeme lexeme && lexeme.Meaning == meaning) {
                return true;
            } else {
                return false;
            }
        }


        private string GetOperatorName(OperatorMeaning meaning) {
            switch (meaning) {
                case OperatorMeaning.Unknown:
                    return "Unknown";

                case OperatorMeaning.OpenParenthesis:
                    return "Opening parenthesis";

                case OperatorMeaning.CloseParenthesis:
                    return "Closing parenthesis";

                case OperatorMeaning.OpenBracket:
                    return "Opening bracket";

                case OperatorMeaning.CloseBracket:
                    return "Closing bracket";

                case OperatorMeaning.OpenBrace:
                    return "Opening brace";

                case OperatorMeaning.CloseBrace:
                    return "Closing brace";

                case OperatorMeaning.NewLine:
                    return "New line";
                case OperatorMeaning.Assignment:
                    return "Assignment operator";

                case OperatorMeaning.Equal:
                    return "Equality comparison operator";

                case OperatorMeaning.Var:
                    return "Variable declaration operator";

                case OperatorMeaning.Let:
                    return "Constant declaration operator";

                case OperatorMeaning.If:
                    return "Conditional operator";

                case OperatorMeaning.Func:
                    return "Function declaration operator";

                default:
                    throw new ArgumentException($"unknown option: {meaning}", nameof(meaning));
            }
        }


        private void RaiseError(string message, Location? location = null) {
            var loc = location ?? currentLexeme.StartLocation;
            throw new ParserException(message, loc.Line, loc.LineNumber, loc.ColumnNumber);
        }


        private void ResetLexemes() {
            lexemeEnumerator = lexemes.GetEnumerator();
        }


        private bool MoveNextLexeme() {
            if (lexemeEnumerator.MoveNext()) {
                currentLexeme = lexemeEnumerator.Current;
                return true;
            } else {
                currentLexeme = null;
                return false;
            }
        }
    }
}

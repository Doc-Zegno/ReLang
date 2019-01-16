using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;


namespace Handmada.ReLang.Compilation.Parsing {
    struct FunctionSignature {
        ITypeInfo ResultType { get; }
        List<ITypeInfo> ArgumentTypes { get; }
    }


    class Parser {
        private Lexer lexer;
        private ILexeme currentLexeme;
        private ScopeStack scopeStack;


        public Parser(IEnumerable<string> lines) {
            lexer = new Lexer(lines);
            scopeStack = new ScopeStack();
        }


        public List<IStatement> Parse() {
            var parsed = GetStatementList();
            if (currentLexeme != null) {
                RaiseError("Parsing is done but end of file wasn't reached");
            }
            return parsed;
        }


        public List<IStatement> GetStatementList() {
            var statements = new List<IStatement>();
            while (MoveNextLexeme()) {
                if (WhetherOperator(OperatorMeaning.NewLine)) {
                    continue;
                }
                if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                    break;
                }
                statements.Add(GetStatement());
            }
            return statements;
        }


        private IStatement GetStatement() {
            switch (currentLexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.Func:
                            MoveNextLexeme();
                            return GetFunctionDeclaration();

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
                    MoveNextLexeme();
                    return new ExpressionStatement(GetFunctionCall(symbolLexeme.Text));

                default:
                    RaiseError("Statement was expected");
                    break;
            }

            return null;
        }


        private IFunctionCallExpression GetFunctionCall(string name) {
            CheckOperator(OperatorMeaning.OpenParenthesis);

            // Pick all the arguments
            var arguments = new List<IExpression>();
            while (true) {
                arguments.Add(GetExpression());
                if (WhetherOperator(OperatorMeaning.Comma)) {
                    MoveNextLexeme();
                } else {
                    break;
                }
            }
            
            CheckOperator(OperatorMeaning.CloseParenthesis);

            // Filter built-ins
            switch (name) {
                case "print":
                    return new BuiltinFunctionCallExpression(
                        new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Void),
                        arguments,
                        BuiltinFunctionCallExpression.Option.Print);

                default:
                    RaiseError("Custom function call is not implemented");
                    return null;
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
                    MoveNextLexeme();
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
            var ifStatements = GetStatementList();
            scopeStack.LeaveScope();
            CheckOperator(OperatorMeaning.CloseBrace);

            // else-clause
            List<IStatement> elseStatements = null;
            if (WhetherOperator(OperatorMeaning.Else)) {
                MoveNextLexeme();
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                elseStatements = GetStatementList();
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);
            }

            return new ConditionalStatement(condition, ifStatements, elseStatements);
        }



        // func name() {
        //     statements
        // }
        private FunctionDeclarationStatement GetFunctionDeclaration() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            scopeStack.EnterScope(isStrong: true);
            var body = GetStatementList();
            scopeStack.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
            return new FunctionDeclarationStatement(name, body);
        }


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


        private void RaiseError(string message) {
            throw new ParserException(message, lexer.CurrentLine, lexer.CurrentLineNumber, lexer.CurrentCharacterNumber);
        }


        private bool MoveNextLexeme() {
            currentLexeme = lexer.GetNextLexeme();
            return currentLexeme != null;
        }
    }
}

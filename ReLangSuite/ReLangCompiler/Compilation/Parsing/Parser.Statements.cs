using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public partial class Parser {
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
            var location = currentLexeme.StartLocation;
            var lexeme = currentLexeme;
            MoveNextLexeme();

            switch (lexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.If:
                            return GetConditional();

                        case OperatorMeaning.Var:
                            return GetVariableDeclaration(true, location);

                        case OperatorMeaning.Let:
                            return GetVariableDeclaration(false, location);

                        case OperatorMeaning.For:
                            return GetForLoop();

                        case OperatorMeaning.Return:
                            return GetReturn();

                        default:
                            RaiseError("Unknown operator lexeme found", location);
                            break;
                    }
                    break;

                case SymbolLexeme symbolLexeme:
                    if (currentLexeme is OperatorLexeme op) {
                        switch (op.Meaning) {
                            case OperatorMeaning.OpenParenthesis:
                                return new ExpressionStatement(GetFunctionCall(symbolLexeme.Text, location));

                            case OperatorMeaning.Assignment:
                                return GetAssignment(symbolLexeme.Text, location);

                            default:
                                RaiseError("Unexpected operator");
                                return null;
                        }
                    } else {
                        RaiseError("Unexpected lexeme");
                        return null;
                    }

                default:
                    RaiseError("Statement was expected", location);
                    break;
            }

            return null;
        }



        // x = y + z
        private IStatement GetAssignment(string name, Location location) {
            CheckOperator(OperatorMeaning.Assignment);

            // Check variable definition
            var maybe = scopeStack.GetDefinition(name);
            if (!maybe.HasValue) {
                RaiseError($"Undeclared identifier '{name}'", location);
            }

            var definition = maybe.Value;
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);

            // Check if it's mutable
            if (!definition.IsMutable) {
                RaiseError($"Object '{name}' was declared as immutable, assignment is impossible", location);
            }

            var loc = currentLexeme.StartLocation;
            var value = GetExpression();
            var converted = ForceConvertExpression(value, definition.TypeInfo, loc);

            return new AssignmentStatement(name, definition.Number, frameOffset, converted);
        }



        // return expr
        private IStatement GetReturn() {
            var location = currentLexeme.StartLocation;
            var operand = GetExpression();
            var definition = functionTree.GetCurrentFunctionDefinition();

            var converted = ForceConvertExpression(operand, definition.ResultType, location);
            return new ReturnStatement(converted);
        }



        // for item in iterable { ... }
        private IStatement GetForLoop() {
            var name = GetSymbolText("Iterable's item name");
            CheckOperator(OperatorMeaning.In);
            var location = currentLexeme.StartLocation;
            var iterable = GetExpression();

            if (iterable.TypeInfo is IIterableTypeInfo iterableType) {
                var itemType = iterableType.ItemType;

                // Enter scope and add item variable
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                scopeStack.DeclareVariable(name, itemType, true, null);
                var statements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);

                return new ForEachStatement(name, iterable, statements);
            } else {
                RaiseError($"Source of items must be iterable (got '{iterable.TypeInfo.Name}')", location);
                return null;
            }
        }



        // [let | var] name = expression
        private IStatement GetVariableDeclaration(bool isMutable, Location locationVar) {
            var locationName = currentLexeme.StartLocation;
            var name = GetSymbolText("Variable name");
            CheckOperator(OperatorMeaning.Assignment);
            var value = GetMultipleExpression();

            // Check scope
            if (!scopeStack.DeclareVariable(name, value.TypeInfo, isMutable, value)) {
                RaiseError($"Variable '{name}' has already been declared", locationName);
            }

            // Check if it's tuple
            if (value.TypeInfo is TupleTypeInfo && isMutable) {
                RaiseError("Tuple objects must be declared as immutable", locationVar);
            }

            return new VariableDeclarationStatement(name, value, isMutable);
        }



        // if condition {
        //     if-statements
        // } else {
        //     else-statements
        // }
        private IStatement GetConditional() {
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
    }
}

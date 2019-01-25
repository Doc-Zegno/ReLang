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

                            /*case OperatorMeaning.Assignment:
                                PutBack();
                                return GetAssignment(symbolLexeme.Text, location);*/

                            default:
                                // Assignment
                                PutBack();
                                return GetAssignment();
                                //RaiseError($"Unexpected operator: {GetOperatorName(op.Meaning)}");
                                //return null;
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
        private IStatement GetAssignment() {
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.Assignment);

            var right = currentLexeme.StartLocation;
            var value = GetMultipleExpression();

            return ForceAssignVariableList(identifiers, value, right);
        }



        private IStatement ForceAssignVariableList(IIdentifier identifiers, IExpression value, Location right) {
            switch (identifiers) {
                case SingleIdentifier single:
                    return ForceAssignVariable(single, value, right);


                case IdentifierList identifierList:
                    if (value.TypeInfo is TupleTypeInfo tupleType) {
                        // Need unpacking
                        CheckNumberOfUnpackedValues(identifierList, tupleType);

                        // Three options:
                        //  1) it's compile-time tuple literal
                        //        x, y = 3, 4
                        //           <=>
                        //        x = 3
                        //        y = 4
                        //
                        //  2) it's a variable
                        //        x, y = point
                        //           <=>
                        //        x = point[0]
                        //        y = point[1]
                        //
                        //  3) it's an arbitrary expression (including a non compile-time literal)
                        //        x, y = f()
                        //           <=>
                        //        let _tmp = f()
                        //        x = _tmp[0]
                        //        y = _tmp[1]
                        var subStatements = new List<IStatement>();

                        switch (value) {
                            case TupleLiteralExpression tupleLiteral when tupleLiteral.IsCompileTime:
                                for (var i = 0; i < identifierList.Identifiers.Count; i++) {
                                    var identifier = identifierList.Identifiers[i];
                                    var expression = tupleLiteral.Items[i];
                                    subStatements.Add(ForceAssignVariableList(identifier, expression, right));
                                }
                                break;

                            case VariableExpression variable:
                                GenerateTupleDestruction(subStatements, variable, tupleType, identifierList, true, right,
                                                         (ids, expr, dummy, loc) => ForceAssignVariableList(ids, expr, loc));
                                break;

                            default:
                                GenerateTupleDestructionWithTmp(subStatements, value, tupleType, identifierList, true, right,
                                                                (ids, expr, dummy, loc) => ForceAssignVariableList(ids, expr, loc));
                                break;
                        }

                        return new CompoundStatement(subStatements);

                    } else {
                        RaiseError("Right-side expression is not a tuple. Nothing to unpack", right);
                        return null;
                    }


                default:
                    throw new NotImplementedException();
            }
        }



        private void CheckNumberOfUnpackedValues(IdentifierList identifierList, TupleTypeInfo tupleType) {
            var expectedCount = identifierList.Identifiers.Count;
            var actualCount = tupleType.ItemTypes.Count;
            if (expectedCount != actualCount) {
                var hint = $"left: {expectedCount}, right: {actualCount}";
                RaiseError($"Mismatching number of values to be unpacked ({hint})", identifierList.StartLocation);
            }
        }



        private IStatement ForceAssignVariable(SingleIdentifier identifier, IExpression value, Location right) {
            // Check variable definition
            var name = identifier.Name;
            var maybe = scopeStack.GetDefinition(name);
            if (!maybe.HasValue) {
                RaiseError($"Undeclared identifier '{name}'", identifier.StartLocation);
            }

            var definition = maybe.Value;
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);

            // Check if it's mutable
            if (!definition.IsMutable) {
                RaiseError($"Object '{name}' was declared as immutable, assignment is impossible", identifier.StartLocation);
            }
            
            var converted = ForceConvertExpression(value, definition.TypeInfo, right);
            return new AssignmentStatement(name, definition.Number, frameOffset, converted);
        }



        // return expr
        private IStatement GetReturn() {
            var location = currentLexeme.StartLocation;
            var operand = GetMultipleExpression();
            var definition = functionTree.GetCurrentFunctionDefinition();

            var converted = ForceConvertExpression(operand, definition.ResultType, location);
            return new ReturnStatement(converted);
        }



        // for item in iterable { ... }
        private IStatement GetForLoop() {
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.In);
            var location = currentLexeme.StartLocation;
            var iterable = GetExpression();
            var itemType = TryGetItemType(iterable.TypeInfo);

            if (itemType != null) {
                // Enter scope and add item variable
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);

                // Either declare an item variable or create a temporary and deconstruct it
                var statements = new List<IStatement>();
                var name = "";
                switch (identifiers) {
                    case SingleIdentifier single:
                        scopeStack.DeclareVariable(single.Name, itemType, true, null);
                        name = single.Name;
                        break;

                    case IdentifierList identifierList:
                        if (itemType is TupleTypeInfo tupleType) {
                            // Need to unpack tuple
                            CheckNumberOfUnpackedValues(identifierList, tupleType);

                            // Declare a temporary to store iterable's item
                            var tupleVariable = DeclareTemporaryVariable(tupleType, null);
                            name = tupleVariable.Name;

                            // Unpack tuple
                            GenerateTupleDestruction(statements, tupleVariable, tupleType, identifierList,
                                                     true, location, ForceDeclareVariableList);

                        } else {
                            RaiseError($"Type of iterable's item is not tuple (got '{itemType.Name}')", location);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
                
                statements.AddRange(GetStatementList(false));
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
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.Assignment);

            var locationValue = currentLexeme.StartLocation;
            var value = GetMultipleExpression();

            return ForceDeclareVariableList(identifiers, value, isMutable, locationValue);
        }



        private IStatement ForceDeclareVariableList(IIdentifier identifiers, IExpression value,
                                                    bool isMutable, Location right)
        {
            switch (identifiers) {
                case SingleIdentifier singleIdentifier:
                    return ForceDeclareVariable(singleIdentifier.Name, value, isMutable, singleIdentifier.StartLocation);

                case IdentifierList identifierList:
                    if (value.TypeInfo is TupleTypeInfo tupleType) {
                        // Need unpacking
                        CheckNumberOfUnpackedValues(identifierList, tupleType);

                        // This bloody tuple can be one of these options:
                        //  1) a tuple literal => can be destructured at compile-time (HOLY SHIT!):
                        //        let point = (3.0, 4.0)
                        //        var x, y = point
                        //           <=>
                        //        var x = 3.0
                        //        var y = 4.0
                        //
                        //  2) a random variable => should use indexing
                        //     (it's safe since evaluation of variable produces no side effect):
                        //        let point = getRandomPoint()
                        //        var x, y = point
                        //           <=>
                        //        var x = point[0]
                        //        var y = point[1]
                        //
                        //  3) a random expression => should pre-evaluate this shit into temporary variable
                        //     since it might (and fucking actually will) have a side effect:
                        //        var x, y = getRandomPointAndPrintHelloWorldToTextFile(fileName)
                        //           <=>
                        //        let _tmp = get...
                        //        var x = _tmp[0]
                        //        var y = _tmp[1]
                        //
                        // (Yeah, I'm talking to myself but that's okay)
                        var subStatements = new List<IStatement>();

                        switch (value) {
                            case TupleLiteralExpression tuple:
                                for (var i = 0; i < identifierList.Identifiers.Count; i++) {
                                    var expression = tuple.Items[i];
                                    var identifier = identifierList.Identifiers[i];
                                    subStatements.Add(ForceDeclareVariableList(identifier, expression, isMutable, right));
                                }
                                break;

                            case VariableExpression variable:
                                GenerateTupleDestruction(subStatements, variable, tupleType, identifierList,
                                                         isMutable, right, ForceDeclareVariableList);
                                break;

                            default:
                                GenerateTupleDestructionWithTmp(subStatements, value, tupleType, identifierList,
                                                                isMutable, right, ForceDeclareVariableList);
                                break;
                        }

                        return new CompoundStatement(subStatements);

                    } else {
                        RaiseError("Expression is not a tuple. Nothing to unpack", right);
                        return null;
                    }

                default:
                    throw new NotImplementedException();
            }
        }



        private void GenerateTupleDestructionWithTmp(
            List<IStatement> subStatements,
            IExpression value,
            TupleTypeInfo tupleType,
            IdentifierList identifierList,
            bool isMutable,
            Location right,
            Func<IIdentifier, IExpression, bool, Location, IStatement> func)
        {
            // Declare a tmp 
            var tupleVariable = DeclareTemporaryVariable(tupleType, value);
            subStatements.Add(new VariableDeclarationStatement(tupleVariable.Name, value, false));

            // Now it's safe to use code for VariableExpression case
            GenerateTupleDestruction(subStatements, tupleVariable, tupleType, identifierList,
                                     isMutable, right, func);
        }



        private VariableExpression DeclareTemporaryVariable(ITypeInfo typeInfo, IExpression value) {
            // Declare a tmp 
            var tmpName = GetNextTmpName();
            if (!scopeStack.DeclareVariable(tmpName, typeInfo, false, value)) {
                RaiseError($"I wasn't able to declare a temporary '{tmpName}'. WTF???");
            }

            // Get a variable expression
            var maybe = scopeStack.GetDefinition(tmpName);
            var definition = maybe.Value;
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
            return new VariableExpression(
                tmpName,
                definition.Number,
                frameOffset,
                false,
                typeInfo
            );
        }



        private void GenerateTupleDestruction(
            List<IStatement> subStatements, 
            VariableExpression tupleVariable,
            TupleTypeInfo tupleType,
            IdentifierList identifierList,
            bool isMutable,
            Location right,
            Func<IIdentifier, IExpression, bool, Location, IStatement> func)
        {
            var actualCount = tupleType.ItemTypes.Count;
            for (var i = 0; i < actualCount; i++) {
                // Generate a call of built-in function `tupleGet(tuple, index)`
                var arguments = new List<IExpression> {
                                        tupleVariable,
                                        new PrimitiveLiteralExpression(i, PrimitiveTypeInfo.Int),
                                    };

                var expression = new BuiltinFunctionCallExpression(
                    tupleType.ItemTypes[i],
                    arguments,
                    BuiltinFunctionCallExpression.Option.TupleGet
                );

                var identifier = identifierList.Identifiers[i];
                subStatements.Add(func(identifier, expression, isMutable, right));
            }
        }



        private IStatement ForceDeclareVariable(string name, IExpression value, bool isMutable, Location locationName) {
            if (value.TypeInfo is TupleTypeInfo && isMutable) {
                RaiseError($"Tuple object '{name}' must be declared as immutable", locationName);
            }

            if (!scopeStack.DeclareVariable(name, value.TypeInfo, isMutable, value)) {
                RaiseError($"Variable '{name}' has already been declared", locationName);
            }

            return new VariableDeclarationStatement(name, value, isMutable);
        }



        // [let] x, y, z
        private IIdentifier GetIdentifierList() {
            var identifiers = new List<IIdentifier>();

            while (true) {
                identifiers.Add(GetSingleIdentifier());
                if (WhetherOperator(OperatorMeaning.Comma)) {
                    MoveNextLexeme();
                } else {
                    break;
                }
            }
            
            if (identifiers.Count > 1) {
                return new IdentifierList(identifiers);
            } else {
                return identifiers[0];
            }
        }



        // [let] x
        private IIdentifier GetSingleIdentifier() {
            if (WhetherOperator(OperatorMeaning.OpenParenthesis)) {
                MoveNextLexeme();
                var list = GetIdentifierList();
                CheckOperator(OperatorMeaning.CloseParenthesis);
                return list;
            } else {
                var location = currentLexeme.StartLocation;
                var name = GetSymbolText("Identifier");
                return new SingleIdentifier(name, location);
            }
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

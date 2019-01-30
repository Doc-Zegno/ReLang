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

                if (WhetherOperator(OperatorMeaning.Commentary)) {
                    do {
                        MoveNextLexeme();
                    } while (!WhetherOperator(OperatorMeaning.NewLine) && currentLexeme != null);
                } else if (WhetherOperator(OperatorMeaning.NewLine)) {
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
            //MoveNextLexeme();

            switch (lexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.If:
                            MoveNextLexeme();
                            return GetConditional();

                        case OperatorMeaning.Var:
                            MoveNextLexeme();
                            return GetVariableDeclaration(true, location);

                        case OperatorMeaning.Let:
                            MoveNextLexeme();
                            return GetVariableDeclaration(false, location);

                        case OperatorMeaning.For:
                            MoveNextLexeme();
                            return GetForLoop();

                        case OperatorMeaning.While:
                            MoveNextLexeme();
                            return GetWhileLoop();

                        case OperatorMeaning.Do:
                            MoveNextLexeme();
                            return GetDoWhileLoop();

                        case OperatorMeaning.Return:
                            MoveNextLexeme();
                            return GetReturn();

                        case OperatorMeaning.OpenParenthesis:
                            return GetFunctionCallOrAssignment();

                        default:
                            RaiseError("Unknown operator lexeme found", location);
                            break;
                    }
                    break;

                default:
                    return GetFunctionCallOrAssignment();
            }

            return null;
        }



        // [while] condition { statements }
        private IStatement GetWhileLoop() {
            scopeStack.EnterScope(false);
            var condition = GetExpression();
            CheckCondition(condition, true, false);

            CheckOperator(OperatorMeaning.OpenBrace);
            var statements = GetStatementList(false);
            CheckOperator(OperatorMeaning.CloseBrace);
            scopeStack.LeaveScope();

            return new WhileStatement(condition, statements);
        }



        // [do] { statements } while condition
        private IStatement GetDoWhileLoop() {
            scopeStack.EnterScope(false);
            CheckOperator(OperatorMeaning.OpenBrace);
            var statements = GetStatementList(false);
            CheckOperator(OperatorMeaning.CloseBrace);
            CheckOperator(OperatorMeaning.While);

            var condition = GetExpression();
            CheckCondition(condition, true, false);
            scopeStack.LeaveScope();

            return new DoWhileStatement(condition, statements);
        }



        private IStatement GetFunctionCallOrAssignment() {
            var location = currentLexeme.StartLocation;
            var expression = GetMultipleExpression();

            if (!expression.IsLvalue) {
                // Function call only
                if (expression is FunctionCallExpression) {
                    return new ExpressionStatement(expression);
                } else {
                    RaiseError($"Function call was expected", location);
                    return null;
                }

            } else {
                // Assignment only
                CheckOperator(OperatorMeaning.Assignment);
                var identifiers = ConvertExpressionToIdentifierList(expression);

                var right = currentLexeme.StartLocation;
                var value = GetMultipleExpression();

                return ForceAssignVariableList(identifiers, value, right);
            }
        }



        private IIdentifier ConvertExpressionToIdentifierList(IExpression expression) {
            switch (expression) {
                case TupleLiteralExpression tupleLiteral:
                    var identifiers = new List<IIdentifier>();
                    foreach (var item in tupleLiteral.Items) {
                        identifiers.Add(ConvertExpressionToIdentifierList(item));
                    }
                    return new IdentifierList(identifiers);

                case VariableExpression variable:
                    return new SingleIdentifier(variable.Name, null, variable.MainLocation);

                case FunctionCallExpression functionCall:
                    var self = functionCall.Arguments[0];
                    var methodName = functionCall.FunctionDefinition.ShortName;
                    var definition = self.TypeInfo.GetMethodDefinition("s" + methodName.Substring(1));

                    Console.WriteLine($"Type of 'self' is '{self.TypeInfo.Name}'");
                    Console.WriteLine($"Method name is '{methodName}'");

                    if (definition == null) {
                        RaiseError("No setter is available for this expression", functionCall.MainLocation);
                    }

                    return new SetterIdentifier(definition, functionCall.Arguments, functionCall.MainLocation);

                default:
                    RaiseError("This expression cannot be at the left side of assignment", expression.MainLocation);
                    return null;
            }
        }



        // x = y + z
        /*private IStatement GetAssignment() {
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.Assignment);

            var right = currentLexeme.StartLocation;
            var value = GetMultipleExpression();

            return ForceAssignVariableList(identifiers, value, right);
        }*/



        private IStatement ForceAssignVariableList(IIdentifier identifiers, IExpression value, Location right) {
            switch (identifiers) {
                case SingleIdentifier single:
                    return ForceAssignVariable(single, value, right);


                case SetterIdentifier setter:
                    var definition = setter.FunctionDefinition;
                    setter.Arguments.Add(value);
                    CheckAndConvertFunctionArguments(definition.Signature, setter.Arguments, setter.StartLocation);

                    //var lastType = definition.ArgumentTypes.Last();
                    //var converted = ForceConvertExpression(value, lastType, value.MainLocation);
                    //setter.Arguments.Add(converted);
                    return new ExpressionStatement(
                        new FunctionCallExpression(definition, setter.Arguments, false, setter.StartLocation)
                    );


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
            var definition = scopeStack.GetDefinition(name);
            if (definition == null) {
                RaiseError($"Undeclared identifier '{name}'", identifier.StartLocation);
            }
            
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);

            // Check if it's mutable
            if (definition.IsFinal) {
                RaiseError($"Object '{name}' was declared as final, assignment is impossible", identifier.StartLocation);
            }
            
            var converted = ForceConvertExpression(value, definition.TypeInfo, right);
            CheckMutability(converted, true);

            return new AssignmentStatement(name, definition.Number, frameOffset, converted);
        }



        // return expr
        private IStatement GetReturn() {
            var location = currentLexeme.StartLocation;
            var operand = GetMultipleExpression();
            var definition = functionTree.GetCurrentFunctionDefinition();

            var converted = ForceConvertExpression(operand, definition.Signature.ResultType, location);
            CheckMutability(converted, definition.Signature.ResultMutability);

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
                        scopeStack.DeclareVariable(single.Name, itemType, true, true, null);
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
                    return ForceDeclareVariable(singleIdentifier, value, isMutable);

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
            subStatements.Add(new VariableDeclarationStatement(tupleVariable.Name, value.TypeInfo, value, false));

            // Now it's safe to use code for VariableExpression case
            GenerateTupleDestruction(subStatements, tupleVariable, tupleType, identifierList,
                                     isMutable, right, func);
        }



        private VariableExpression DeclareTemporaryVariable(ITypeInfo typeInfo, IExpression value) {
            // Declare a tmp 
            var tmpName = GetNextTmpName();
            if (!scopeStack.DeclareVariable(tmpName, typeInfo, true, false, value)) {
                RaiseError($"I wasn't able to declare a temporary '{tmpName}'. WTF???");
            }

            // Get a variable expression
            var definition = scopeStack.GetDefinition(tmpName);
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
            return new VariableExpression(
                tmpName,
                definition.Number,
                frameOffset,
                false,
                typeInfo,
                null
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
                                        new PrimitiveLiteralExpression(i, PrimitiveTypeInfo.Int, null),
                                    };

                var expression = new FunctionCallExpression(tupleType.GetTupleAccessorDefinition(i), arguments, false, null);

                /*var expression = new BuiltinFunctionCallExpression(
                    tupleType.ItemTypes[i],
                    arguments,
                    BuiltinFunctionCallExpression.Option.TupleGet
                );*/

                var identifier = identifierList.Identifiers[i];
                subStatements.Add(func(identifier, expression, isMutable, right));
            }
        }



        private IStatement ForceDeclareVariable(SingleIdentifier identifier, IExpression value, bool isMutable) {
            var converted = value;
            var typeInfo = value.TypeInfo;
            if (identifier.ExpectedType != null) {
                converted = ForceConvertExpression(value, identifier.ExpectedType, value.MainLocation);
                typeInfo = identifier.ExpectedType;
            }

            if (WhetherPrimitiveType(typeInfo, PrimitiveTypeInfo.Option.Void)) {
                RaiseError("Cannot declare variable of type 'Void'", converted.MainLocation);
            }

            if (typeInfo is NullTypeInfo) {
                RaiseError("Cannot deduce type from given expression", converted.MainLocation);
            }

            if (typeInfo is TupleTypeInfo && isMutable) {
                RaiseError($"Tuple object '{identifier.Name}' must be declared as immutable", identifier.StartLocation);
            }

            CheckMutability(converted, isMutable);

            if (!scopeStack.DeclareVariable(identifier.Name, typeInfo, !isMutable, isMutable, converted)) {
                RaiseError($"Variable '{identifier.Name}' has already been declared", identifier.StartLocation);
            }

            Console.WriteLine($"Declared object '{identifier.Name}' of type '{typeInfo.Name}'");

            return new VariableDeclarationStatement(identifier.Name, typeInfo, converted, isMutable);
        }



        private void CheckMutability(IExpression expression, bool mustBeMutable) {
            if (mustBeMutable && !WhetherExpressionMutable(expression)) {
                RaiseError("Expression's result is not mutable", expression.MainLocation);
            }
        }



        private bool WhetherExpressionMutable(IExpression expression) {
            if (expression.TypeInfo is TupleTypeInfo) {
                return false;

            } else if (expression is VariableExpression variable) {
                var definition = scopeStack.GetDefinition(variable.Name);
                if (definition.TypeInfo.IsReferential && !definition.IsMutable) {
                    return false;
                }

            } else if (expression is FunctionCallExpression functionCall) {
                var definition = functionCall.FunctionDefinition;
                var signature = definition.Signature;
                if (signature.ResultType.IsReferential && !signature.ResultMutability) {
                    return false;
                }
            }

            return true;
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

                ITypeInfo expectedType = null;
                if (WhetherOperator(OperatorMeaning.Colon)) {
                    MoveNextLexeme();
                    expectedType = GetTypeInfo();
                }

                return new SingleIdentifier(name, expectedType, location);
            }
        }



        // if let number = maybe { ... }
        private IStatement GetConditional() {
            bool isMutable;

            if (WhetherOperator(OperatorMeaning.Var)) {
                isMutable = true;
            } else if (WhetherOperator(OperatorMeaning.Let)) {
                isMutable = false;
            } else {
                return GetRawConditional();
            }

            // Parse variable binding
            MoveNextLexeme();
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.Assignment);
            var value = GetExpression();  // Multiple expression is a tuple literal and thus can't be a maybe

            // Check if it's a maybe and unwrap construction
            var statements = new List<IStatement>();
            if (value.TypeInfo is MaybeTypeInfo maybeType) {
                if (value.IsCompileTime) {
                    if (value.Value == null) {
                        RaiseError("Right-hand expression is always null", value.MainLocation, true);
                    } else {
                        RaiseError("Right-hand expression is always not null", value.MainLocation, true);
                    }
                }

                VariableExpression variable = null;
                if (value is VariableExpression variableExpression) {
                    variable = variableExpression;
                } else {
                    variable = DeclareTemporaryVariable(value.TypeInfo, value);
                    statements.Add(new VariableDeclarationStatement(variable.Name, value.TypeInfo, value, false));
                    value = variable;
                }

                var condition = new UnaryOperatorExpression(UnaryOperatorExpression.Option.TestNotNull, variable,
                                                            PrimitiveTypeInfo.Bool, variable.MainLocation);

                scopeStack.EnterScope(false);
                CheckOperator(OperatorMeaning.OpenBrace);

                // Adjust variable expression on scope enter
                variable = new VariableExpression(variable.Name, variable.Number, variable.FrameOffset - 1,
                                                  variable.IsCompileTime, variable.TypeInfo, variable.MainLocation);

                // Declare binded variables on scope enter
                List<IStatement> ifStatements = null;
                var unwrapped = new UnaryOperatorExpression(UnaryOperatorExpression.Option.FromMaybe, variable,
                                                            maybeType.InternalType, variable.MainLocation);
                var declaration = ForceDeclareVariableList(identifiers, unwrapped, isMutable, unwrapped.MainLocation);
                if (declaration is CompoundStatement compound) {
                    ifStatements = compound.Statements;
                } else {
                    ifStatements = new List<IStatement> { declaration };
                }

                ifStatements.AddRange(GetStatementList(false));
                CheckOperator(OperatorMeaning.CloseBrace);
                scopeStack.LeaveScope();

                var elseStatements = GetElseStatements();
                statements.Add(new ConditionalStatement(condition, ifStatements, elseStatements));
                
                if (statements.Count > 1) {
                    return new CompoundStatement(statements);
                } else {
                    return statements[0];
                }

            } else {
                RaiseError("Right-hand expression must have a maybe type", value.MainLocation);
                return null;
            }
        }



        // if condition {
        //     if-statements
        // } elif condition {
        //     elif-statements
        // } else {
        //     else-statements
        // }
        private IStatement GetRawConditional() {
            var condition = GetExpression();
            CheckCondition(condition, false, false);

            // if-clause
            CheckOperator(OperatorMeaning.OpenBrace);
            scopeStack.EnterScope(isStrong: false);
            var ifStatements = GetStatementList(false);
            scopeStack.LeaveScope();
            CheckOperator(OperatorMeaning.CloseBrace);

            // else-clause
            var elseStatements = GetElseStatements();

            return new ConditionalStatement(condition, ifStatements, elseStatements);
        }



        private List<IStatement> GetElseStatements() {
            List<IStatement> elseStatements = null;
            if (WhetherOperator(OperatorMeaning.Elif)) {
                MoveNextLexeme();
                scopeStack.EnterScope(false);

                var nested = GetConditional();
                if (nested is CompoundStatement compound) {
                    elseStatements = compound.Statements;
                } else {
                    elseStatements = new List<IStatement> { nested };
                }
                
                scopeStack.LeaveScope();

            } else if (WhetherOperator(OperatorMeaning.Else)) {
                MoveNextLexeme();
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                elseStatements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);
            }
            return elseStatements;
        }



        private void CheckCondition(IExpression condition, bool isTrueAllowed, bool isFalseAllowed) {
            if (!WhetherPrimitiveType(condition.TypeInfo, PrimitiveTypeInfo.Option.Bool)) {
                RaiseError("Condition must be a boolean expression", condition.MainLocation);
            }

            if (condition.IsCompileTime) {
                var boolean = (bool)condition.Value;
                if (boolean && !isTrueAllowed) {
                    RaiseError("Condition is always true", condition.MainLocation, true);
                }
                if (!boolean && !isFalseAllowed) {
                    RaiseError("Condition is always false", condition.MainLocation, true);
                }
            }
        }
    }
}

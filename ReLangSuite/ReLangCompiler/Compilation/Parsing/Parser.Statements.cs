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
                            return GetVariableDeclaration(VariableQualifier.Mutable, location);

                        case OperatorMeaning.Let:
                            MoveNextLexeme();
                            return GetVariableDeclaration(VariableQualifier.Final, location);

                        case OperatorMeaning.Use:
                            MoveNextLexeme();
                            return GetVariableDeclaration(VariableQualifier.Final | VariableQualifier.Mutable | VariableQualifier.Disposable,
                                                          location);

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

                        case OperatorMeaning.Break:
                            MoveNextLexeme();
                            return GetBreak(location);

                        case OperatorMeaning.Continue:
                            MoveNextLexeme();
                            return GetContinue(location);

                        case OperatorMeaning.Try:
                            MoveNextLexeme();
                            return GetTryCatch();

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



        // [try] {
        //     expr
        // } catch IOError {
        //     expr
        // }
        private IStatement GetTryCatch() {
            CheckOperator(OperatorMeaning.OpenBrace);
            scopeStack.EnterScope(false, false);
            var tryStatements = GetStatementList(false);
            scopeStack.LeaveScope();
            CheckOperator(OperatorMeaning.CloseBrace);

            var catchBlocks = new List<(ErrorTypeInfo.Option, string, List<IStatement>)>();
            var traced = new HashSet<ErrorTypeInfo.Option>();

            do {
                var location = currentLexeme.StartLocation;
                CheckOperator(OperatorMeaning.Catch);
                var errorOption = ErrorTypeInfo.Option.None;
                var instanceName = "_";

                if (traced.Contains(ErrorTypeInfo.Option.Error)) {
                    RaiseError("Useless catch-block: universal handler has already been defined", location, true);
                }

                if (WhetherOperator(OperatorMeaning.OpenBrace)) {
                    // Catch all
                    traced.Add(ErrorTypeInfo.Option.Error);
                    errorOption = ErrorTypeInfo.Option.Error;

                } else {
                    // Catch concrete one
                    location = currentLexeme.StartLocation;
                    var name = GetSymbolText("Either error's type or name of error's instance");
                    var errorTypeName = "";
                    if (WhetherOperator(OperatorMeaning.Colon)) {
                        // Type of error after name
                        MoveNextLexeme();
                        instanceName = name;
                        location = currentLexeme.StartLocation;
                        errorTypeName = GetSymbolText("Error's type");
                    } else {
                        // Only type
                        errorTypeName = name;
                    }

                    foreach (var objectOption in Enum.GetValues(typeof(ErrorTypeInfo.Option))) {
                        var option = (ErrorTypeInfo.Option)objectOption;
                        if (option.ToString() == errorTypeName) {
                            errorOption = option;
                            break;
                        }
                    }
                    if (errorOption == ErrorTypeInfo.Option.None) {
                        RaiseError($"'{errorTypeName}' is not a valid error's type", location);
                    }

                    if (traced.Contains(errorOption)) {
                        RaiseError("Handler for this error has already been declared", location, true);
                    }

                    traced.Add(errorOption);
                }

                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(false, false);
                if (instanceName != "_") {
                    scopeStack.DeclareVariable(instanceName, new ErrorTypeInfo(errorOption), MakeQualifier(true, true, false), null);
                }
                var catchStatements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);

                catchBlocks.Add((errorOption, instanceName, catchStatements));
            } while (WhetherOperator(OperatorMeaning.Catch));

            return new TryCatchStatement(tryStatements, catchBlocks);
        }



        // [break]
        private IStatement GetBreak(Location location) {
            if (!scopeStack.IsInsideLoop) {
                RaiseError("Break statement is not placed inside loop", location);
            }
            return new BreakStatement(false);
        }



        // [continue]
        private IStatement GetContinue(Location location) {
            if (!scopeStack.IsInsideLoop) {
                RaiseError("Continue statement is not placed inside loop", location);
            }
            return new BreakStatement(true);
        }



        // [while] condition { statements }
        private IStatement GetWhileLoop() {
            var condition = GetExpression();
            CheckCondition(condition, true, false);

            scopeStack.EnterScope(false, true);
            CheckOperator(OperatorMeaning.OpenBrace);
            var statements = GetStatementList(false);
            CheckOperator(OperatorMeaning.CloseBrace);
            scopeStack.LeaveScope();

            return new WhileStatement(condition, statements);
        }



        // [do] { statements } while condition
        private IStatement GetDoWhileLoop() {
            scopeStack.EnterScope(false, true);
            CheckOperator(OperatorMeaning.OpenBrace);
            var statements = GetStatementList(false);
            CheckOperator(OperatorMeaning.CloseBrace);
            scopeStack.LeaveScope();

            CheckOperator(OperatorMeaning.While);
            var condition = GetExpression();
            CheckCondition(condition, true, false);

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
                // Assignment, short-hand assignment or in/decrement
                var identifiers = ConvertExpressionToIdentifierList(expression);
                var middle = currentLexeme.StartLocation;
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.Assignment:
                            return GetAssignment(identifiers);

                        case OperatorMeaning.Increment:
                            return GetIncrementDecrement(identifiers, true);

                        case OperatorMeaning.Decrement:
                            return GetIncrementDecrement(identifiers, false);

                        case OperatorMeaning.Plus:
                        case OperatorMeaning.Minus:
                        case OperatorMeaning.Asterisk:
                        case OperatorMeaning.ForwardSlash:
                        case OperatorMeaning.BackSlash:
                        case OperatorMeaning.Modulo:
                            MoveNextLexeme();
                            CheckOperator(OperatorMeaning.Assignment);
                            var right = GetMultipleExpression();
                            return ForceShorthandAssignList(identifiers, operatorLexeme.Meaning, right, middle);

                        default:
                            RaiseError($"Unexpected operator: {GetOperatorName(operatorLexeme.Meaning)}");
                            return null;
                    }
                } else {
                    RaiseError("Either assignment or increment/decrement were expected");
                    return null;
                }
            }
        }



        private IStatement ForceShorthandAssignList(IIdentifier identifiers, OperatorMeaning meaning, IExpression right, Location middle) {
            switch (identifiers) {
                case IdentifierList identifierList:
                    RaiseError("Massive shorthand for assignment is not supported in current version", middle);
                    return null;

                case SingleIdentifier single:
                    // v += e
                    //  <=>
                    // v = v + e
                    var variable = CreateVariableExpression(single);
                    var value = CreateBinaryExpression(meaning, variable, right, middle);
                    return ForceAssignVariable(single, value);

                case SetterIdentifier setter:
                    return ForceShorthandAssignSetter(setter, meaning, right, middle);

                default:
                    throw new NotSupportedException($"Unknown identifier's type: {identifiers}");
            }
        }



        private IStatement ForceShorthandAssignSetter(SetterIdentifier setter, OperatorMeaning meaning, IExpression right, Location middle) {
            var setterDefinition = setter.SetterDefinition;
            var getterDefinition = setter.GetterDefinition;
            var arguments = setter.Arguments;
            var argumentMutabilities = setterDefinition.Signature.ArgumentMutabilities;

            var statements = new List<IStatement>();
            // Precalculate arguments
            for (var i = 0; i < arguments.Count; i++) {
                var argument = arguments[i];
                var mutability = argumentMutabilities[i];
                if (!(argument is VariableExpression || argument is PrimitiveLiteralExpression)) {
                    // Store into tmp
                    var tmpVariable = DeclareTemporaryVariable(argument.TypeInfo, argument, mutability);
                    var qualifier = VariableQualifier.Final;
                    if (mutability) {
                        qualifier |= VariableQualifier.Mutable;
                    }

                    statements.Add(new VariableDeclarationStatement(tmpVariable.Name, tmpVariable.TypeInfo, argument, qualifier));
                    setter.Arguments[i] = tmpVariable;
                }
            }

            // value = get(tmp, i) op expr
            // set(tmp, i, value)
            var getterArguments = new List<IExpression>(setter.Arguments);
            var getterResultType = getterDefinition.Signature.ResultType.ResolveGeneric();
            var getterExpression = new FunctionCallExpression(getterDefinition, getterArguments, getterResultType, true, setter.StartLocation);
            var value = CreateBinaryExpression(meaning, getterExpression, right, middle);
            setter.Arguments.Add(value);
            var setterResultType = CheckAndConvertFunctionArguments(setterDefinition.Signature, setter.Arguments, middle);
            statements.Add(
                new ExpressionStatement(
                    new FunctionCallExpression(setterDefinition, setter.Arguments, setterResultType, true, setter.StartLocation)
                )
            );

            if (statements.Count > 1) {
                return new CompoundStatement(statements);
            } else {
                return statements[0];
            }
        }



        // [x, y, z] = a, b, c
        private IStatement GetAssignment(IIdentifier identifiers) {
            CheckOperator(OperatorMeaning.Assignment);

            var right = currentLexeme.StartLocation;
            var value = GetMultipleExpression();

            return ForceAssignVariableList(identifiers, value);
        }



        // [x]++
        private IStatement GetIncrementDecrement(IIdentifier identifiers, bool isIncrement) {
            var location = currentLexeme.StartLocation;
            MoveNextLexeme();

            var one = new PrimitiveLiteralExpression(1, PrimitiveTypeInfo.Int, location);
            var meaning = isIncrement ? OperatorMeaning.Plus : OperatorMeaning.Minus;

            return ForceShorthandAssignList(identifiers, meaning, one, location);
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
                    var isSelfMutable = WhetherExpressionMutable(self);
                    var methodName = functionCall.FunctionDefinition.ShortName;
                    var setter = self.TypeInfo.GetMethodDefinition("s" + methodName.Substring(1), isSelfMutable);
                    var getter = functionCall.FunctionDefinition;

                    Console.WriteLine($"Type of 'self' is '{self.TypeInfo.Name}'");
                    Console.WriteLine($"Method name is '{methodName}'");

                    if (setter == null) {
                        RaiseError("No setter is available for this expression", functionCall.MainLocation);
                    }

                    return new SetterIdentifier(setter, getter, functionCall.Arguments, functionCall.MainLocation);

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



        private IStatement ForceAssignVariableList(IIdentifier identifiers, IExpression value) {
            // Check if value is mutable
            CheckMutability(value, true);

            switch (identifiers) {
                case SingleIdentifier single:
                    return ForceAssignVariable(single, value);


                case SetterIdentifier setter:
                    var definition = setter.SetterDefinition;
                    setter.Arguments.Add(value);
                    var setterResultType = CheckAndConvertFunctionArguments(definition.Signature, setter.Arguments, setter.StartLocation);

                    //var lastType = definition.ArgumentTypes.Last();
                    //var converted = ForceConvertExpression(value, lastType, value.MainLocation);
                    //setter.Arguments.Add(converted);
                    return new ExpressionStatement(
                        new FunctionCallExpression(definition, setter.Arguments, setterResultType, false, setter.StartLocation)
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
                                    subStatements.Add(ForceAssignVariableList(identifier, expression));
                                }
                                break;

                            case VariableExpression variable:
                                GenerateTupleDestruction(subStatements, variable, tupleType, identifierList, VariableQualifier.None,
                                                         (id, expr, qual) => ForceAssignVariableList(id, expr));
                                break;

                            default:
                                GenerateTupleDestructionWithTmp(subStatements, value, tupleType, identifierList, VariableQualifier.None,
                                                                (id, expr, qual) => ForceAssignVariableList(id, expr));
                                break;
                        }

                        return new CompoundStatement(subStatements);

                    } else {
                        RaiseError("Right-side expression is not a tuple. Nothing to unpack", value.MainLocation);
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



        private IStatement ForceAssignVariable(SingleIdentifier identifier, IExpression value) {
            // Check variable definition
            var name = identifier.Name;
            var definition = scopeStack.GetDefinition(name);
            if (definition == null) {
                RaiseError($"Undeclared identifier '{name}'", identifier.StartLocation);
            }
            
            //var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);

            // Check if it's mutable
            if ((definition.Qualifier & VariableQualifier.Final) != 0) {
                RaiseError($"Object '{name}' was declared as final, assignment is impossible", identifier.StartLocation);
            }
            
            var converted = ForceConvertExpression(value, definition.TypeInfo, value.MainLocation);
            CheckMutability(converted, true);

            return new AssignmentStatement(name, definition.Number, 0, converted);
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
            var isItemMutable = WhetherExpressionMutable(iterable);
            var qualifier = MakeQualifier(true, isItemMutable, false);

            if (itemType != null) {
                // Enter scope and add item variable
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(false, true);

                // Either declare an item variable or create a temporary and deconstruct it
                var statements = new List<IStatement>();
                var name = "";
                switch (identifiers) {
                    case SingleIdentifier single:
                        scopeStack.DeclareVariable(single.Name, itemType, qualifier, null);
                        name = single.Name;
                        break;

                    case IdentifierList identifierList:
                        if (itemType is TupleTypeInfo tupleType) {
                            // Need to unpack tuple
                            CheckNumberOfUnpackedValues(identifierList, tupleType);

                            // Declare a temporary to store iterable's item
                            var tupleVariable = DeclareTemporaryVariable(tupleType, null, isItemMutable);
                            name = tupleVariable.Name;

                            // Unpack tuple
                            GenerateTupleDestruction(statements, tupleVariable, tupleType, identifierList,
                                                     qualifier, ForceDeclareVariableList);

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



        // [let | var | use] name = expression
        private IStatement GetVariableDeclaration(VariableQualifier qualifier, Location locationVar) {
            var identifiers = GetIdentifierList();
            CheckOperator(OperatorMeaning.Assignment);

            var locationValue = currentLexeme.StartLocation;
            var value = GetMultipleExpression();

            return ForceDeclareVariableList(identifiers, value, qualifier);
        }



        private IStatement ForceDeclareVariableList(IIdentifier identifiers, IExpression value, VariableQualifier qualifier) {
            // Check if value is mutable
            CheckMutability(value, (qualifier & VariableQualifier.Mutable) != 0);

            switch (identifiers) {
                case SingleIdentifier singleIdentifier:
                    return ForceDeclareVariable(singleIdentifier, value, qualifier);

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
                                    subStatements.Add(ForceDeclareVariableList(identifier, expression, qualifier));
                                }
                                break;

                            case VariableExpression variable:
                                GenerateTupleDestruction(subStatements, variable, tupleType, identifierList,
                                                         qualifier, ForceDeclareVariableList);
                                break;

                            default:
                                GenerateTupleDestructionWithTmp(subStatements, value, tupleType, identifierList,
                                                                qualifier, ForceDeclareVariableList);
                                break;
                        }

                        return new CompoundStatement(subStatements);

                    } else {
                        RaiseError("Expression is not a tuple. Nothing to unpack", value.MainLocation);
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
            VariableQualifier qualifier,
            Func<IIdentifier, IExpression, VariableQualifier, IStatement> func)
        {
            // Declare a tmp 
            var isTmpMutable = WhetherExpressionMutable(value);
            var tupleVariable = DeclareTemporaryVariable(tupleType, value, isTmpMutable);
            subStatements.Add(new VariableDeclarationStatement(tupleVariable.Name, value.TypeInfo, value, qualifier));

            // Now it's safe to use code for VariableExpression case
            GenerateTupleDestruction(subStatements, tupleVariable, tupleType, identifierList, qualifier, func);
        }



        private VariableExpression DeclareTemporaryVariable(ITypeInfo typeInfo, IExpression value, bool isMutable) {
            // Declare a tmp 
            var tmpName = GetNextTmpName();
            var qualifier = MakeQualifier(true, isMutable, false);

            if (!scopeStack.DeclareVariable(tmpName, typeInfo, qualifier, value)) {
                RaiseError($"I wasn't able to declare a temporary '{tmpName}'. WTF???");
            }

            // Get a variable expression
            var definition = scopeStack.GetDefinition(tmpName);
            //var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
            return new VariableExpression(
                tmpName,
                definition.Number,
                0,
                false,
                typeInfo,
                value?.MainLocation
            );
        }



        private void GenerateTupleDestruction(
            List<IStatement> subStatements, 
            VariableExpression tupleVariable,
            TupleTypeInfo tupleType,
            IdentifierList identifierList,
            VariableQualifier qualifier,
            Func<IIdentifier, IExpression, VariableQualifier, IStatement> func)
        {
            var isTupleMutable = WhetherExpressionMutable(tupleVariable);
            var actualCount = tupleType.ItemTypes.Count;
            for (var i = 0; i < actualCount; i++) {
                // Generate a call of built-in function `tupleGet(tuple, index)`
                var arguments = new List<IExpression> {
                                        tupleVariable,
                                        new PrimitiveLiteralExpression(i, PrimitiveTypeInfo.Int, null),
                                    };

                var definition = tupleType.GetTupleAccessorDefinition(i, isTupleMutable);
                var resultType = CheckAndConvertFunctionArguments(definition.Signature, arguments, tupleVariable.MainLocation);

                var expression = new FunctionCallExpression(definition, arguments, resultType, false, null);

                /*var expression = new BuiltinFunctionCallExpression(
                    tupleType.ItemTypes[i],
                    arguments,
                    BuiltinFunctionCallExpression.Option.TupleGet
                );*/

                var identifier = identifierList.Identifiers[i];
                subStatements.Add(func(identifier, expression, qualifier));
            }
        }



        private IStatement ForceDeclareVariable(SingleIdentifier identifier, IExpression value, VariableQualifier qualifier) {
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

            if ((qualifier & VariableQualifier.Disposable) != 0) {
                ForceConvertExpression(converted, new DisposableTypeInfo(), converted.MainLocation);
            }

            /*if (typeInfo is TupleTypeInfo && isMutable) {
                RaiseError($"Tuple object '{identifier.Name}' must be declared as immutable", identifier.StartLocation);
            }*/

            CheckMutability(converted, (qualifier & VariableQualifier.Mutable) != 0);

            if (!scopeStack.DeclareVariable(identifier.Name, typeInfo, qualifier, converted)) {
                RaiseError($"Variable '{identifier.Name}' has already been declared", identifier.StartLocation);
            }

            Console.WriteLine($"Declared object '{identifier.Name}' of type '{typeInfo.Name}'");

            return new VariableDeclarationStatement(identifier.Name, typeInfo, converted, qualifier);
        }



        private void CheckMutability(IExpression expression, bool mustBeMutable) {
            if (mustBeMutable && !WhetherExpressionMutable(expression)) {
                RaiseError("Expression's result is not mutable", expression.MainLocation);
            }
        }



        private bool WhetherExpressionMutable(IExpression expression) {
            if (expression is VariableExpression variable) {
                var definition = scopeStack.GetDefinition(variable.Name);
                var typeInfo = definition.TypeInfo;
                if ((typeInfo.IsReferential || typeInfo is TupleTypeInfo) && (definition.Qualifier & VariableQualifier.Mutable) == 0) {
                    return false;
                }

            } else if (expression is FunctionCallExpression functionCall) {
                var definition = functionCall.FunctionDefinition;
                var signature = definition.Signature;
                if (signature.ResultType.IsReferential && !signature.ResultMutability) {
                    return false;
                }

            } else if (expression is UnaryOperatorExpression unary
                       && unary.OperatorOption == UnaryOperatorExpression.Option.FromMaybe)
            {
                if (!WhetherExpressionMutable(unary.Expression)) {
                    return false;
                }

            } else if (expression is BinaryOperatorExpression binary
                       && binary.OperatorOption == BinaryOperatorExpression.Option.ValueOrDefault)
            {
                if (!WhetherExpressionMutable(binary.LeftOperand)) {
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
            var qualifier = VariableQualifier.None;

            if (WhetherOperator(OperatorMeaning.Var)) {
                qualifier = VariableQualifier.Mutable;
            } else if (WhetherOperator(OperatorMeaning.Let)) {
                qualifier = VariableQualifier.Final;
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
                    var isTmpMutable = WhetherExpressionMutable(value);
                    variable = DeclareTemporaryVariable(value.TypeInfo, value, isTmpMutable);
                    statements.Add(new VariableDeclarationStatement(variable.Name, value.TypeInfo, value, MakeQualifier(true, isTmpMutable, false)));
                    value = variable;
                }

                var condition = new UnaryOperatorExpression(UnaryOperatorExpression.Option.TestNotNull, variable,
                                                            PrimitiveTypeInfo.Bool, variable.MainLocation);

                scopeStack.EnterScope(false, false);
                CheckOperator(OperatorMeaning.OpenBrace);

                // Adjust variable expression on scope enter
                /*variable = new VariableExpression(variable.Name, variable.Number, variable.FrameOffset - 1,
                                                  variable.IsCompileTime, variable.TypeInfo, variable.MainLocation);*/

                // Declare binded variables on scope enter
                List<IStatement> ifStatements = null;
                var unwrapped = new UnaryOperatorExpression(UnaryOperatorExpression.Option.FromMaybe, variable,
                                                            maybeType.InternalType, variable.MainLocation);
                var declaration = ForceDeclareVariableList(identifiers, unwrapped, qualifier);
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
            scopeStack.EnterScope(false, false);
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
                scopeStack.EnterScope(false, false);

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
                scopeStack.EnterScope(false, false);
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

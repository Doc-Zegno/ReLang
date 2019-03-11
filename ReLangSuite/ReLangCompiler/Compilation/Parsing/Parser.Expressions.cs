using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public partial class Parser {
        private IExpression GetFunctionCall(string name, Location location) {
            CheckOperator(OperatorMeaning.OpenParenthesis);

            Console.WriteLine($"parsing call of '{name}'...");

            // Pick all the arguments
            var (names, arguments) = GetFunctionArguments();

            // Filter built-ins
            IFunctionDefinition definition;
            switch (name) {
                case "print":
                    definition = BuiltinFunctionDefinition.Print;
                    break;

                case "enumerate":
                    if (arguments.Count >= 1) {
                        var items = arguments[0];
                        var areItemsMutable = WhetherExpressionMutable(items);
                        definition = BuiltinFunctionDefinition.CreateEnumerate(areItemsMutable);
                    } else {
                        RaiseError("No arguments are provided for this function call", location);
                        definition = null;
                    }
                    break;

                case "zip":
                    if (arguments.Count >= 2) {
                        var itemsA = arguments[0];
                        var itemsB = arguments[1];
                        var areItemsMutable = WhetherExpressionMutable(itemsA) && WhetherExpressionMutable(itemsB);
                        definition = BuiltinFunctionDefinition.CreateZip(areItemsMutable);
                    } else {
                        RaiseError("Not enough arguments provided for this function call", location);
                        definition = null;
                    }
                    break;

                case "open":
                    definition = BuiltinFunctionDefinition.Open;
                    break;

                case "max":
                    if (arguments.Count >= 1) {
                        if (TryConvertExpression(arguments[0], PrimitiveTypeInfo.Int) != null) {
                            definition = BuiltinFunctionDefinition.Maxi;
                        } else {
                            definition = BuiltinFunctionDefinition.Maxf;
                        }
                    } else {
                        RaiseError("Not enough arguments provided for this function call", location);
                        definition = null;
                    }
                    break;

                case "min":
                    if (arguments.Count >= 1) {
                        if (TryConvertExpression(arguments[0], PrimitiveTypeInfo.Int) != null) {
                            definition = BuiltinFunctionDefinition.Mini;
                        } else {
                            definition = BuiltinFunctionDefinition.Minf;
                        }
                    } else {
                        RaiseError("Not enough arguments provided for this function call", location);
                        definition = null;
                    }
                    break;

                default:
                    definition = functionTree.GetFunctionDefinition(name);
                    if (definition == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    }
                    break;
            }

            // Check them against expected types
            var (resultType, convertedArguments) = CheckAndConvertFunctionArguments(definition.Signature, names, arguments, location);

            // return appropriate function call expression
            return new FunctionCallExpression(definition, convertedArguments, resultType, false, location);
        }



        private (List<string>, List<IExpression>) GetFunctionArguments(OperatorMeaning stop = OperatorMeaning.CloseParenthesis) {
            var names = new List<string>();
            var arguments = new List<IExpression>();
            var mustBeNamed = false;

            if (WhetherOperator(stop)) {
                MoveNextLexeme();
            } else {
                while (true) {
                    // Check whether it's named argument
                    var name = "";
                    if (mustBeNamed) {
                        name = GetSymbolText("Argument's name");
                        CheckOperator(OperatorMeaning.Assignment);
                    } else {
                        if (currentLexeme is SymbolLexeme symbol) {
                            MoveNextLexeme();
                            if (WhetherOperator(OperatorMeaning.Assignment)) {
                                MoveNextLexeme();
                                name = symbol.Text;
                                mustBeNamed = true;
                            } else {
                                PutBack();
                            }
                        }
                    }

                    names.Add(name);
                    arguments.Add(GetExpression());
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }
                CheckOperator(stop);
            }

            return (names, arguments);
        }



        private (ITypeInfo, List<IExpression>) CheckAndConvertFunctionArguments(
            FunctionSignature signature, 
            List<string> actualNames, 
            List<IExpression> arguments, 
            Location location) 
        {
            var expectedNames = signature.ArgumentNames;
            var expectedTypes = signature.ArgumentTypes;
            var expectedMutabilities = signature.ArgumentMutabilities;

            var unsetNames = new HashSet<string>(expectedNames);

            var convertedArguments = new List<IExpression>();
            foreach (var e in expectedTypes) {
                convertedArguments.Add(null);
            }

            /*if (arguments.Count > expectedTypes.Count) {
                RaiseError($"Wrong number of arguments for this function call (expected {expectedTypes.Count}"
                           + $" but got {arguments.Count})", location);
            }*/

            for (var index = 0; index < arguments.Count; index++) {
                var actualName = actualNames[index];
                var argument = arguments[index];
                var mapped = index;

                if (actualName != "") {
                    mapped = expectedNames.IndexOf(actualName);
                    if (mapped == -1) {
                        RaiseError($"There is no formal argument with name '{actualName}'", argument.MainLocation);
                    }
                    if (!unsetNames.Contains(actualName)) {
                        RaiseError($"Value has already been assigned to argument '{actualName}'", argument.MainLocation);
                    }
                }

                var expectedName = expectedNames[mapped];
                var expectedType = expectedTypes[mapped];

                // Type checks (and conversions if necessary)
                var converted = TryConvertExpression(argument, expectedType);
                if (converted == null) {
                    RaiseError($"Cannot convert expression to type of argument '{expectedName}' (expected '{expectedType.Name}'"
                               + $" but got '{argument.TypeInfo.Name}')", argument.MainLocation);
                }

                // Debug
                Console.WriteLine($"Converted type: '{converted.TypeInfo.Name}', expected type is '{expectedType.Name}'");

                // Mutability check
                CheckMutability(converted, expectedMutabilities[mapped]);

                convertedArguments[mapped] = converted;
                unsetNames.Remove(expectedName);
            }

            for (var i = 0; i < expectedTypes.Count; i++) {
                if (convertedArguments[i] == null) {
                    var defaultValue = signature.ArgumentDefaultValues[i];
                    if (defaultValue != null) {
                        convertedArguments[i] = defaultValue;
                    } else {
                        RaiseError($"No value was assigned to argument '{expectedNames[i]}'", location);
                    }
                }
            }

            var resultType = signature.ResultType;
            var resolvedReturnType = resultType.ResolveGeneric();
            if (resolvedReturnType == null) {
                RaiseError($"Cannot resolve return type '{resultType.Name}' for this function call", location);
            }
            return (resolvedReturnType, convertedArguments);
        }



        // expr1, expr2, expr3 ...
        private IExpression GetMultipleExpression() {
            var items = new List<IExpression>();
            while (true) {
                items.Add(GetExpression());
                if (WhetherOperator(OperatorMeaning.Comma)) {
                    MoveNextLexeme();
                } else {
                    break;
                }
            }

            if (items.Count > 1) {
                return new TupleLiteralExpression(items);
            } else {
                return items[0];
            }
        }



        private IExpression GetExpression() {
            return GetValueOrDefaultExpression();
        }



        // [a, b, c].append(d)
        private IExpression GetAtomicExpression() {
            var expression = GetPrimitiveExpression();
            while (currentLexeme is OperatorLexeme operatorLexeme) {
                switch (operatorLexeme.Meaning) {
                    case OperatorMeaning.QuestionMark:
                        if (expression is TypeLiteralExpression typeLiteral) {
                            MoveNextLexeme();
                            expression = new TypeLiteralExpression(
                                new MaybeTypeInfo(typeLiteral.InternalType), 
                                currentLexeme.StartLocation
                            );
                        } else {
                            return expression;
                        }
                        break;

                    case OperatorMeaning.Asterisk:
                        if (expression is TypeLiteralExpression typeLiteral2) {
                            MoveNextLexeme();
                            expression = new TypeLiteralExpression(
                                new IterableTypeInfo(typeLiteral2.InternalType), 
                                currentLexeme.StartLocation
                            );
                        } else {
                            return expression;
                        }
                        break;

                    case OperatorMeaning.Dot:
                        // Access to the field
                        expression = GetMemberAccessExpression(expression);
                        break;

                    case OperatorMeaning.OpenParenthesis:
                        // Function or constructor call
                        expression = GetConstructorOrFunctionCall(expression);
                        break;

                    case OperatorMeaning.OpenBracket:
                        // Indexing
                        expression = GetIndexingExpression(expression);
                        break;

                    case OperatorMeaning.ExclamationMark:
                        expression = GetFromMaybe(expression);
                        break;

                    default:
                        return expression;
                }
            }
            return expression;
        }



        private IExpression GetFromMaybe(IExpression expression) {
            var location = currentLexeme.StartLocation;
            CheckOperator(OperatorMeaning.ExclamationMark);

            if (expression.TypeInfo is MaybeTypeInfo maybeType) {
                if (expression.IsCompileTime) {
                    if (expression.Value == null) {
                        RaiseError("Expression is equal to 'null'", expression.MainLocation);
                    }
                    return expression.ChangeType(maybeType.InternalType);
                } else {
                    return new UnaryOperatorExpression(UnaryOperatorExpression.Option.FromMaybe, expression,
                                                       maybeType.InternalType, location);
                }
   
            } else {
                RaiseError("Expression is not a maybe", expression.MainLocation);
                return null;
            }
        }



        // [i]
        private IExpression GetIndexingExpression(IExpression self) {
            var location = currentLexeme.StartLocation;
            CheckOperator(OperatorMeaning.OpenBracket);

            // Decide whether it's slicing or just indexing
            var arguments = new List<IExpression> { self };  // self
            if (WhetherOperator(OperatorMeaning.Colon)) {
                // Slicing with start = 0
                var start = new PrimitiveLiteralExpression(0, PrimitiveTypeInfo.Int, currentLexeme.StartLocation);
                return GetSliceExpression(self, location, start);

            } else {
                var argument = GetExpression();
                if (WhetherOperator(OperatorMeaning.Colon)) {
                    // Slicing with specified start
                    return GetSliceExpression(self, location, argument);
                }

                // Indexing
                CheckOperator(OperatorMeaning.CloseBracket);
                arguments.Add(argument);

                // Get definition of "get" and make function call
                var isSelfMutable = WhetherExpressionMutable(self);
                var definition = self.TypeInfo.GetMethodDefinition("get", isSelfMutable);

                if (definition == null) {
                    RaiseError($"Type '{self.TypeInfo.Name}' doesn't implement indexing", location);
                }

                var actualNames = new List<string>();
                foreach (var arg in arguments) {
                    actualNames.Add("");
                }
                
                var (resultType, convertedArguments) =
                    CheckAndConvertFunctionArguments(definition.Signature, actualNames, arguments, location);
                return new FunctionCallExpression(definition, convertedArguments, resultType, true, location);
            }
        }



        // {[start}:end:step]
        private IExpression GetSliceExpression(IExpression self, Location locationBracket, IExpression start) {
            // Default values for 'end' and 'step'
            IExpression end = new NullLiteralExpression(currentLexeme.StartLocation)
                                  .ChangeType(new MaybeTypeInfo(PrimitiveTypeInfo.Int));
            IExpression step = new PrimitiveLiteralExpression(1, PrimitiveTypeInfo.Int, currentLexeme.StartLocation);

            CheckOperator(OperatorMeaning.Colon);
            // Possible options:
            // [start:]
            // [start::step]
            // [start:end]
            // [start:end:step]
            if (WhetherOperator(OperatorMeaning.CloseBracket)) {
                // 'end' and 'step' were skipped
                // Do nothing

            } else if (WhetherOperator(OperatorMeaning.Colon)) {
                // 'end' was skipped
                // 'step' is still there
                MoveNextLexeme();
                step = GetExpression();

            } else {
                // 'end' is still there
                end = GetExpression();
                if (WhetherOperator(OperatorMeaning.Colon)) {
                    // 'step' is still there
                    MoveNextLexeme();
                    step = GetExpression();
                }
            }

            CheckOperator(OperatorMeaning.CloseBracket);

            // Depending on mutability of 'self' call either 'getSlice' or 'getConstSlice'
            var name = "getSlice";
            var isSelfMutable = WhetherExpressionMutable(self);
            var definition = self.TypeInfo.GetMethodDefinition(name, isSelfMutable);

            if (definition == null) {
                RaiseError($"This object doesn't support method '{name}'", locationBracket);
            }

            var arguments = new List<IExpression> { self, start, end, step };
            var actualNames = new List<string> { "", "", "", "" };
            var (resultType, convertedArguments) =
                CheckAndConvertFunctionArguments(definition.Signature, actualNames, arguments, locationBracket);
            return new FunctionCallExpression(definition, convertedArguments, resultType, false, locationBracket);
        }



        // .append(d)
        private IExpression GetMemberAccessExpression(IExpression self) {
            CheckOperator(OperatorMeaning.Dot);
            var location = currentLexeme.StartLocation;
            var name = GetSymbolText("Member's name");

            var arguments = new List<IExpression> { self };
            var actualNames = new List<string> { "" };
            IFunctionDefinition definition = null;
            var isLvalue = false;

            var isSelfMutable = WhetherExpressionMutable(self);

            if (WhetherOperator(OperatorMeaning.OpenParenthesis)) {
                // Get method's definition
                definition = self.TypeInfo.GetMethodDefinition(name, isSelfMutable);
                if (definition == null) {
                    RaiseError($"Type '{self.TypeInfo.Name}' doesn't implement method '{name}'", location);
                }

                MoveNextLexeme();
                var (tailNames, tailArguments) = GetFunctionArguments();
                arguments.AddRange(tailArguments);
                actualNames.AddRange(tailNames);

            } else {
                // Get property's definition
                var fullName = "get" + char.ToUpper(name[0]) + name.Substring(1);
                definition = self.TypeInfo.GetMethodDefinition(fullName, isSelfMutable);
                if (definition == null) {
                    RaiseError($"Type '{self.TypeInfo.Name}' doesn't have property '{name}'", location);
                }
                isLvalue = true;
            }

            // Check arguments' types
            var (resultType, convertedArguments) =
                CheckAndConvertFunctionArguments(definition.Signature, actualNames, arguments, location);

            return new FunctionCallExpression(definition, convertedArguments, resultType, isLvalue, location);
        }



        // variable, f(x, y, z), (a + b), [a, b, c], {a, b, c}, literal
        private IExpression GetPrimitiveExpression() {
            var location = currentLexeme.StartLocation;
            switch (currentLexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.OpenParenthesis:
                            MoveNextLexeme();
                            var expression = GetMultipleExpression();
                            CheckOperator(OperatorMeaning.CloseParenthesis);

                            // Can be tuple of type expressions
                            if (expression is TupleLiteralExpression tupleLiteral) {
                                var isTypeLiteral = false;
                                var typeLiterals = new List<ITypeInfo>();
                                for (var i = 0; i < tupleLiteral.Items.Count; i++) {
                                    var item = tupleLiteral.Items[i];
                                    if (item is TypeLiteralExpression typeLiteral) {
                                        if (i > 0 && !isTypeLiteral) {
                                            RaiseError("Expression was expected but type literal was found", item.MainLocation);
                                        }

                                        isTypeLiteral = true;
                                        typeLiterals.Add(typeLiteral.InternalType);
                                    } else {
                                        if (isTypeLiteral) {
                                            RaiseError("Type literal was expected but expression was found", item.MainLocation);
                                        }
                                    }
                                }

                                if (isTypeLiteral) {
                                    return new TypeLiteralExpression(new TupleTypeInfo(typeLiterals), location);
                                }

                            } else if (expression is TypeLiteralExpression typeLiteral) {
                                // Single type literal within parenthesis is not allowed for now
                                // since functional types are not supported in 1st Revision
                                RaiseError("Tuple type literals with one item are not supported", expression.MainLocation);
                            }

                            return expression;

                        case OperatorMeaning.OpenBracket:
                            MoveNextLexeme();
                            return GetListLiteral(location);

                        case OperatorMeaning.OpenBrace:
                            MoveNextLexeme();
                            return GetSetOrDictionaryLiteral(location);

                        case OperatorMeaning.Null:
                            MoveNextLexeme();
                            return new NullLiteralExpression(location);

                        default:
                            RaiseError($"Unexpected operator: {GetOperatorName(operatorLexeme.Meaning)}");
                            return null;
                    }

                case SymbolLexeme symbol:
                    MoveNextLexeme();
                    return GetPrimitiveSymbolExpression(symbol.Text, location);

                case LiteralLexeme literal:
                    var typeOption = PrimitiveTypeInfo.Option.Void;
                    switch (literal.Value) {
                        case bool value:
                            typeOption = PrimitiveTypeInfo.Option.Bool;
                            break;

                        case char value:
                            typeOption = PrimitiveTypeInfo.Option.Char;
                            break;

                        case int value:
                            typeOption = PrimitiveTypeInfo.Option.Int;
                            break;

                        case double value:
                            typeOption = PrimitiveTypeInfo.Option.Float;
                            break;

                        case string value:
                            typeOption = PrimitiveTypeInfo.Option.String;
                            break;

                        default:
                            RaiseError("Unknown literal");
                            break;
                    }
                    MoveNextLexeme();
                    return new PrimitiveLiteralExpression(literal.Value, new PrimitiveTypeInfo(typeOption), location);

                case FormatStringLexeme formatString:
                    MoveNextLexeme();
                    return GetFormatStringExpression(formatString);

                default:
                    RaiseError("Expression was expected");
                    break;
            }

            // Sanity check
            return null;
        }



        // [identifier]
        // [Type]
        // [Type]<T>
        private IExpression GetPrimitiveSymbolExpression(string name, Location location) {
            if (char.IsUpper(name[0])) {
                // Try parse as an error literal
                var maybe = TryGetErrorOption(name, location);
                if (maybe != null) {
                    return new TypeLiteralExpression(
                        new ErrorTypeInfo(maybe.Value), location
                    );
                }

                // Type
                ITypeInfo targetType = null;
                ITypeInfo keyType = null;
                ITypeInfo valueType = null;
                switch (name) {
                    case "Object":
                        targetType = PrimitiveTypeInfo.Object;
                        break;

                    case "Bool":
                        targetType = PrimitiveTypeInfo.Bool;
                        break;

                    case "Char":
                        targetType = PrimitiveTypeInfo.Char;
                        break;

                    case "Int":
                        targetType = PrimitiveTypeInfo.Int;
                        break;

                    case "Float":
                        targetType = PrimitiveTypeInfo.Float;
                        break;

                    case "String":
                        targetType = PrimitiveTypeInfo.String;
                        break;

                    case "List":
                        if (WhetherOperator(OperatorMeaning.Less)) {
                            MoveNextLexeme();
                            keyType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.More);
                        } else {
                            keyType = new IncompleteTypeInfo();
                        }
                        targetType = new ArrayListTypeInfo(keyType);
                        break;

                    case "Set":
                        if (WhetherOperator(OperatorMeaning.Less)) {
                            MoveNextLexeme();
                            keyType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.More);
                        } else {
                            keyType = new IncompleteTypeInfo();
                        }
                        targetType = new HashSetTypeInfo(keyType);  // Doesn't matter
                        break;

                    case "Dictionary":
                        if (WhetherOperator(OperatorMeaning.Less)) {
                            MoveNextLexeme();
                            keyType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.Comma);
                            valueType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.More);
                        } else {
                            keyType = new IncompleteTypeInfo();
                            valueType = new IncompleteTypeInfo();
                        }
                        targetType = new DictionaryTypeInfo(keyType, valueType);
                        break;

                    case "Range":
                        targetType = new RangeTypeInfo(PrimitiveTypeInfo.Int);
                        break;

                    default:
                        RaiseError($"Unknown type '{name}'", location);
                        return null;
                }

                return new TypeLiteralExpression(targetType, location);

            } else if (char.IsLower(name[0])) {
                // Variable or function 
                var functionDefinition = TryGetBuiltinFunctionDefinition(name);
                if (functionDefinition == null) {
                    functionDefinition = functionTree.GetFunctionDefinition(name);
                }

                if (functionDefinition != null) {
                    // Try function
                    return new FunctionLiteralExpression(functionDefinition, location);

                } else {
                    // Try variable
                    var definition = scopeStack.GetDefinition(name);
                    if (definition == null) {
                        RaiseError($"Undeclared identifier '{name}'", location);
                    }

                    if (definition.Qualifier == VariableQualifier.Final && definition.Value != null && definition.Value.IsCompileTime) {
                        // Can be evaluated at compile-time
                        return definition.Value;

                        //var value = definition.Value;
                        //return new PrimitiveLiteralExpression(value.Value, value.TypeInfo);
                    } else {
                        // Should be resolved at run-time
                        //var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                        return new VariableExpression(name, definition.Number, 0, false, definition.TypeInfo, location);
                    }
                }

            } else {
                RaiseError("Only identifiers starting with a letter are allowed here", location);
                return null;
            }
        }



        private IFunctionDefinition TryGetBuiltinFunctionDefinition(string name) {
            switch (name) {
                case "print":
                    return BuiltinFunctionDefinition.Print;

                case "enumerate":
                    return BuiltinFunctionDefinition.CreateEnumerate(false);

                case "zip":
                    return BuiltinFunctionDefinition.CreateZip(false);

                case "open":
                    return BuiltinFunctionDefinition.Open;

                case "max":
                    return BuiltinFunctionDefinition.Maxf;

                case "min":
                    return BuiltinFunctionDefinition.Minf;

                default:
                    return null;
            }
        }



        // [$"{]x}, {y}"
        private IExpression GetFormatStringExpression(FormatStringLexeme startFormat) {
            var pieces = new List<string> { startFormat.Piece };
            var expressions = new List<IExpression>();

            var format = startFormat;
            while (!format.IsEnding) {
                var expression = GetMultipleExpression();
                var converted = ForceConvertExpression(expression, PrimitiveTypeInfo.Object, expression.MainLocation);
                expressions.Add(converted);

                if (currentLexeme is FormatStringLexeme nextFormat) {
                    MoveNextLexeme();
                    pieces.Add(nextFormat.Piece);
                    format = nextFormat;
                } else {
                    RaiseError("Format string's piece was expected");
                }
            }

            if (expressions.Count > 0) {
                return new FormatStringExpression(pieces, expressions, startFormat.StartLocation);
            } else {
                return new PrimitiveLiteralExpression(pieces[0], PrimitiveTypeInfo.String, startFormat.StartLocation);
            }
        }



        private IExpression GetConstructorOrFunctionCall(IExpression callable) {
            var location = currentLexeme.StartLocation;
            CheckOperator(OperatorMeaning.OpenParenthesis);
            var (argumentsNames, arguments) = GetFunctionArguments();

            if (callable is TypeLiteralExpression typeLiteral) {
                var internalType = typeLiteral.InternalType;

                // Use in-place constructor call for error literals
                if (internalType is ErrorTypeInfo errorType) {
                    if (arguments.Count != 1) {
                        RaiseError("Error object's constructor takes only one argument", location);
                    }

                    var description = arguments[0];
                    var converted = ForceConvertExpression(description, PrimitiveTypeInfo.String, description.MainLocation);
                    return new ErrorLiteralExpression(errorType.ErrorOption, converted, location);
                }

                // Try interpret as a default constructor call
                if (arguments.Count == 0) {
                    var defaultValue = internalType.GetDefaultValue(location);
                    if (defaultValue != null) {
                        return defaultValue;
                    }
                }

                // Try interpret this as an explicit conversion
                if (arguments.Count == 1) {
                    var constructed = TryConstructFrom(arguments[0], internalType, typeLiteral.MainLocation);
                    if (constructed != null) {
                        return constructed;
                    }
                }

                // Interpret as a custom constructor call otherwise
                var definition = internalType.GetMethodDefinition("init", false);
                if (definition == null) {
                    RaiseError($"Type '{internalType.Name}' has no constructor", location);
                }

                var (resultType, convertedArguments) = CheckAndConvertFunctionArguments(
                    definition.Signature, argumentsNames, arguments, location
                );

                return new FunctionCallExpression(definition, convertedArguments, resultType, false, location);

            } else if (callable is FunctionLiteralExpression functionLiteral) {
                var definition = functionLiteral.Definition;
                var (resultType, convertedArguments) = CheckAndConvertFunctionArguments(
                    definition.Signature, argumentsNames, arguments, location
                );

                return new FunctionCallExpression(definition, convertedArguments, resultType, false, location);

            } else if (callable.TypeInfo is FunctionTypeInfo functionType) {
                // TODO: implement
                var definition = functionType.GetMethodDefinition("call", WhetherExpressionMutable(callable));
                if (definition == null) {
                    RaiseError($"Type '{functionType.Name}' doesn't implement functional interface", location);
                }

                arguments.Insert(0, callable);
                argumentsNames.Insert(0, "");
                var (resultType, convertedArguments) = CheckAndConvertFunctionArguments(
                    definition.Signature, argumentsNames, arguments, location
                );

                return new FunctionCallExpression(definition, convertedArguments, resultType, false, location);

            } else {
                RaiseError("Expression is not callable", callable.MainLocation);
                return null;
            }
        }



        // {[}a, b, c, d, e]
        private IExpression GetListLiteral(Location location) {
            if (WhetherOperator(OperatorMeaning.CloseBracket)) {
                MoveNextLexeme();
                return new ListLiteralExpression(new List<IExpression>(), new IncompleteTypeInfo(), location);
            } else {
                var firstItem = GetExpression();

                // Can be type literal
                if (firstItem is TypeLiteralExpression typeLiteral) {
                    CheckOperator(OperatorMeaning.CloseBracket);
                    return new TypeLiteralExpression(new ArrayListTypeInfo(typeLiteral.InternalType), location);
                }

                var items = GetItemList(firstItem);
                CheckOperator(OperatorMeaning.CloseBracket);
                return new ListLiteralExpression(items, items[0].TypeInfo, location);
            }
        }



        // {a, b, c, d, e}
        private IExpression GetSetOrDictionaryLiteral(Location location) {
            if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                MoveNextLexeme();
                return new SetLiteralExpression(new List<IExpression>(), new IncompleteTypeInfo(), location);

            } else if (WhetherOperator(OperatorMeaning.Colon)) {
                MoveNextLexeme();
                CheckOperator(OperatorMeaning.CloseBrace);
                return new DictionaryLiteralExpression(new List<(IExpression, IExpression)>(),
                                                       new IncompleteTypeInfo(), new IncompleteTypeInfo(), location);

            } else {
                // Determine whether this is set or dictionary
                var key = GetExpression();

                if (WhetherOperator(OperatorMeaning.Colon)) {
                    // Dictionary
                    MoveNextLexeme();
                    var value = GetExpression();

                    // Can be type literal
                    if (key is TypeLiteralExpression keyTypeLiteral) {
                        if (value is TypeLiteralExpression valueTypeLiteral) {
                            CheckOperator(OperatorMeaning.CloseBrace);
                            return new TypeLiteralExpression(
                                new DictionaryTypeInfo(keyTypeLiteral.InternalType, valueTypeLiteral.InternalType),
                                location
                            );
                        } else {
                            RaiseError("Expected type literal but got expression", value.MainLocation);
                        }
                    }

                    var pairs = GetPairList(key, value);
                    CheckOperator(OperatorMeaning.CloseBrace);
                    return new DictionaryLiteralExpression(pairs, pairs[0].Item1.TypeInfo, pairs[0].Item2.TypeInfo, location);

                } else {
                    // Set
                    // Can be type literal
                    if (key is TypeLiteralExpression typeLiteral) {
                        CheckOperator(OperatorMeaning.CloseBrace);
                        return new TypeLiteralExpression(new HashSetTypeInfo(typeLiteral.InternalType), location);
                    }

                    var items = GetItemList(key);
                    CheckOperator(OperatorMeaning.CloseBrace);
                    return new SetLiteralExpression(items, items[0].TypeInfo, location);
                }
            }
        }



        private List<(IExpression, IExpression)> GetPairList(IExpression firstKey, IExpression firstValue) {
            var pairs = new List<(IExpression, IExpression)> { (firstKey, firstValue) };
            var joinedKeyType = firstKey.TypeInfo;
            var joinedValueType = firstValue.TypeInfo;
            var leftKey = firstKey;
            var leftValue = firstValue;

            while (WhetherOperator(OperatorMeaning.Comma)) {
                MoveNextLexeme();

                // Get key and check type
                var rightKey = GetExpression();
                joinedKeyType = ForceJoinTypes(leftKey, rightKey);
                leftKey = joinedKeyType.ConvertFrom(rightKey);

                // Swallow colon
                CheckOperator(OperatorMeaning.Colon);

                // Get value and check type
                var rightValue = GetExpression();
                joinedValueType = ForceJoinTypes(leftValue, rightValue);
                leftValue = joinedValueType.ConvertFrom(rightValue);

                pairs.Add((leftKey, leftValue));
            }

            return new List<(IExpression, IExpression)>(
                pairs.Select(
                    pair => (joinedKeyType.ConvertFrom(pair.Item1), joinedValueType.ConvertFrom(pair.Item2))
                )
            );
        }



        private List<IExpression> GetItemList(IExpression firstItem) {
            var items = new List<IExpression> { firstItem };
            var joinedType = firstItem.TypeInfo;
            var leftItem = firstItem;


            while (WhetherOperator(OperatorMeaning.Comma)) {
                MoveNextLexeme();
                var rightItem = GetExpression();
                items.Add(rightItem);
                joinedType = ForceJoinTypes(leftItem, rightItem);
                leftItem = joinedType.ConvertFrom(rightItem);
            }

            return new List<IExpression>(items.Select(item => joinedType.ConvertFrom(item)));
        }



        // maybe ?? value
        private IExpression GetValueOrDefaultExpression() {
            var left = GetOrExpression();

            if (WhetherOperator(OperatorMeaning.ValueOrDefault)) {
                var location = currentLexeme.StartLocation;

                MoveNextLexeme();
                var right = GetOrExpression();

                if (left.TypeInfo is MaybeTypeInfo maybeType) {
                    var y = ForceConvertExpression(right, maybeType.InternalType, right.MainLocation);

                    if (left.IsCompileTime) {
                        if (left.Value != null) {
                            left = left.ChangeType(maybeType.InternalType);
                        } else {
                            left = y;
                        }
                    } else {
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.ValueOrDefault, left, y, location);
                    }

                } else {
                    RaiseError("Left operand must have maybe type", left.MainLocation);
                }
            }

            return left;
        }



        // a || b
        private IExpression GetOrExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetAndExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (WhetherOperator(OperatorMeaning.Or)) {
                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetAndExpression();

                    // Try to convert to boolean
                    var x = ForceConvertExpression(left, PrimitiveTypeInfo.Bool, locationLeft);
                    var y = ForceConvertExpression(right, PrimitiveTypeInfo.Bool, locationRight);

                    if (x.IsCompileTime) {
                        if ((bool)x.Value) {
                            left = new PrimitiveLiteralExpression(true, PrimitiveTypeInfo.Bool, locationLeft);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.Or, x, y, locationMiddle);
                    }

                } else {
                    return left;
                }
            }
        }



        // a && b
        private IExpression GetAndExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetRelationalExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (WhetherOperator(OperatorMeaning.And)) {
                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetRelationalExpression();

                    // Try to convert to boolean
                    var x = ForceConvertExpression(left, PrimitiveTypeInfo.Bool, locationLeft);
                    var y = ForceConvertExpression(right, PrimitiveTypeInfo.Bool, locationRight);

                    if (x.IsCompileTime) {
                        if (!(bool)x.Value) {
                            left = new PrimitiveLiteralExpression(false, PrimitiveTypeInfo.Bool, locationLeft);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.And, x, y, locationMiddle);
                    }

                } else {
                    return left;
                }
            }
        }



        // a > b, a != b
        private IExpression GetRelationalExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetRangeExpression();

            var locationMiddle = currentLexeme.StartLocation;

            if (currentLexeme is OperatorLexeme operatorLexeme) {
                var meaning = operatorLexeme.Meaning;
                switch (meaning) {
                    case OperatorMeaning.Equal:
                    case OperatorMeaning.NotEqual:
                    case OperatorMeaning.Less:
                    case OperatorMeaning.LessOrEqual:
                    case OperatorMeaning.More:
                    case OperatorMeaning.MoreOrEqual:
                    case OperatorMeaning.In:
                        break;

                    default:
                        return left;
                }

                // Get right operand
                MoveNextLexeme();
                var right = GetRangeExpression();

                if (meaning == OperatorMeaning.In) {
                    // Resolve to .contains() call
                    var definition = right.TypeInfo.GetMethodDefinition("contains", false);
                    if (definition == null) {
                        RaiseError($"Type '{right.TypeInfo.Name}' doesn't implement method 'contains'", right.MainLocation);
                    }

                    var arguments = new List<IExpression> { right, left };
                    var actualNames = new List<string> { "", "" };
                    var (resultType, convertedArguments) =
                        CheckAndConvertFunctionArguments(definition.Signature, actualNames, arguments, locationMiddle);
                    left = new FunctionCallExpression(definition, convertedArguments, resultType, false, locationMiddle);

                } else if (left.TypeInfo is MaybeTypeInfo maybeType) {
                    if (left is NullLiteralExpression) {
                        RaiseError("'null' is not allowed here", left.MainLocation);
                    }

                    if (!(right is NullLiteralExpression)) {
                        RaiseError("This operand should be a 'null' literal", right.MainLocation);
                    }

                    switch (meaning) {
                        case OperatorMeaning.Equal:
                            if (left.IsCompileTime) {
                                left = new PrimitiveLiteralExpression(left.Value == null, PrimitiveTypeInfo.Bool, locationMiddle);
                            } else {
                                left = new UnaryOperatorExpression(UnaryOperatorExpression.Option.TestNull, left,
                                                                   PrimitiveTypeInfo.Bool, locationMiddle);
                            }
                            break;

                        case OperatorMeaning.NotEqual:
                            if (left.IsCompileTime) {
                                left = new PrimitiveLiteralExpression(left.Value != null, PrimitiveTypeInfo.Bool, locationMiddle);
                            } else {
                                left = new UnaryOperatorExpression(UnaryOperatorExpression.Option.TestNotNull, left,
                                                                   PrimitiveTypeInfo.Bool, locationMiddle);
                            }
                            break;

                        default:
                            RaiseError("Maybe types don't support this operator", locationMiddle);
                            break;
                    }

                } else if (left.TypeInfo is PrimitiveTypeInfo primitive) {
                    // Try to convert to each other
                    var (x, y) = CrossConvert(left, right, locationLeft);

                    switch (primitive.TypeOption) {
                        case PrimitiveTypeInfo.Option.Bool:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (bool)x.Value;
                                var b = (bool)y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool, locationLeft);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualBoolean;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualBoolean;
                                } else {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Int:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (int)x.Value;
                                var b = (int)y.Value;
                                var result = false;

                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        result = a == b;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        result = a != b;
                                        break;

                                    case OperatorMeaning.Less:
                                        result = a < b;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        result = a <= b;
                                        break;

                                    case OperatorMeaning.More:
                                        result = a > b;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        result = a >= b;
                                        break;

                                    default:
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool, locationLeft);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        option = BinaryOperatorExpression.Option.EqualInteger;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        option = BinaryOperatorExpression.Option.NotEqualInteger;
                                        break;

                                    case OperatorMeaning.Less:
                                        option = BinaryOperatorExpression.Option.LessInteger;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        option = BinaryOperatorExpression.Option.LessOrEqualInteger;
                                        break;

                                    case OperatorMeaning.More:
                                        option = BinaryOperatorExpression.Option.MoreInteger;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        option = BinaryOperatorExpression.Option.MoreOrEqualInteger;
                                        break;

                                    default:
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Float:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (double)x.Value;
                                var b = (double)y.Value;
                                var result = false;

                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        result = a == b;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        result = a != b;
                                        break;

                                    case OperatorMeaning.Less:
                                        result = a < b;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        result = a <= b;
                                        break;

                                    case OperatorMeaning.More:
                                        result = a > b;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        result = a >= b;
                                        break;

                                    default:
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool, locationLeft);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        option = BinaryOperatorExpression.Option.EqualFloating;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        option = BinaryOperatorExpression.Option.NotEqualFloating;
                                        break;

                                    case OperatorMeaning.Less:
                                        option = BinaryOperatorExpression.Option.LessFloating;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        option = BinaryOperatorExpression.Option.LessOrEqualFloating;
                                        break;

                                    case OperatorMeaning.More:
                                        option = BinaryOperatorExpression.Option.MoreFloating;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        option = BinaryOperatorExpression.Option.MoreOrEqualFloating;
                                        break;

                                    default:
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.String:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (string)x.Value;
                                var b = (string)y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("String operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool, locationLeft);

                            } else {
                                // Delay until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualString;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualString;
                                } else {
                                    RaiseError("String operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Object:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = x.Value;
                                var b = y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("Object operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool, locationLeft);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualObject;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualObject;
                                } else {
                                    RaiseError("Object operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                            }
                            break;


                        default:
                            RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                            return null;
                    }

                } else {
                    RaiseError($"Unsupported type '{left.TypeInfo.Name}' for this operator", locationLeft);
                }
            }

            return left;
        }



        // a..b
        private IExpression GetRangeExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetSumExpression();

            if (WhetherOperator(OperatorMeaning.Range)) {
                MoveNextLexeme();

                var locationRight = currentLexeme.StartLocation;
                var right = GetSumExpression();

                var intType = PrimitiveTypeInfo.Int;
                var x = ForceConvertExpression(left, intType, locationLeft);
                var y = ForceConvertExpression(right, intType, locationRight);

                if (x.IsCompileTime && y.IsCompileTime) {
                    var a = (int)x.Value;
                    var b = (int)y.Value;

                    if (a >= b) {
                        RaiseError($"Empty range: start index (got {a}) must be smaller than end one (got {b})", locationLeft, true);
                    }
                }

                return new RangeLiteralExpression(x, y);
            } else {
                return left;
            }
        }



        // a + b, a - b
        private IExpression GetSumExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetProductExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
                        case OperatorMeaning.Plus:
                        case OperatorMeaning.Minus:
                            MoveNextLexeme();
                            if (WhetherOperator(OperatorMeaning.Assignment)) {
                                PutBack();
                                return left;
                            }
                            PutBack();
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var right = GetProductExpression();

                    // Force create binary expression
                    left = CreateBinaryExpression(meaning, left, right, locationMiddle);

                } else {
                    return left;
                }
            }
        }


        private (IExpression, IExpression) CrossConvert(IExpression left, IExpression right, Location location) {
            var x = right.TypeInfo.ConvertFrom(left);
            if (x != null) {
                return (x, right);
            } else {
                var y = left.TypeInfo.ConvertFrom(right);
                if (y != null) {
                    return (left, y);
                } else {
                    var hint = $"('{left.TypeInfo.Name}' and '{right.TypeInfo.Name}')";
                    RaiseError($"Operands of binary operator {hint} are not convertible to each other", location);
                    return (null, null);
                }
            }
        }



        // a * b, a / b, a \ b, a % b
        private IExpression GetProductExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetNegateExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
                        case OperatorMeaning.Asterisk:
                        case OperatorMeaning.ForwardSlash:
                        case OperatorMeaning.BackSlash:
                        case OperatorMeaning.Modulo:
                            MoveNextLexeme();
                            if (WhetherOperator(OperatorMeaning.Assignment)) {
                                PutBack();
                                return left;
                            }
                            PutBack();
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetNegateExpression();

                    // Force create binary expression
                    left = CreateBinaryExpression(meaning, left, right, locationMiddle);

                } else {
                    return left;
                }
            }
        }



        private IExpression GetNegateExpression() {
            if (currentLexeme is OperatorLexeme operatorLexeme) {
                var mainLocation = currentLexeme.StartLocation;
                var meaning = operatorLexeme.Meaning;
                switch (meaning) {
                    case OperatorMeaning.ExclamationMark:
                    case OperatorMeaning.Minus:
                        break;

                    default:
                        return GetAtomicExpression();
                }

                MoveNextLexeme();
                var location = currentLexeme.StartLocation;
                var atomic = GetAtomicExpression();
                var typeName = atomic.TypeInfo.Name;
                IExpression converted;

                switch (meaning) {
                    case OperatorMeaning.ExclamationMark:
                        converted = TryConvertExpression(atomic, PrimitiveTypeInfo.Bool);
                        if (converted != null) {
                            if (converted.IsCompileTime) {
                                var a = (bool)converted.Value;
                                return new PrimitiveLiteralExpression(!a, PrimitiveTypeInfo.Bool, mainLocation);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.Not, converted,
                                                                   PrimitiveTypeInfo.Bool, mainLocation);
                            }
                        } else {
                            RaiseError($"Operand of logical Not must be a boolean expression (got '{typeName}')");
                            return null;
                        }


                    case OperatorMeaning.Minus:
                        converted = TryConvertExpression(atomic, PrimitiveTypeInfo.Int);
                        if (converted != null) {
                            if (converted.IsCompileTime) {
                                var a = (int)converted.Value;
                                return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Int, mainLocation);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateInteger, converted,
                                                                   PrimitiveTypeInfo.Int, mainLocation);
                            }

                        } else {
                            converted = TryConvertExpression(atomic, PrimitiveTypeInfo.Float);
                            if (converted != null) {
                                if (converted.IsCompileTime) {
                                    var a = (double)converted.Value;
                                    return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Float, mainLocation);
                                } else {
                                    return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateFloating, converted,
                                                                       PrimitiveTypeInfo.Float, mainLocation);
                                }
                            } else {
                                RaiseError($"Operand of negation operator must be a numeric expression (got '{typeName}')");
                                return null;
                            }
                        }


                    default:
                        throw new NotImplementedException();
                }
            } else {
                return GetAtomicExpression();
            }
        }



        private IExpression CreateBinaryExpression(OperatorMeaning meaning, IExpression left, IExpression right, Location middle) {
            // Try to convert to each other
            var (x, y) = CrossConvert(left, right, left.MainLocation);

            if (x.TypeInfo is ArrayListTypeInfo arrayList) {
                if (meaning == OperatorMeaning.Plus) {
                    return new BinaryOperatorExpression(BinaryOperatorExpression.Option.AddList, x, y, middle);
                } else {
                    RaiseError($"Type '{x.TypeInfo.Name}' doesn't support this operator", middle);
                    return null;
                }

            } else if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                switch (primitive.TypeOption) {
                    case PrimitiveTypeInfo.Option.Int:
                        if (x.IsCompileTime && y.IsCompileTime) {
                            var a = (int)x.Value;
                            var b = (int)y.Value;
                            var typeInfo = PrimitiveTypeInfo.Int;
                            object result;

                            switch (meaning) {
                                case OperatorMeaning.Plus:
                                    result = a + b;
                                    break;

                                case OperatorMeaning.Minus:
                                    result = a - b;
                                    break;

                                case OperatorMeaning.Asterisk:
                                    result = a * b;
                                    break;

                                case OperatorMeaning.BackSlash:
                                    result = a / b;
                                    break;

                                case OperatorMeaning.ForwardSlash:
                                    result = (double)a / b;
                                    typeInfo = PrimitiveTypeInfo.Float;
                                    break;

                                case OperatorMeaning.Modulo:
                                    result = a % b;
                                    break;

                                default:
                                    RaiseError("Integer operands don't support this operator", middle);
                                    return null;
                            }
                            return new PrimitiveLiteralExpression(result, typeInfo, left.MainLocation);

                        } else {
                            BinaryOperatorExpression.Option option;
                            switch (meaning) {
                                case OperatorMeaning.Plus:
                                    option = BinaryOperatorExpression.Option.AddInteger;
                                    break;

                                case OperatorMeaning.Minus:
                                    option = BinaryOperatorExpression.Option.SubtractInteger;
                                    break;

                                case OperatorMeaning.Asterisk:
                                    option = BinaryOperatorExpression.Option.MultiplyInteger;
                                    break;

                                case OperatorMeaning.BackSlash:
                                    option = BinaryOperatorExpression.Option.DivideInteger;
                                    break;

                                case OperatorMeaning.ForwardSlash:
                                    option = BinaryOperatorExpression.Option.DivideFloating;
                                    x = ForceConvertExpression(x, PrimitiveTypeInfo.Float, left.MainLocation);
                                    y = ForceConvertExpression(y, PrimitiveTypeInfo.Float, right.MainLocation);
                                    break;

                                case OperatorMeaning.Modulo:
                                    option = BinaryOperatorExpression.Option.Modulo;
                                    break;

                                default:
                                    RaiseError("Integer operands don't support this operator", middle);
                                    return null;
                            }
                            return new BinaryOperatorExpression(option, x, y, middle);
                        }


                    case PrimitiveTypeInfo.Option.Float:
                        if (x.IsCompileTime && y.IsCompileTime) {
                            var a = (double)x.Value;
                            var b = (double)y.Value;
                            var result = 0.0;

                            switch (meaning) {
                                case OperatorMeaning.Plus:
                                    result = a + b;
                                    break;

                                case OperatorMeaning.Minus:
                                    result = a - b;
                                    break;

                                case OperatorMeaning.Asterisk:
                                    result = a * b;
                                    break;

                                case OperatorMeaning.ForwardSlash:
                                    result = a / b;
                                    break;

                                default:
                                    RaiseError("Floating operands don't support this operator", middle);
                                    return null;
                            }
                            return new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float, left.MainLocation);

                        } else {
                            BinaryOperatorExpression.Option option;
                            switch (meaning) {
                                case OperatorMeaning.Plus:
                                    option = BinaryOperatorExpression.Option.AddFloating;
                                    break;

                                case OperatorMeaning.Minus:
                                    option = BinaryOperatorExpression.Option.SubtractFloating;
                                    break;

                                case OperatorMeaning.Asterisk:
                                    option = BinaryOperatorExpression.Option.MultiplyFloating;
                                    break;

                                case OperatorMeaning.ForwardSlash:
                                    option = BinaryOperatorExpression.Option.DivideFloating;
                                    break;

                                default:
                                    RaiseError("Floating operands don't support this operator", middle);
                                    return null;
                            }
                            return new BinaryOperatorExpression(option, x, y, middle);
                        }


                    case PrimitiveTypeInfo.Option.String:
                        if (x.IsCompileTime && y.IsCompileTime) {
                            var a = (string)x.Value;
                            var b = (string)y.Value;
                            var result = "";

                            if (meaning == OperatorMeaning.Plus) {
                                result = a + b;
                            } else {
                                RaiseError("String operands don't support this operator", middle);
                                return null;
                            }
                            return new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.String, left.MainLocation);

                        } else {
                            BinaryOperatorExpression.Option option;
                            if (meaning == OperatorMeaning.Plus) {
                                option = BinaryOperatorExpression.Option.AddString;
                            } else {
                                RaiseError("String operands don't support this operator", middle);
                                return null;
                            }
                            return new BinaryOperatorExpression(option, x, y, middle);
                        }


                    default:
                        RaiseError($"Unsupported type '{primitive.Name}' for this operator", left.MainLocation);
                        return null;
                }

            } else {
                RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", left.MainLocation);
                return null;
            }
        }
    }
}

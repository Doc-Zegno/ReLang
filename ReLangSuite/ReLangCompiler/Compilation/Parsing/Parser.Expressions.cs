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
            IFunctionDefinition definition;

            // Filter built-ins
            switch (name) {
                case "print":
                    definition = BuiltinFunctionDefinition.Print;
                    break;

                default:
                    definition = functionTree.GetFunctionDefinition(name);
                    if (definition == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    }
                    break;
            }

            // Pick all the arguments
            var arguments = GetFunctionArguments();

            // Check them against expected types
            CheckAndConvertFunctionArguments(definition.Signature, arguments, location);

            // return appropriate function call expression
            return new FunctionCallExpression(definition, arguments, false, location);
        }



        private List<IExpression> GetFunctionArguments(OperatorMeaning stop = OperatorMeaning.CloseParenthesis) {
            var arguments = new List<IExpression>();

            if (WhetherOperator(stop)) {
                MoveNextLexeme();
            } else {
                while (true) {
                    arguments.Add(GetExpression());
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }
                CheckOperator(stop);
            }

            return arguments;
        }



        private void CheckAndConvertFunctionArguments(FunctionSignature signature, List<IExpression> arguments, Location location) {
            var names = signature.ArgumentNames;
            var expectedTypes = signature.ArgumentTypes;
            var expectedMutabilities = signature.ArgumentMutabilities;

            if (arguments.Count != expectedTypes.Count) {
                RaiseError($"Wrong number of arguments for this function call (expected {expectedTypes.Count}"
                           + $" but got {arguments.Count})", location);
            }

            for (var index = 0; index < arguments.Count; index++) {
                var name = names[index];
                var argument = arguments[index];
                var expectedType = expectedTypes[index];

                // Type checks (and conversions if necessary)
                var converted = TryConvertExpression(argument, expectedType);
                if (converted == null) {
                    RaiseError($"Cannot convert expression to type of argument '{name}' (expected '{expectedType.Name}'"
                               + $" but got '{argument.TypeInfo.Name}')", argument.MainLocation);
                }

                // Mutability check
                CheckMutability(converted, expectedMutabilities[index]);

                arguments[index] = converted;
            }
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
                    case OperatorMeaning.Dot:
                        // Access to the field
                        expression = GetMemberAccessExpression(expression);
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
                
                CheckAndConvertFunctionArguments(definition.Signature, arguments, location);
                return new FunctionCallExpression(definition, arguments, true, location);
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
            CheckAndConvertFunctionArguments(definition.Signature, arguments, locationBracket);
            return new FunctionCallExpression(definition, arguments, false, locationBracket);
        }



        // .append(d)
        private IExpression GetMemberAccessExpression(IExpression self) {
            CheckOperator(OperatorMeaning.Dot);
            var location = currentLexeme.StartLocation;
            var name = GetSymbolText("Member's name");

            var arguments = new List<IExpression> { self };
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
                arguments.AddRange(GetFunctionArguments());

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
            CheckAndConvertFunctionArguments(definition.Signature, arguments, location);

            return new FunctionCallExpression(definition, arguments, isLvalue, location);
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
                    if (WhetherOperator(OperatorMeaning.OpenParenthesis)) {
                        return GetConstructorOrFunctionCall(symbol.Text, location);

                    } else {
                        var definition = scopeStack.GetDefinition(symbol.Text);
                        if (definition == null) {
                            RaiseError($"Undeclared identifier '{symbol.Text}'");
                        }
                        
                        if (!definition.IsMutable && definition.Value != null && definition.Value.IsCompileTime) {
                            // Can be evaluated at compile-time
                            return definition.Value;

                            //var value = definition.Value;
                            //return new PrimitiveLiteralExpression(value.Value, value.TypeInfo);
                        } else {
                            // Should be resolved at run-time
                            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                            return new VariableExpression(symbol.Text, definition.Number, frameOffset,
                                                          false, definition.TypeInfo, location);
                        }
                    }

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

                default:
                    RaiseError("Expression was expected");
                    break;
            }

            // Sanity check
            return null;
        }



        private IExpression GetConstructorOrFunctionCall(string name, Location locationName) {
            ITypeInfo targetType = null;
            switch (name) {
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
                    targetType = new ArrayListTypeInfo(PrimitiveTypeInfo.Object);  // No matter what type it is
                    break;

                case "Set":
                    targetType = new HashSetTypeInfo(PrimitiveTypeInfo.Object);  // Doesn't matter
                    break;

                case "Dictionary":
                    targetType = new DictionaryTypeInfo(PrimitiveTypeInfo.Object, PrimitiveTypeInfo.Object);  // Doesn't matter
                    break;

                default:
                    return GetFunctionCall(name, locationName);
            }

            CheckOperator(OperatorMeaning.OpenParenthesis);
            var locationValue = currentLexeme.StartLocation;
            var value = GetExpression();
            CheckOperator(OperatorMeaning.CloseParenthesis);

            return ForceConstructFrom(value, targetType, locationValue);
        }



        // [a, b, c, d, e]
        private IExpression GetListLiteral(Location location) {
            var item = GetExpression();
            var items = new List<IExpression> { item };
            items.AddRange(GetItemList(item.TypeInfo));
            CheckOperator(OperatorMeaning.CloseBracket);
            return new ListLiteralExpression(items, item.TypeInfo, location);
        }



        // {a, b, c, d, e}
        private IExpression GetSetOrDictionaryLiteral(Location location) {
            // Determine whether this is set or dictionary
            var key = GetExpression();

            if (WhetherOperator(OperatorMeaning.Colon)) {
                // Dictionary
                MoveNextLexeme();
                var value = GetExpression();

                var pairs = new List<(IExpression, IExpression)> { (key, value) };
                pairs.AddRange(GetPairList(key.TypeInfo, value.TypeInfo));
                CheckOperator(OperatorMeaning.CloseBrace);
                return new DictionaryLiteralExpression(pairs, key.TypeInfo, value.TypeInfo, location);

            } else {
                // Set
                var items = new List<IExpression> { key };
                items.AddRange(GetItemList(key.TypeInfo));
                CheckOperator(OperatorMeaning.CloseBrace);
                return new SetLiteralExpression(items, key.TypeInfo, location);
            }
        }



        private List<(IExpression, IExpression)> GetPairList(ITypeInfo keyType, ITypeInfo valueType) {
            var pairs = new List<(IExpression, IExpression)>();
            while (WhetherOperator(OperatorMeaning.Comma)) {
                MoveNextLexeme();

                // Get key and check type
                var locationKey = currentLexeme.StartLocation;
                var key = GetExpression();
                if (!keyType.Equals(key.TypeInfo)) {
                    RaiseError($"Key's type mismatch (expected '{keyType.Name}' but got '{key.TypeInfo.Name}')", locationKey);
                }

                // Swallow colon
                CheckOperator(OperatorMeaning.Colon);

                // Get value and check type
                var locationValue = currentLexeme.StartLocation;
                var value = GetExpression();
                if (!valueType.Equals(value.TypeInfo)) {
                    RaiseError($"Value's type mismatch (expected '{valueType.Name}' but got '{value.TypeInfo.Name}')", locationValue);
                }

                pairs.Add((key, value));
            }
            return pairs;
        }



        private List<IExpression> GetItemList(ITypeInfo itemType) {
            var items = new List<IExpression>();
            while (WhetherOperator(OperatorMeaning.Comma)) {
                MoveNextLexeme();

                // Get item and check type
                var location = currentLexeme.StartLocation;
                var item = GetExpression();
                if (!itemType.Equals(item.TypeInfo)) {
                    RaiseError($"Item's type mismatch (expected '{itemType.Name}' but got '{item.TypeInfo.Name}')", location);
                }

                items.Add(item);
            }
            return items;
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
                    CheckAndConvertFunctionArguments(definition.Signature, arguments, locationMiddle);
                    left = new FunctionCallExpression(definition, arguments, false, locationMiddle);

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
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var right = GetProductExpression();

                    // Try to convert to each other
                    var (x, y) = CrossConvert(left, right, locationLeft);

                    if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                        switch (primitive.TypeOption) {
                            case PrimitiveTypeInfo.Option.Int:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (int)x.Value;
                                    var b = (int)y.Value;
                                    var result = 0;

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        result = a - b;
                                    } else {
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Int, locationLeft);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddInteger;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        option = BinaryOperatorExpression.Option.SubtractInteger;
                                    } else {
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                                }
                                break;


                            case PrimitiveTypeInfo.Option.Float:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (double)x.Value;
                                    var b = (double)y.Value;
                                    var result = 0.0;

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        result = a - b;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float, locationLeft);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddFloating;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        option = BinaryOperatorExpression.Option.SubtractFloating;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                                }
                                break;


                            case PrimitiveTypeInfo.Option.String:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (string)x.Value;
                                    var b = (string)y.Value;
                                    var result = "";

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.String, locationLeft);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddString;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y, locationMiddle);
                                }
                                break;


                            default:
                                RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                                break;
                        }
                    } else {
                        RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
                    }

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
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetNegateExpression();

                    // Try to convert to each other
                    var (x, y) = CrossConvert(left, right, locationLeft);

                    if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                        switch (primitive.TypeOption) {
                            case PrimitiveTypeInfo.Option.Int:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (int)x.Value;
                                    var b = (int)y.Value;
                                    var typeInfo = PrimitiveTypeInfo.Int;
                                    object result;

                                    switch (meaning) {
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
                                            RaiseError("Integer operands don't support this operator", locationMiddle);
                                            return null;
                                    }

                                    left = new PrimitiveLiteralExpression(result, typeInfo, locationLeft);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    switch (meaning) {
                                        case OperatorMeaning.Asterisk:
                                            option = BinaryOperatorExpression.Option.MultiplyInteger;
                                            break;

                                        case OperatorMeaning.BackSlash:
                                            option = BinaryOperatorExpression.Option.DivideInteger;
                                            break;

                                        case OperatorMeaning.ForwardSlash:
                                            option = BinaryOperatorExpression.Option.DivideFloating;
                                            x = ForceConvertExpression(x, PrimitiveTypeInfo.Float, locationLeft);
                                            y = ForceConvertExpression(y, PrimitiveTypeInfo.Float, locationRight);
                                            break;

                                        case OperatorMeaning.Modulo:
                                            option = BinaryOperatorExpression.Option.Modulo;
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
                                    var a = (double)x.Value;
                                    var b = (double)y.Value;
                                    var result = 0.0;

                                    if (meaning == OperatorMeaning.Asterisk) {
                                        result = a * b;
                                    } else if (meaning == OperatorMeaning.ForwardSlash) {
                                        result = a / b;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }

                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float, locationLeft);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Asterisk) {
                                        option = BinaryOperatorExpression.Option.MultiplyFloating;
                                    } else if (meaning == OperatorMeaning.ForwardSlash) {
                                        option = BinaryOperatorExpression.Option.DivideFloating;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
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
                        RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
                    }

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
    }
}

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
            FunctionDefinition definition;
            BuiltinFunctionCallExpression.Option? builtinOption = null;

            // Filter built-ins
            switch (name) {
                case "print":
                    definition = Builtins.PrintDefinition;
                    builtinOption = BuiltinFunctionCallExpression.Option.Print;
                    break;

                default:
                    var maybe = functionTree.GetFunctionDefinition(name);
                    if (maybe == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    } else {
                        definition = maybe.Value;
                    }
                    break;
            }

            // Pick all the arguments
            var arguments = new List<IExpression>();
            var expectedTypes = definition.ArgumentTypes;

            if (WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                MoveNextLexeme();
            } else {
                for (var index = 0; currentLexeme != null; index++) {
                    var loc = currentLexeme.StartLocation;
                    var argument = GetExpression();

                    // Type checks (and conversions if necessary)
                    if (index + 1 > expectedTypes.Count) {
                        RaiseError($"Too many arguments for this function call (expected {expectedTypes.Count})", loc);
                        return null;
                    } else {
                        var expectedType = expectedTypes[index];
                        var converted = TryConvertExpression(argument, expectedType);
                        if (converted == null) {
                            RaiseError($"Cannot convert given argument to target type (expected '{expectedType.Name}'"
                                       + $" but got '{argument.TypeInfo.Name}')", loc);
                            return null;
                        } else {
                            argument = converted;
                        }
                    }

                    arguments.Add(argument);
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }

                CheckOperator(OperatorMeaning.CloseParenthesis);
            }

            // Final count check
            if (arguments.Count != expectedTypes.Count) {
                RaiseError($"Wrong number of arguments for this function call (expected {expectedTypes.Count}"
                           + $" but got {arguments.Count})", location);
                return null;
            }

            // return appropriate function call expression
            if (builtinOption != null) {
                return new BuiltinFunctionCallExpression(definition.ResultType, arguments, builtinOption.Value);
            } else {
                return new CustomFunctionCallExpression(definition.ResultType, arguments, definition.Number);
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
            return GetOrExpression();
        }



        // variable, f(x, y, z), (a + b), [a, b, c], {a, b, c}, literal
        private IExpression GetAtomicExpression() {
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
                            return GetListLiteral();

                        case OperatorMeaning.OpenBrace:
                            MoveNextLexeme();
                            return GetSetOrDictionaryLiteral();

                        default:
                            RaiseError($"Unexpected operator: {GetOperatorName(operatorLexeme.Meaning)}");
                            return null;
                    }

                case SymbolLexeme symbol:
                    MoveNextLexeme();
                    if (WhetherOperator(OperatorMeaning.OpenParenthesis)) {
                        return GetConstructorOrFunctionCall(symbol.Text, location);

                    } else {
                        var maybe = scopeStack.GetDefinition(symbol.Text);
                        if (!maybe.HasValue) {
                            RaiseError($"Undeclared identifier '{symbol.Text}'");
                        }

                        var definition = maybe.Value;
                        if (!definition.IsMutable && definition.Value != null && definition.Value.IsCompileTime) {
                            // Can be evaluated at compile-time
                            return definition.Value;

                            //var value = definition.Value;
                            //return new PrimitiveLiteralExpression(value.Value, value.TypeInfo);
                        } else {
                            // Should be resolved at run-time
                            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                            return new VariableExpression(symbol.Text, definition.Number,
                                                          frameOffset, false, definition.TypeInfo);
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
                    return new PrimitiveLiteralExpression(literal.Value, new PrimitiveTypeInfo(typeOption));

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
        private IExpression GetListLiteral() {
            var item = GetExpression();
            var items = new List<IExpression> { item };
            items.AddRange(GetItemList(item.TypeInfo));
            CheckOperator(OperatorMeaning.CloseBracket);
            return new ListLiteralExpression(items, item.TypeInfo);
        }



        // {a, b, c, d, e}
        private IExpression GetSetOrDictionaryLiteral() {
            // Determine whether this is set or dictionary
            var key = GetExpression();

            if (WhetherOperator(OperatorMeaning.Colon)) {
                // Dictionary
                MoveNextLexeme();
                var value = GetExpression();

                var pairs = new List<(IExpression, IExpression)> { (key, value) };
                pairs.AddRange(GetPairList(key.TypeInfo, value.TypeInfo));
                CheckOperator(OperatorMeaning.CloseBrace);
                return new DictionaryLiteralExpression(pairs, key.TypeInfo, value.TypeInfo);

            } else {
                // Set
                var items = new List<IExpression> { key };
                items.AddRange(GetItemList(key.TypeInfo));
                CheckOperator(OperatorMeaning.CloseBrace);
                return new SetLiteralExpression(items, key.TypeInfo);
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
                            left = new PrimitiveLiteralExpression(true, PrimitiveTypeInfo.Bool);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.Or, x, y);
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
                            left = new PrimitiveLiteralExpression(false, PrimitiveTypeInfo.Bool);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.And, x, y);
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
                        break;

                    default:
                        return left;
                }

                // Get right operand
                MoveNextLexeme();
                var right = GetRangeExpression();

                // Try to convert to each other
                var (x, y) = CrossConvert(left, right, locationLeft);

                if (x.TypeInfo is PrimitiveTypeInfo primitive) {
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
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

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
                                left = new BinaryOperatorExpression(option, x, y);
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
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

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
                                left = new BinaryOperatorExpression(option, x, y);
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
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

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
                                left = new BinaryOperatorExpression(option, x, y);
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
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

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
                                left = new BinaryOperatorExpression(option, x, y);
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
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

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
                                left = new BinaryOperatorExpression(option, x, y);
                            }
                            break;


                        default:
                            RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                            return null;
                    }

                } else {
                    RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
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
                        RaiseError($"Empty range: start index (got {a}) must be smaller than end one (got {b})", locationLeft);
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
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Int);

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
                                    left = new BinaryOperatorExpression(option, x, y);
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
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float);

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
                                    left = new BinaryOperatorExpression(option, x, y);
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
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.String);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddString;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
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

                                    left = new PrimitiveLiteralExpression(result, typeInfo);

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
                                    left = new BinaryOperatorExpression(option, x, y);
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

                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float);

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
                                    left = new BinaryOperatorExpression(option, x, y);
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
                var meaning = operatorLexeme.Meaning;
                switch (meaning) {
                    case OperatorMeaning.Not:
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
                    case OperatorMeaning.Not:
                        converted = TryConvertExpression(atomic, PrimitiveTypeInfo.Bool);
                        if (converted != null) {
                            if (converted.IsCompileTime) {
                                var a = (bool)converted.Value;
                                return new PrimitiveLiteralExpression(!a, PrimitiveTypeInfo.Bool);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.Not, converted);
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
                                return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Int);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateInteger, converted);
                            }

                        } else {
                            converted = TryConvertExpression(atomic, PrimitiveTypeInfo.Float);
                            if (converted != null) {
                                if (converted.IsCompileTime) {
                                    var a = (double)converted.Value;
                                    return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Float);
                                } else {
                                    return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateFloating, converted);
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

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta information about primitive types
    /// </summary>
    class PrimitiveTypeInfo : ITypeInfo {
        /// <summary>
        /// Supported primitive types
        /// </summary>
        public enum Option {
            Void,
            Bool,
            Char,
            Int,
            Float,
            String,
            Object,
        }


        public Option TypeOption { get; }
        public string Name => TypeOption.ToString();
        public bool IsReferential { get; }
        public bool IsComplete => true;


        public PrimitiveTypeInfo(Option typeOption) {
            TypeOption = typeOption;

            switch (typeOption) {
                case Option.String:
                case Option.Object:
                    IsReferential = true;
                    break;

                default:
                    IsReferential = false;
                    break;
            }
        }


        public IExpression GetDefaultValue(Location location) {
            switch (TypeOption) {
                case Option.Bool:
                    return new PrimitiveLiteralExpression(false, Bool, location);

                case Option.Char:
                    return new PrimitiveLiteralExpression((char)0, Char, location);

                case Option.Int:
                    return new PrimitiveLiteralExpression(0, Int, location);

                case Option.Float:
                    return new PrimitiveLiteralExpression(0.0, Float, location);

                case Option.String:
                    return new PrimitiveLiteralExpression("", String, location);

                default:
                    return null;
            }
        }


        public ITypeInfo ResolveGeneric() => this;


        public bool CanUpcast(ITypeInfo sourceType) {
            if (TypeOption == Option.Object) {
                if (sourceType is NullTypeInfo || sourceType is MaybeTypeInfo
                    || sourceType is PrimitiveTypeInfo primitive && primitive.TypeOption == Option.Void)
                {
                    return false;
                } else {
                    return true;
                }
            } else {
                return Equals(sourceType);
            }
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (CanUpcast(expression.TypeInfo)) {
                return expression.ChangeType(this);

            } else if (expression.TypeInfo is PrimitiveTypeInfo primitiveSource) {
                switch (primitiveSource.TypeOption) {
                    case Option.Char:
                        if (TypeOption == Option.Int) {
                            if (expression.IsCompileTime) {
                                var character = (char)expression.Value;
                                return new PrimitiveLiteralExpression((int)character, Int, expression.MainLocation);
                            } else {
                                return new ConversionExpression(ConversionExpression.Option.Char2Int, expression, expression.MainLocation);
                            }
                        } else {
                            return null;
                        }

                    case Option.Int:
                        if (TypeOption == Option.Float) {
                            if (expression.IsCompileTime) {
                                var integer = (int)expression.Value;
                                return new PrimitiveLiteralExpression((double)integer, Float, expression.MainLocation);
                            } else {
                                return new ConversionExpression(ConversionExpression.Option.Int2Float, expression, expression.MainLocation);
                            }
                        } else {
                            return null;
                        }

                    default:
                        return null;
                }

            } else {
                return null;
            }
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            var implicitConversion = ConvertFrom(expression);
            if (implicitConversion != null) {
                return implicitConversion;

            } else if (expression.TypeInfo is PrimitiveTypeInfo primitiveType) {
                switch (primitiveType.TypeOption) {
                    case Option.Bool:
                        switch (TypeOption) {
                            case Option.String:
                                if (expression.IsCompileTime) {
                                    var boolean = (bool)expression.Value;
                                    return new PrimitiveLiteralExpression(boolean ? "true" : "false", String, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Bool2String, expression, location);
                                }

                            default:
                                return null;
                        }


                    case Option.Char:
                        switch (TypeOption) {
                            case Option.String:
                                if (expression.IsCompileTime) {
                                    var character = (char)expression.Value;
                                    return new PrimitiveLiteralExpression(new string(character, 1), String, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Char2String, expression, location);
                                }

                            default:
                                return null;
                        }


                    case Option.Int:
                        switch (TypeOption) {
                            case Option.Char:
                                if (expression.IsCompileTime) {
                                    var integer = (int)expression.Value;
                                    if (integer >= 0 && integer <= char.MaxValue) {
                                        return new PrimitiveLiteralExpression((char)integer, Char, location);
                                    } else {
                                        throw new FormatException($"Value {integer} cannot be converted to valid character code");
                                    }
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Int2Char, expression, location);
                                }

                            case Option.String:
                                if (expression.IsCompileTime) {
                                    var integer = (int)expression.Value;
                                    return new PrimitiveLiteralExpression(integer.ToString(), String, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Int2String, expression, location);
                                }

                            default:
                                return null;
                        }


                    case Option.Float:
                        switch (TypeOption) {
                            case Option.Int:
                                if (expression.IsCompileTime) {
                                    var floating = (double)expression.Value;
                                    return new PrimitiveLiteralExpression((int)floating, Int, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Float2Int, expression, location);
                                }

                            case Option.String:
                                if (expression.IsCompileTime) {
                                    var floating = (double)expression.Value;
                                    return new PrimitiveLiteralExpression(floating.ToString(new CultureInfo("en-US")), String, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Float2String, expression, location);
                                }

                            default:
                                return null;
                        }


                    case Option.String:
                        switch (TypeOption) {
                            case Option.Bool:
                                if (expression.IsCompileTime) {
                                    var s = (string)expression.Value;
                                    var boolean = false;
                                    if (s == "true") {
                                        boolean = true;
                                    } else if (s == "false") {
                                        boolean = false;
                                    } else {
                                        throw new FormatException($"Cannot convert \"{s}\" to boolean");
                                    }
                                    return new PrimitiveLiteralExpression(boolean, Bool, location);
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.String2Bool, expression, location);
                                }

                            case Option.Int:
                                if (expression.IsCompileTime) {
                                    var s = (string)expression.Value;
                                    if (int.TryParse(s, out int integer)) {
                                        return new PrimitiveLiteralExpression(integer, Int, location);
                                    } else {
                                        throw new FormatException($"Cannot convert \"{s}\" to integer");
                                    }
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.String2Int, expression, location);
                                }

                            case Option.Float:
                                if (expression.IsCompileTime) {
                                    var s = (string)expression.Value;
                                    if (double.TryParse(s, NumberStyles.Number, new CultureInfo("en-US"), out double floating)) {
                                        return new PrimitiveLiteralExpression(floating, Float, location);
                                    } else {
                                        throw new FormatException($"Cannot convert \"{s}\" to floating");
                                    }
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.String2Float, expression, location);
                                }

                            default:
                                return null;
                        }


                    default:
                        return null;
                }

            } else {
                return null;
            }
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (TypeOption) {
                case Option.String:
                    switch (name) {
                        case "init":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringInit,
                                new List<string> { "count", "value" },
                                new List<ITypeInfo> { Int, Char },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                this);

                        case "get":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringGet,
                                new List<string> { "self", "index" },
                                new List<ITypeInfo> { this, Int },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                Char);

                        case "getLength":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringGetLength,
                                new List<string> { "self" },
                                new List<ITypeInfo> { this },
                                new List<bool> { false },
                                new List<IExpression> { null },
                                Int);

                        case "getSlice":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringGetSlice,
                                new List<string> { "self", "start", "end", "step" },
                                new List<ITypeInfo> { this, Int, new MaybeTypeInfo(Int), Int },
                                new List<bool> { isSelfMutable, false, false, false },
                                new List<IExpression> { null, null, null, null },
                                String,
                                isSelfMutable);

                        case "toLower":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringToLower,
                                new List<string> { "self" },
                                new List<ITypeInfo> { this },
                                new List<bool> { false },
                                new List<IExpression> { null },
                                String);

                        case "toUpper":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringToUpper,
                                new List<string> { "self" },
                                new List<ITypeInfo> { this },
                                new List<bool> { false },
                                new List<IExpression> { null },
                                String);

                        case "split":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringSplit,
                                new List<string> { "self" },
                                new List<ITypeInfo> { this },
                                new List<bool> { false },
                                new List<IExpression> { null },  // TODO: add spliting by string
                                new ArrayListTypeInfo(String));

                        case "contains":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringContains,
                                new List<string> { "self", "substring" },
                                new List<ITypeInfo> { this, this },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                Bool);

                        case "join":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringJoin,
                                new List<string> { "self", "items" },
                                new List<ITypeInfo> { this, new IterableTypeInfo(Object) },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                String);

                        case "reversed":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringReversed,
                                new List<string> { "self" },
                                new List<ITypeInfo> { this },
                                new List<bool> { false },
                                new List<IExpression> { null },
                                String);

                        case "find":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringFind,
                                new List<string> { "self", "substring" },
                                new List<ITypeInfo> { this, this },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                new MaybeTypeInfo(Int));

                        case "findLast":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringFindLast,
                                new List<string> { "self", "substring" },
                                new List<ITypeInfo> { this, this },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                new MaybeTypeInfo(Int));

                        case "startsWith":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringStartsWith,
                                new List<string> { "self", "prefix" },
                                new List<ITypeInfo> { this, this },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                Bool);

                        case "endsWith":
                            return new BuiltinFunctionDefinition(
                                name,
                                BuiltinFunctionDefinition.Option.StringEndsWith,
                                new List<string> { "self", "suffix" },
                                new List<ITypeInfo> { this, this },
                                new List<bool> { false, false },
                                new List<IExpression> { null, null },
                                Bool);

                        default:
                            return null;
                    }

                default:
                    return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo || obj is PrimitiveTypeInfo primitiveType && TypeOption == primitiveType.TypeOption) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -543757662;
            hashCode = hashCode * -1521134295 + TypeOption.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }


        public static PrimitiveTypeInfo Void => new PrimitiveTypeInfo(Option.Void);
        public static PrimitiveTypeInfo Bool => new PrimitiveTypeInfo(Option.Bool);
        public static PrimitiveTypeInfo Char => new PrimitiveTypeInfo(Option.Char);
        public static PrimitiveTypeInfo Int => new PrimitiveTypeInfo(Option.Int);
        public static PrimitiveTypeInfo Float => new PrimitiveTypeInfo(Option.Float);
        public static PrimitiveTypeInfo String => new PrimitiveTypeInfo(Option.String);
        public static PrimitiveTypeInfo Object => new PrimitiveTypeInfo(Option.Object);
    }
}

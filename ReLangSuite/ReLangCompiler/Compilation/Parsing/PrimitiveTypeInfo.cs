using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
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
            Int,
            Float,
            String,
            Object,
        }


        public Option TypeOption { get; }
        public string Name => TypeOption.ToString();


        public PrimitiveTypeInfo(Option typeOption) {
            TypeOption = typeOption;
        }


        public IExpression ConvertTo(IExpression expression, ITypeInfo targetTypeInfo) {
            if (targetTypeInfo is PrimitiveTypeInfo primitiveTarget) {
                if (primitiveTarget.TypeOption == TypeOption || primitiveTarget.TypeOption == Option.Object) {
                    // Identity conversion or trivial conversion
                    return expression;

                } else {
                    switch (TypeOption) {
                        case Option.Int:
                            if (primitiveTarget.TypeOption == Option.Float) {
                                if (expression.IsCompileTime) {
                                    var integer = (int)expression.Value;
                                    return new PrimitiveLiteralExpression((double)integer, new PrimitiveTypeInfo(Option.Float));
                                } else {
                                    return new ConversionExpression(ConversionExpression.Option.Int2Float, expression);
                                }
                                //return new ConversionExpression(ConversionExpression.Option.Int2Float, expression);
                            } else {
                                return null;
                            }

                        default:
                            return null;
                    }
                } 
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is PrimitiveTypeInfo primitiveType && TypeOption == primitiveType.TypeOption) {
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
        public static PrimitiveTypeInfo Int => new PrimitiveTypeInfo(Option.Int);
        public static PrimitiveTypeInfo Float => new PrimitiveTypeInfo(Option.Float);
        public static PrimitiveTypeInfo String => new PrimitiveTypeInfo(Option.String);
        public static PrimitiveTypeInfo Object => new PrimitiveTypeInfo(Option.Object);
    }
}

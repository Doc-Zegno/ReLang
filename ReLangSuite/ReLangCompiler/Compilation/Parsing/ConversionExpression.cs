using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Expression representing a conversion from one type to another
    /// </summary>
    class ConversionExpression : IExpression {
        /// <summary>
        /// Possible conversion
        /// </summary>
        public enum Option {
            Int2Float,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        public Option ConversionOption { get; }
        public IExpression Operand { get; }


        public ConversionExpression(Option conversionOption, IExpression operand) {
            ConversionOption = conversionOption;
            Operand = operand;

            switch (ConversionOption) {
                case Option.Int2Float:
                    TypeInfo = new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Float);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing unary operator
    /// </summary>
    class UnaryOperatorExpression : IOperatorExpression {
        /// <summary>
        /// Possible unary operators
        /// </summary>
        public enum Option {
            NegateInteger,
            NegateFloating,
            Not,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public Option OperatorOption { get; }
        public IExpression Expression { get; }


        public UnaryOperatorExpression(Option operatorOption, IExpression expression, Location mainLocation) {
            OperatorOption = operatorOption;
            Expression = expression;

            TypeInfo = expression.TypeInfo;
            MainLocation = mainLocation;
        }
    }
}

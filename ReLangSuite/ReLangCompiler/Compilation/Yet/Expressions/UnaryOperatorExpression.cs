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
            FromMaybe,
            TestNull,
            TestNotNull,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public Option OperatorOption { get; }
        public IExpression Expression { get; }


        public UnaryOperatorExpression(Option operatorOption, IExpression expression, ITypeInfo resultType, Location mainLocation) {
            OperatorOption = operatorOption;
            Expression = expression;

            TypeInfo = resultType;
            MainLocation = mainLocation;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (UnaryOperatorExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

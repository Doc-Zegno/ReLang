using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Expression representing binary operator
    /// </summary>
    class BinaryOperatorExpression : IOperatorExpression {
        /// <summary>
        /// Possible binary operators
        /// </summary>
        public enum Option {
            AddInteger,
            AddFloating,
            AddString,
            SubtractInteger,
            SubtractFloating,
            MultiplyInteger,
            MultiplyFloating,
            DivideInteger,
            DivideFloating,
            And,
            Or,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        public Option OperatorOption { get; }
        public IExpression LeftOperand { get; }
        public IExpression RightOperang { get; }


        public BinaryOperatorExpression(Option operatorOption, IExpression leftOperand, IExpression rightOperand) {
            OperatorOption = operatorOption;
            LeftOperand = leftOperand;
            RightOperang = rightOperand;

            TypeInfo = leftOperand.TypeInfo;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
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

            Modulo,

            And,
            Or,

            EqualBoolean,
            EqualInteger,
            EqualFloating,
            EqualString,
            EqualObject,

            NotEqualBoolean,
            NotEqualInteger,
            NotEqualFloating,
            NotEqualString,
            NotEqualObject,

            LessInteger,
            LessFloating,

            LessOrEqualInteger,
            LessOrEqualFloating,

            MoreInteger,
            MoreFloating,

            MoreOrEqualInteger,
            MoreOrEqualFloating,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public Option OperatorOption { get; }
        public IExpression LeftOperand { get; }
        public IExpression RightOperang { get; }


        public BinaryOperatorExpression(Option operatorOption, IExpression leftOperand, IExpression rightOperand, Location mainLocation) {
            OperatorOption = operatorOption;
            LeftOperand = leftOperand;
            RightOperang = rightOperand;

            if ((int)operatorOption >= (int)Option.EqualBoolean && (int)operatorOption <= (int)Option.MoreOrEqualFloating) {
                TypeInfo = PrimitiveTypeInfo.Bool;
            } else {
                TypeInfo = leftOperand.TypeInfo;
            }
            MainLocation = mainLocation;
        }
    }
}

﻿using System;
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
            AddList,

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

            ValueOrDefault,
        }


        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
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
            } else if (operatorOption == Option.ValueOrDefault) {
                TypeInfo = rightOperand.TypeInfo;
            } else {
                TypeInfo = leftOperand.TypeInfo;
            }
            MainLocation = mainLocation;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (BinaryOperatorExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

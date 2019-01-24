﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a conversion from one type to another
    /// </summary>
    class ConversionExpression : IExpression {
        /// <summary>
        /// Possible conversion
        /// </summary>
        public enum Option {
            Int2Float,
            Float2Int,
            Bool2String,
            Int2String,
            Float2String,
            String2Int,
            String2Float,
            String2Bool,
            Iterable2List,
            Iterable2Set,
            Iterable2Dictionary,
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

            if (operand.TypeInfo is IIterableTypeInfo iterableType) {
                var itemType = iterableType.ItemType;

                switch (conversionOption) {
                    case Option.Iterable2List:
                        TypeInfo = new ArrayListTypeInfo(itemType);
                        break;

                    case Option.Iterable2Set:
                        TypeInfo = new HashSetTypeInfo(itemType);
                        break;

                    case Option.Iterable2Dictionary:
                        var tupleType = (TupleTypeInfo)itemType;
                        TypeInfo = new DictionaryTypeInfo(tupleType.ItemTypes[0], tupleType.ItemTypes[1]);
                        break;

                    default:
                        throw new NotImplementedException();
                }

            } else {
                switch (ConversionOption) {
                    case Option.Int2Float:
                        TypeInfo = PrimitiveTypeInfo.Float;
                        break;

                    case Option.Float2Int:
                        TypeInfo = PrimitiveTypeInfo.Int;
                        break;

                    case Option.Bool2String:
                        TypeInfo = PrimitiveTypeInfo.String;
                        break;

                    case Option.Int2String:
                        TypeInfo = PrimitiveTypeInfo.String;
                        break;

                    case Option.Float2String:
                        TypeInfo = PrimitiveTypeInfo.String;
                        break;

                    case Option.String2Int:
                        TypeInfo = PrimitiveTypeInfo.Int;
                        break;

                    case Option.String2Float:
                        TypeInfo = PrimitiveTypeInfo.Float;
                        break;

                    case Option.String2Bool:
                        TypeInfo = PrimitiveTypeInfo.Bool;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            } 
        }
    }
}

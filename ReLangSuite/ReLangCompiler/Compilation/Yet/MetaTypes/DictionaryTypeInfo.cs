using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Information about dictionary type
    /// </summary>
    class DictionaryTypeInfo : IterableTypeInfo {
        public override string Name => $"{{{KeyType.Name}: {ValueType.Name}}}";

        public ITypeInfo KeyType { get; }
        public ITypeInfo ValueType { get; }


        public DictionaryTypeInfo(ITypeInfo keyType, ITypeInfo valueType)
            : base(new TupleTypeInfo(new List<ITypeInfo> { keyType, valueType }))
        {
            KeyType = keyType;
            ValueType = valueType;
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override IExpression ConstructFrom(IExpression expression) {
            if (expression.TypeInfo is IterableTypeInfo iterableType
                && iterableType.ItemType is TupleTypeInfo tupleType
                && tupleType.ItemTypes.Count == 2) {
                return new ConversionExpression(ConversionExpression.Option.Iterable2Dictionary, expression);
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is DictionaryTypeInfo dictionaryType
                && KeyType.Equals(dictionaryType.KeyType)
                && ValueType.Equals(dictionaryType.ValueType))
            {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -304684678;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(KeyType);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ValueType);
            return hashCode;
        }


        public override IFunctionDefinition GetMethodDefinition(string name) {
            switch (name) {
                case "lengthGet":
                    return new BuiltinFunctionDefinition(
                        BuiltinFunctionDefinition.Option.DictionaryLengthGet,
                        new List<ITypeInfo> { },
                        PrimitiveTypeInfo.Int);

                default:
                    return base.GetMethodDefinition(name);
            }
        }
    }
}

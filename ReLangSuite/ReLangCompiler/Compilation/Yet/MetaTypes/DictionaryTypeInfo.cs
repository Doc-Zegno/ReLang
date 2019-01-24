using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Information about dictionary type
    /// </summary>
    class DictionaryTypeInfo : IIterableTypeInfo {
        public ITypeInfo ItemType { get; }
        public string Name => $"{{{KeyType.Name}: {ValueType.Name}}}";

        public ITypeInfo KeyType { get; }
        public ITypeInfo ValueType { get; }


        public DictionaryTypeInfo(ITypeInfo keyType, ITypeInfo valueType) {
            KeyType = keyType;
            ValueType = valueType;

            ItemType = new TupleTypeInfo(new List<ITypeInfo> { KeyType, ValueType });
        }


        public IExpression ConvertTo(IExpression expression, ITypeInfo targetTypeInfo) {
            if (Equals(targetTypeInfo)) {
                return expression;
            } else {
                switch (targetTypeInfo) {
                    case PrimitiveTypeInfo primitiveType when primitiveType.TypeOption == PrimitiveTypeInfo.Option.Object:
                    case IIterableTypeInfo iterableType when ItemType.Equals(iterableType.ItemType):
                        return expression;

                    default:
                        return null;
                }
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
    }
}

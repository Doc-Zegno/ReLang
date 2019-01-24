using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about set
    /// </summary>
    class HashSetTypeInfo : IIterableTypeInfo {
        public ITypeInfo ItemType { get; }
        public string Name => $"{{{ItemType.Name}}}";


        public HashSetTypeInfo(ITypeInfo itemType) {
            ItemType = itemType;
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
            if (obj is HashSetTypeInfo setType && ItemType.Equals(setType.ItemType)) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -1109124596;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }


        public IExpression ConstructFrom(IExpression expression) {
            if (expression.TypeInfo is IIterableTypeInfo iterableType) {
                return new ConversionExpression(ConversionExpression.Option.Iterable2Set, expression);
            } else {
                return null;
            }
        }
    }
}

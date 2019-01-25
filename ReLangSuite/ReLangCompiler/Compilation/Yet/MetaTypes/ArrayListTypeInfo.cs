using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about ArrayList
    /// </summary>
    class ArrayListTypeInfo : IterableTypeInfo {
        public override string Name => $"[{ItemType.Name}]";


        public ArrayListTypeInfo(ITypeInfo itemType) : base(itemType) {
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override IExpression ConstructFrom(IExpression expression) {
            switch (expression.TypeInfo) {
                case IterableTypeInfo iterable:
                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    return new ConversionExpression(ConversionExpression.Option.Iterable2List, expression);

                default:
                    return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is ArrayListTypeInfo arrayListType && ItemType.Equals(arrayListType.ItemType)) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -120175732;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            return hashCode;
        }
    }
}

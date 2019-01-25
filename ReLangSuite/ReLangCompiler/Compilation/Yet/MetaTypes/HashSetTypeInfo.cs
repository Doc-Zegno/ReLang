using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about set
    /// </summary>
    class HashSetTypeInfo : IterableTypeInfo {
        public override string Name => $"{{{ItemType.Name}}}";


        public HashSetTypeInfo(ITypeInfo itemType) : base(itemType) {
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override IExpression ConstructFrom(IExpression expression) {
            if (expression.TypeInfo is IterableTypeInfo iterableType) {
                return new ConversionExpression(ConversionExpression.Option.Iterable2Set, expression);
            } else {
                return null;
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
    }
}

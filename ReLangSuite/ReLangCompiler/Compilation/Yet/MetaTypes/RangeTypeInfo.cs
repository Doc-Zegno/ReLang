using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about range object
    /// </summary>
    class RangeTypeInfo : IterableTypeInfo {
        public override string Name => $"Range<{ItemType}>";


        public RangeTypeInfo(ITypeInfo itemType) : base(itemType) {

        }


        public override IExpression ConstructFrom(IExpression expression) {
            throw new NotImplementedException();
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            return obj is RangeTypeInfo;
        }


        public override int GetHashCode() {
            var hashCode = -1109124596;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}

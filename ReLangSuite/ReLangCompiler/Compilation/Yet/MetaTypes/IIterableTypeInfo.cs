using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class IterableTypeInfo : ITypeInfo {
        public virtual string Name => $"{ItemType.Name}&";

        /// <summary>
        /// Type of items obtained from this iterable
        /// </summary>
        public ITypeInfo ItemType { get; }


        public IterableTypeInfo(ITypeInfo itemType) {
            ItemType = itemType;
        }


        public virtual IExpression ConstructFrom(IExpression expression) {
            throw new NotImplementedException();
        }


        public virtual IExpression ConvertFrom(IExpression expression) {
            switch (expression) {
                case IterableTypeInfo iterableType when ItemType.Equals(iterableType.ItemType):
                case PrimitiveTypeInfo primitive
                when primitive.TypeOption == PrimitiveTypeInfo.Option.String && ItemType.Equals(PrimitiveTypeInfo.Char):
                    return expression;

                default:
                    return null;
            }
        }
    }
}

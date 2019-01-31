using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class IterableTypeInfo : ITypeInfo {
        public virtual string Name => $"{ItemType.Name}&";
        public virtual bool IsReferential => true;

        /// <summary>
        /// Type of items obtained from this iterable
        /// </summary>
        public ITypeInfo ItemType { get; }


        public IterableTypeInfo(ITypeInfo itemType) {
            ItemType = itemType;
        }


        public virtual IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public virtual IExpression ConvertFrom(IExpression expression) {
            switch (expression.TypeInfo) {
                case IterableTypeInfo iterableType when ItemType.Equals(iterableType.ItemType):
                case PrimitiveTypeInfo primitive
                when primitive.TypeOption == PrimitiveTypeInfo.Option.String && ItemType.Equals(PrimitiveTypeInfo.Char):
                    return expression.ChangeType(this);

                default:
                    return null;
            }
        }


        public virtual IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "contains" when ItemType is PrimitiveTypeInfo || ItemType is TupleTypeInfo:
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.IterableContains, 
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType }, 
                        new List<bool> { false, false },
                        PrimitiveTypeInfo.Bool);

                default:
                    return null;
            } 
        }
    }
}

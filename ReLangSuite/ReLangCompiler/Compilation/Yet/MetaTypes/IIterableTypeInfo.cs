using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class IterableTypeInfo : ITypeInfo {
        public virtual string Name => $"{ItemType.Name}*";
        public virtual bool IsReferential => true;
        public virtual bool IsComplete => ItemType.IsComplete;

        /// <summary>
        /// Type of items obtained from this iterable
        /// </summary>
        public ITypeInfo ItemType { get; }


        public IterableTypeInfo(ITypeInfo itemType) {
            ItemType = itemType;
        }


        public virtual IExpression GetDefaultValue(Location location) {
            return null;
        }


        public virtual ITypeInfo ResolveGeneric() {
            var resolvedItemType = ItemType.ResolveGeneric();
            if (resolvedItemType != null) {
                return new IterableTypeInfo(resolvedItemType);
            } else {
                return null;
            }
        }


        public virtual bool CanUpcast(ITypeInfo sourceType) {
            ITypeInfo sourceItemType = null;
            switch (sourceType) {
                case IterableTypeInfo iterable:
                    sourceItemType = iterable.ItemType;
                    break;

                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    sourceItemType = PrimitiveTypeInfo.Char;
                    break;

                default:
                    return false;
            }

            if (sourceItemType != null && ItemType.CanUpcast(sourceItemType)) {
                return true;
            } else {
                return false;
            }
        }


        public virtual IExpression ConstructFrom(IExpression expression, Location location) {
            /*if (CanUpcast(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else {
                return null;
            }*/
            return null;
        }


        public virtual IExpression ConvertFrom(IExpression expression) {
            /*switch (expression.TypeInfo) {
                case IterableTypeInfo iterableType when ItemType.Equals(iterableType.ItemType):
                case PrimitiveTypeInfo primitive
                when primitive.TypeOption == PrimitiveTypeInfo.Option.String && ItemType.Equals(PrimitiveTypeInfo.Char):
                    return expression.ChangeType(this);

                default:
                    return null;
            }*/

            if (CanUpcast(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else {
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
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                default:
                    return null;
            } 
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo || obj is IterableTypeInfo iterable && ItemType.Equals(iterable.ItemType)) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -1213424712;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            return hashCode;
        }
    }
}

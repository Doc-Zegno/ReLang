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
        public override string Name => $"Range<{ItemType.Name}>";


        public RangeTypeInfo(ITypeInfo itemType) : base(itemType) {
        }


        public override IExpression GetDefaultValue(Location location) => null;


        public override ITypeInfo ResolveGeneric() {
            var resolvedItemType = ItemType.ResolveGeneric();
            if (resolvedItemType != null) {
                return new RangeTypeInfo(resolvedItemType);
            } else {
                return null;
            }
        }


        public override bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public override IExpression ConstructFrom(IExpression expression, Location location) {
            //throw new NotImplementedException();
            return null;
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else {
                return null;
            }
        }


        public override IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "init":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.RangeInit,
                        new List<string> { "start", "end", "step" },
                        new List<ITypeInfo> { PrimitiveTypeInfo.Int, PrimitiveTypeInfo.Int, PrimitiveTypeInfo.Int },
                        new List<bool> { false, false, false },
                        new List<IExpression> {
                            null,
                            null,
                            new PrimitiveLiteralExpression(1, PrimitiveTypeInfo.Int, null)
                        },
                        this);

                case "contains":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.RangeContains, 
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType }, 
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                default:
                    return base.GetMethodDefinition(name, isSelfMutable);
            }
        }


        public override bool Equals(object obj) {
            return obj is IncompleteTypeInfo || obj is RangeTypeInfo;
        }


        public override int GetHashCode() {
            var hashCode = -1109124596;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}

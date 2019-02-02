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


        public override bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public override IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "contains":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.RangeContains, 
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType }, 
                        new List<bool> { false, false },
                        PrimitiveTypeInfo.Bool);

                default:
                    return base.GetMethodDefinition(name, isSelfMutable);
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

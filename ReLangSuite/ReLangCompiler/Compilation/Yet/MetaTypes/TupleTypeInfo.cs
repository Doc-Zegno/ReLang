using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about a tuple
    /// </summary>
    class TupleTypeInfo : ITypeInfo {
        public string Name => $"({string.Join(", ", ItemTypes.Select(itemType => itemType.Name))})";
        public bool IsReferential => false;

        /// <summary>
        /// Types of tuple's items
        /// </summary>
        public List<ITypeInfo> ItemTypes { get; }


        public TupleTypeInfo(List<ITypeInfo> itemTypes) {
            ItemTypes = itemTypes;
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public IFunctionDefinition GetMethodDefinition(string name) {
            switch (name) {
                case "getFirst" when ItemTypes.Count >= 1:
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.TupleGetFirst, 
                        new List<string> { "self" },
                        new List<ITypeInfo> { this }, 
                        new List<bool> { false },
                        ItemTypes[0]);

                case "getSecond" when ItemTypes.Count >= 2:
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.TupleGetSecond,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        ItemTypes[1]);

                case "getThird" when ItemTypes.Count >= 3:
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.TupleGetThird,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        ItemTypes[2]);

                default:
                    return null;
            }
        }


        public IFunctionDefinition GetTupleAccessorDefinition(int index) =>
            new BuiltinFunctionDefinition(
                "get",
                BuiltinFunctionDefinition.Option.TupleGet,
                new List<string> { "self", "index" },
                new List<ITypeInfo> { this, PrimitiveTypeInfo.Int },
                new List<bool> { false, false },
                ItemTypes[index]);


        public override bool Equals(object obj) {
            if (obj is TupleTypeInfo tupleType) {
                if (tupleType.ItemTypes.Count == ItemTypes.Count) {
                    for (var i = 0; i < ItemTypes.Count; i++) {
                        var x = ItemTypes[i];
                        var y = tupleType.ItemTypes[i];
                        if (!x.Equals(y)) {
                            return false;
                        }
                    }
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = 701787509;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<ITypeInfo>>.Default.GetHashCode(ItemTypes);
            return hashCode;
        }
    }
}

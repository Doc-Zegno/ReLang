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


        public override IExpression ConstructFrom(IExpression expression, Location location) {
            switch (expression.TypeInfo) {
                case IterableTypeInfo iterable:
                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    return new ConversionExpression(ConversionExpression.Option.Iterable2Set, expression, location);

                default:
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


        public override IFunctionDefinition GetMethodDefinition(string name) {
            switch (name) {
                case "getLength":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetGetLength,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        PrimitiveTypeInfo.Int);

                case "add":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetAdd,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<bool> { true, false },
                        PrimitiveTypeInfo.Void);

                case "remove":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetRemove,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<bool> { true, false },
                        PrimitiveTypeInfo.Bool);

                case "union":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetUnion,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<bool> { false, false },
                        this);

                case "intersection":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetIntersection,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<bool> { false, false },
                        this);

                case "difference":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetDifference,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<bool> { false, false },
                        this);

                case "contains" when ItemType is PrimitiveTypeInfo || ItemType is TupleTypeInfo:
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetContains,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<bool> { false, false },
                        PrimitiveTypeInfo.Bool);

                case "copy":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetCopy,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        this);

                default:
                    return base.GetMethodDefinition(name);
            }
        }
    }
}

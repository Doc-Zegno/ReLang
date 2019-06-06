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


        public override IExpression GetDefaultValue(Location location) {
            return new SetLiteralExpression(new List<IExpression>(), ItemType, location);
        }


        public override ITypeInfo ResolveGeneric() {
            var resolvedItemType = ItemType.ResolveGeneric();
            if (resolvedItemType != null) {
                return new HashSetTypeInfo(resolvedItemType);
            } else {
                return null;
            }
        }


        public override bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else {
                return null;
            }
        }


        public override IExpression ConstructFrom(IExpression expression, Location location) {
            ITypeInfo itemType = null;
            switch (expression.TypeInfo) {
                case IterableTypeInfo iterable:
                    itemType = iterable.ItemType;
                    break;

                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    itemType = PrimitiveTypeInfo.Char;
                    break;
                    
                default:
                    return null;
            }

            if (ItemType.IsComplete) {
                if (ItemType.CanUpcast(itemType)) {
                    return new ConversionExpression(
                        ConversionExpression.Option.Iterable2Set,
                        expression.ChangeType(new IterableTypeInfo(ItemType)),
                        location);
                } else {
                    return null;
                }
            } else {
                return new ConversionExpression(ConversionExpression.Option.Iterable2Set, expression, location);
            }
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo || obj is HashSetTypeInfo setType && ItemType.Equals(setType.ItemType)) {
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
                /*case "init":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetInit,
                        new List<string> { },
                        new List<ITypeInfo> { },
                        new List<bool> { },
                        new List<IExpression> { },
                        this);*/

                case "getLength":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetGetLength,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<IExpression> { null },
                        PrimitiveTypeInfo.Int);

                case "add":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetAdd,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Void);

                case "remove":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetRemove,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                case "union":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetUnion,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<IExpression> { null, null },
                        this);

                case "intersection":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetIntersection,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<IExpression> { null, null },
                        this);

                case "difference":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetDifference,
                        new List<string> { "self", "set" },
                        new List<ITypeInfo> { this, this },
                        new List<IExpression> { null, null },
                        this);

                case "contains" when ItemType is PrimitiveTypeInfo || ItemType is TupleTypeInfo:
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetContains,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                case "copy":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetCopy,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<IExpression> { null },
                        this);

                default:
                    return base.GetMethodDefinition(name);
            }
        }
    }
}

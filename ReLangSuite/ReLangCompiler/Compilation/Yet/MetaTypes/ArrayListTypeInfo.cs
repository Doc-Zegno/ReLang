using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about ArrayList
    /// </summary>
    class ArrayListTypeInfo : IterableTypeInfo {
        public override string Name => $"[{ItemType.Name}]";


        public ArrayListTypeInfo(ITypeInfo itemType) : base(itemType) {
        }


        public override ITypeInfo ResolveGeneric() {
            var resolvedItemType = ItemType.ResolveGeneric();
            if (resolvedItemType != null) {
                return new ArrayListTypeInfo(resolvedItemType);
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
            switch (expression.TypeInfo) {
                case IterableTypeInfo iterable when !(iterable.ItemType is IncompleteTypeInfo):
                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    return new ConversionExpression(ConversionExpression.Option.Iterable2List, expression, location);

                default:
                    return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo || obj is ArrayListTypeInfo arrayListType && ItemType.Equals(arrayListType.ItemType)) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -120175732;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            return hashCode;
        }


        public override IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "get":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGet, 
                        new List<string> { "self", "index" },
                        new List<ITypeInfo> { this, PrimitiveTypeInfo.Int }, 
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        ItemType,
                        isSelfMutable);

                case "getLength":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGetLength,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this }, 
                        new List<bool> { false },
                        new List<IExpression> { null },
                        PrimitiveTypeInfo.Int);

                case "getSlice":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGetSlice,
                        new List<string> { "self", "start", "end", "step" },
                        new List<ITypeInfo> { this, PrimitiveTypeInfo.Int, new MaybeTypeInfo(PrimitiveTypeInfo.Int), PrimitiveTypeInfo.Int },
                        new List<bool> { isSelfMutable, false, false, false },
                        new List<IExpression> { null, null, null, null },
                        this,
                        isSelfMutable);

                /*case "getConstSlice":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGetSlice,
                        new List<string> { "self", "start", "end", "step" },
                        new List<ITypeInfo> { this, PrimitiveTypeInfo.Int, new MaybeTypeInfo(PrimitiveTypeInfo.Int), PrimitiveTypeInfo.Int },
                        new List<bool> { false, false, false, false },
                        this,
                        false);*/

                case "set":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListSet, 
                        new List<string> { "self", "index", "value" },
                        new List<ITypeInfo> { this, PrimitiveTypeInfo.Int, ItemType },
                        new List<bool> { true, false, false },
                        new List<IExpression> { null, null, null },
                        PrimitiveTypeInfo.Void);

                case "append":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListAppend, 
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<bool> { true, false },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Void);

                case "extend":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListExtend,
                        new List<string> { "self", "list" },
                        new List<ITypeInfo> { this, this },
                        new List<bool> { true, false },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Void);

                case "contains" when ItemType is PrimitiveTypeInfo || ItemType is TupleTypeInfo:
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.ListContains,
                        new List<string> { "self", "value" },
                        new List<ITypeInfo> { this, ItemType },
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                case "copy":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListCopy,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        new List<IExpression> { null },
                        this);

                default:
                    return base.GetMethodDefinition(name, isSelfMutable);
            }
        }
    }
}

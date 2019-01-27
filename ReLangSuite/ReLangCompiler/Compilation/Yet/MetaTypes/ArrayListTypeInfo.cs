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
                    return new ConversionExpression(ConversionExpression.Option.Iterable2List, expression, location);

                default:
                    return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is ArrayListTypeInfo arrayListType && ItemType.Equals(arrayListType.ItemType)) {
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


        public override IFunctionDefinition GetMethodDefinition(string name) {
            switch (name) {
                case "get":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGet, 
                        new List<ITypeInfo> { PrimitiveTypeInfo.Int }, 
                        ItemType);

                case "getLength":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListGetLength, 
                        new List<ITypeInfo> { }, 
                        PrimitiveTypeInfo.Int);

                case "set":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListSet, 
                        new List<ITypeInfo> { PrimitiveTypeInfo.Int, ItemType }, 
                        PrimitiveTypeInfo.Void);

                case "append":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListAppend, 
                        new List<ITypeInfo> { ItemType }, 
                        PrimitiveTypeInfo.Void);

                case "extend":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ListExtend, 
                        new List<ITypeInfo> { this }, 
                        PrimitiveTypeInfo.Void);

                default:
                    return base.GetMethodDefinition(name);
            }
        }
    }
}

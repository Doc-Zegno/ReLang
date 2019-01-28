﻿using System;
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
                        new List<ITypeInfo> { },
                        PrimitiveTypeInfo.Int);

                case "add":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetAdd, 
                        new List<ITypeInfo> { ItemType }, 
                        PrimitiveTypeInfo.Void);

                case "remove":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetRemove, 
                        new List<ITypeInfo> { ItemType }, 
                        PrimitiveTypeInfo.Bool);

                case "union":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetUnion, 
                        new List<ITypeInfo> { this }, 
                        this);

                case "intersection":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetIntersection,
                        new List<ITypeInfo> { this },
                        this);

                case "difference":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetDifference,
                        new List<ITypeInfo> { this },
                        this);

                case "contains":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.SetContains, 
                        new List<ITypeInfo> { ItemType }, 
                        PrimitiveTypeInfo.Bool);

                case "copy":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.SetCopy,
                        new List<ITypeInfo> { },
                        this);

                default:
                    return base.GetMethodDefinition(name);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Information about dictionary type
    /// </summary>
    class DictionaryTypeInfo : IterableTypeInfo {
        public override string Name => $"{{{KeyType.Name}: {ValueType.Name}}}";

        public ITypeInfo KeyType { get; }
        public ITypeInfo ValueType { get; }


        public DictionaryTypeInfo(ITypeInfo keyType, ITypeInfo valueType)
            : base(new TupleTypeInfo(new List<ITypeInfo> { keyType, valueType }))
        {
            KeyType = keyType;
            ValueType = valueType;
        }


        public override IExpression GetDefaultValue(Location location) {
            return new DictionaryLiteralExpression(new List<(IExpression, IExpression)>(), KeyType, ValueType, location);
        }


        public override ITypeInfo ResolveGeneric() {
            var resolvedKeyType = KeyType.ResolveGeneric();
            var resolvedValueType = ValueType.ResolveGeneric();
            if (resolvedKeyType != null && resolvedValueType != null) {
                return new DictionaryTypeInfo(resolvedKeyType, resolvedValueType);
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
            if (expression.TypeInfo is IterableTypeInfo iterableType
                && iterableType.ItemType is TupleTypeInfo tupleType
                && tupleType.ItemTypes.Count == 2)
            {
                var keyType = tupleType.ItemTypes[0];
                var valueType = tupleType.ItemTypes[1];

                if (KeyType.IsComplete && ValueType.IsComplete) {
                    if (KeyType.CanUpcast(keyType) && ValueType.CanUpcast(valueType)) {
                        return new ConversionExpression(
                            ConversionExpression.Option.Iterable2Dictionary,
                            expression.ChangeType(
                                new IterableTypeInfo(new TupleTypeInfo(new List<ITypeInfo> { KeyType, ValueType }))),
                            location);
                    } else {
                        return null;
                    }

                } else if (!KeyType.IsComplete && !ValueType.IsComplete) {
                    return new ConversionExpression(ConversionExpression.Option.Iterable2Dictionary, expression, location);

                } else {
                    throw new NotImplementedException("Dictionary type has only one of parameter types being incomplete. Is it ever possible?");
                }
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo
                || obj is DictionaryTypeInfo dictionaryType
                   && KeyType.Equals(dictionaryType.KeyType)
                   && ValueType.Equals(dictionaryType.ValueType))
            {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -304684678;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ItemType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(KeyType);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ValueType);
            return hashCode;
        }


        public override IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                /*case "init":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.DictionaryInit,
                        new List<string> { },
                        new List<ITypeInfo> { },
                        new List<bool> { },
                        new List<IExpression> { },
                        this);*/

                case "get":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.DictionaryGet, 
                        new List<string> { "self", "key" },
                        new List<ITypeInfo> { this, KeyType }, 
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        ValueType,
                        isSelfMutable);

                case "set":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.DictionarySet,
                        new List<string> { "self", "key", "value" },
                        new List<ITypeInfo> { this, KeyType, ValueType },
                        new List<bool> { true, false, false },
                        new List<IExpression> { null, null, null },
                        PrimitiveTypeInfo.Void);

                case "tryGet":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.DictionaryTryGet,
                        new List<string> { "self", "key" },
                        new List<ITypeInfo> { this, KeyType },
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        new MaybeTypeInfo(ValueType),
                        isSelfMutable);

                case "getLength":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.DictionaryGetLength,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        new List<IExpression> { null },
                        PrimitiveTypeInfo.Int);

                case "contains" when KeyType is PrimitiveTypeInfo || KeyType is TupleTypeInfo:
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.DictionaryContains,
                        new List<string> { "self", "key" },
                        new List<ITypeInfo> { this, KeyType },
                        new List<bool> { false, false },
                        new List<IExpression> { null, null },
                        PrimitiveTypeInfo.Bool);

                case "copy":
                    return new BuiltinFunctionDefinition(
                        name, 
                        BuiltinFunctionDefinition.Option.DictionaryCopy,
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

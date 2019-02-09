using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class GenericTypeInfo : ITypeInfo {
        private IDictionary<string, ITypeInfo> names2types;

        public string MetaName { get; }
        public string Name {
            get {
                if (names2types.TryGetValue(MetaName, out ITypeInfo typeInfo)) {
                    return $"{MetaName}={typeInfo.Name}";
                } else {
                    return $"{MetaName}=???";
                }
            }
        }

        public bool IsReferential => throw new NotImplementedException();
        public bool IsComplete => throw new NotImplementedException();


        public GenericTypeInfo(string metaName, IDictionary<string, ITypeInfo> names2types) {
            this.names2types = names2types;
            MetaName = metaName;
        }


        public ITypeInfo ResolveGeneric() {
            if (names2types.TryGetValue(MetaName, out ITypeInfo resolvedType)) {
                return resolvedType;
            } else {
                return null;
            }
        }


        public bool CanUpcast(ITypeInfo sourceType) {
            if (names2types.TryGetValue(MetaName, out ITypeInfo typeInfo)) {
                return typeInfo.CanUpcast(sourceType);
            } else {
                if (sourceType is NullTypeInfo || sourceType is MaybeTypeInfo) {
                    return false;
                } else {
                    names2types[MetaName] = sourceType;
                    return true;
                }
            }
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (names2types.TryGetValue(MetaName, out ITypeInfo typeInfo)) {
                return typeInfo.ConvertFrom(expression);
            } else {
                if (expression.TypeInfo is NullTypeInfo || expression.TypeInfo is MaybeTypeInfo) {
                    return null;
                } else {
                    names2types[MetaName] = expression.TypeInfo;
                    return expression;
                }
            }
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            if (names2types.TryGetValue(MetaName, out ITypeInfo typeInfo)) {
                return typeInfo.GetMethodDefinition(name, isSelfMutable);
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            if (names2types.TryGetValue(MetaName, out ITypeInfo typeInfo)) {
                return typeInfo.Equals(obj);
            } else {
                return obj is GenericTypeInfo genericType && MetaName == genericType.MetaName;
            }
        }


        public override int GetHashCode() {
            var hashCode = -1463151007;
            hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<string, ITypeInfo>>.Default.GetHashCode(names2types);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MetaName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            return hashCode;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta type of type literal
    /// </summary>
    class TypeLiteralTypeInfo : ITypeInfo {
        public string Name => $"Type<{InternalType.Name}>";
        public bool IsReferential => false;
        public bool IsComplete => false;

        public ITypeInfo InternalType { get; }


        public TypeLiteralTypeInfo(ITypeInfo internalType) {
            InternalType = internalType;
        }


        public IExpression GetDefaultValue(Location location) => null;


        public bool CanUpcast(ITypeInfo sourceType) {
            throw new NotImplementedException();
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public IExpression ConvertFrom(IExpression expression) {
            throw new NotImplementedException();
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            throw new NotImplementedException();
        }


        public ITypeInfo ResolveGeneric() {
            throw new NotImplementedException();
        }


        public override bool Equals(object obj) {
            return obj is TypeLiteralTypeInfo typeLiteral && InternalType.Equals(typeLiteral.InternalType);
        }


        public override int GetHashCode() {
            var hashCode = -160525060;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            hashCode = hashCode * -1521134295 + IsComplete.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(InternalType);
            return hashCode;
        }
    }
}

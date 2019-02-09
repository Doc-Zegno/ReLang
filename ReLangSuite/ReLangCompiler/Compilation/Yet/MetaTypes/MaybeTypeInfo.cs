using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about maybe
    /// </summary>
    class MaybeTypeInfo : ITypeInfo {
        public string Name => $"{InternalType.Name}?";
        public bool IsReferential => true;
        public bool IsComplete => InternalType.IsComplete;

        /// <summary>
        /// Type of object within this maybe
        /// </summary>
        public ITypeInfo InternalType { get; }


        public MaybeTypeInfo(ITypeInfo internalType) {
            InternalType = internalType;
        }


        public ITypeInfo ResolveGeneric() {
            var resolvedInternalType = InternalType.ResolveGeneric();
            if (resolvedInternalType != null) {
                return new MaybeTypeInfo(resolvedInternalType);
            } else {
                return null;
            }
        }


        public bool CanUpcast(ITypeInfo sourceType) {
            // Object? <- Float?
            return sourceType is MaybeTypeInfo maybeType && InternalType.CanUpcast(maybeType.InternalType);
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            return ConvertFrom(expression);
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (CanUpcast(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else if (InternalType.Equals(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else if (expression.TypeInfo is NullTypeInfo) {
                return expression.ChangeType(this);
            } else {
                return null;
            }
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            return null;
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo || obj is MaybeTypeInfo maybeType && InternalType.Equals(maybeType.InternalType)) {
                return true;
            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -1755835554;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(InternalType);
            return hashCode;
        }
    }
}

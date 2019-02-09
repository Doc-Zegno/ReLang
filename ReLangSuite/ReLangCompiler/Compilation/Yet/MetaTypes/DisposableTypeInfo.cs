using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta information about disposable interface
    /// </summary>
    class DisposableTypeInfo : ITypeInfo {
        public string Name => "Disposable";
        public bool IsReferential => true;
        public bool IsComplete => true;


        public ITypeInfo ResolveGeneric() {
            return this;
        }


        public bool CanUpcast(ITypeInfo sourceType) {
            if (Equals(sourceType)) {
                return true;
            } else {
                switch (sourceType) {
                    case FileStreamTypeInfo fileStreamType:
                        return true;

                    default:
                        return false;
                }
            }
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            throw new NotImplementedException();
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (CanUpcast(expression.TypeInfo)) {
                return expression.ChangeType(this);
            } else {
                return null;
            }
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            return null;
        }


        public override bool Equals(object obj) {
            return obj is IncompleteTypeInfo || obj is DisposableTypeInfo;
        }


        public override int GetHashCode() {
            var hashCode = 1978336082;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            return hashCode;
        }
    }
}

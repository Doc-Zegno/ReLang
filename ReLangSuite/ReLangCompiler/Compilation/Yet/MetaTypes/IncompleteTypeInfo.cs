using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class IncompleteTypeInfo : ITypeInfo {
        public string Name => "???";
        public bool IsReferential => throw new NotImplementedException();
        public bool IsComplete => false;


        public bool CanUpcast(ITypeInfo sourceType) {
            return false;
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            return null;
        }


        public IExpression ConvertFrom(IExpression expression) {
            return null;
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            return null;
        }


        public ITypeInfo ResolveGeneric() {
            return this;
        }


        public override bool Equals(object obj) {
            return obj is IncompleteTypeInfo;
        }


        public override int GetHashCode() {
            var hashCode = 1978336082;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            return hashCode;
        }
    }
}

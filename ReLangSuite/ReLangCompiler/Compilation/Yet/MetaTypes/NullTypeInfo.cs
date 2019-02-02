using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class NullTypeInfo : ITypeInfo {
        public string Name => "Null";
        public bool IsReferential => true;


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
    }
}

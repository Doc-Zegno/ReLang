using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a function literal
    /// </summary>
    class FunctionLiteralExpression : ILiteralExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public IFunctionDefinition Definition { get; }


        public FunctionLiteralExpression(IFunctionDefinition definition, Location location) {
            Definition = definition;
            MainLocation = location;

            var signature = definition.Signature;
            TypeInfo = new FunctionTypeInfo(
                signature.ArgumentTypes, signature.ArgumentMutabilities, signature.ResultType, signature.ResultMutability
            );
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (FunctionLiteralExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

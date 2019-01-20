using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Expression representing a type literal (Int, [Bool])
    /// </summary>
    class TypeLiteralExpression : IExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => true;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }


        public TypeLiteralExpression(ITypeInfo typeInfo) {
            TypeInfo = typeInfo;
        }
    }
}

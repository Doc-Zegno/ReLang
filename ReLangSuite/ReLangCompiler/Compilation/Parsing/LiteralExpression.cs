using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Literal used as an expression
    /// </summary>
    class LiteralExpression : IExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => true;
        public object Value { get; }
        public ITypeInfo TypeInfo { get; }


        public LiteralExpression(object value, ITypeInfo typeInfo) {
            Value = value;
            TypeInfo = typeInfo;
        }
    }
}

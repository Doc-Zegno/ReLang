using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Literal used as an expression
    /// </summary>
    class PrimitiveLiteralExpression : ILiteralExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => true;
        public object Value { get; }
        public ITypeInfo TypeInfo { get; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }


        public PrimitiveLiteralExpression(object value, ITypeInfo typeInfo, Location mainLocation) {
            Value = value;
            TypeInfo = typeInfo;
            MainLocation = mainLocation;
        }
    }
}

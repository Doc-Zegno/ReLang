using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a null literal
    /// </summary>
    class NullLiteralExpression : ILiteralExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => true;
        public object Value => null;
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        
        public NullLiteralExpression(Location mainLocation) {
            TypeInfo = new NullTypeInfo();
            MainLocation = mainLocation;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (NullLiteralExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

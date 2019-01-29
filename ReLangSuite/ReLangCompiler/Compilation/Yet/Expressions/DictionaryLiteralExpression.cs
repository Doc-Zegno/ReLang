using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a dictionary literal ({1: "One", 2: "Two"})
    /// </summary>
    class DictionaryLiteralExpression : ILiteralExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        /// <summary>
        /// (key, value)-pairs of this dictionary literal
        /// </summary>
        public List<(IExpression, IExpression)> Pairs { get; }


        public DictionaryLiteralExpression(List<(IExpression, IExpression)> pairs, ITypeInfo keyType,
                                           ITypeInfo valueType, Location mainLocation)
        {
            Pairs = pairs;
            TypeInfo = new DictionaryTypeInfo(keyType, valueType);
            MainLocation = mainLocation;

            HasSideEffect = false;
            foreach (var (key, value) in pairs) {
                if (key.HasSideEffect || value.HasSideEffect) {
                    HasSideEffect = true;
                    break;
                }
            }
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (DictionaryLiteralExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

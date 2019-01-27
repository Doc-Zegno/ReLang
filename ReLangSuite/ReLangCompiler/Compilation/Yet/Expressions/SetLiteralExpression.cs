using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Literal representing a has set of values ({1, 2, 3, 4, 5})
    /// </summary>
    class SetLiteralExpression : ILiteralExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public List<IExpression> Items { get; }


        public SetLiteralExpression(List<IExpression> items, ITypeInfo itemType, Location mainLocation) {
            Items = items;
            TypeInfo = new HashSetTypeInfo(itemType);
            MainLocation = mainLocation;

            HasSideEffect = false;
            foreach (var item in items) {
                if (item.HasSideEffect) {
                    HasSideEffect = true;
                    break;
                }
            }
        }
    }
}

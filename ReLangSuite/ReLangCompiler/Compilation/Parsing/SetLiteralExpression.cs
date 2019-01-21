using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Literal representing a has set of values ({1, 2, 3, 4, 5})
    /// </summary>
    class SetLiteralExpression : ILiteralExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        public List<IExpression> Items { get; }


        public SetLiteralExpression(List<IExpression> items, ITypeInfo itemType) {
            Items = items;
            TypeInfo = new HashSetTypeInfo(itemType);

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

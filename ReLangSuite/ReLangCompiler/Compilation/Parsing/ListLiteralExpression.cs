using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Expression representing a list literal ([1, 2, 3, 4, 5])
    /// </summary>
    class ListLiteralExpression : IExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        /// <summary>
        /// Items of list literal
        /// </summary>
        public List<IExpression> Items { get; }


        public ListLiteralExpression(List<IExpression> items, ITypeInfo itemType, bool hasSideEffect) {
            Items = items;
            TypeInfo = new ArrayListTypeInfo(itemType);
            HasSideEffect = hasSideEffect;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a tuple literal ((1, 2.0, "3"))
    /// </summary>
    class TupleLiteralExpression : ILiteralExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime { get; }
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        /// <summary>
        /// Items of this tuple literal
        /// </summary>
        public List<IExpression> Items { get; }


        public TupleLiteralExpression(List<IExpression> items) {
            Items = items;

            var itemTypes = new List<ITypeInfo>();
            var hasSideEffect = false;
            var isCompileTime = true;
            foreach (var item in items) {
                itemTypes.Add(item.TypeInfo);
                if (item.HasSideEffect) {
                    hasSideEffect = true;
                }
                if (!item.IsCompileTime) {
                    isCompileTime = false;
                }
            }

            HasSideEffect = hasSideEffect;
            IsCompileTime = isCompileTime;
            TypeInfo = new TupleTypeInfo(itemTypes);
        }
    }
}

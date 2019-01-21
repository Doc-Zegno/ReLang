using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    // TODO: remove this
    /// <summary>
    /// Adapter for tuple data type
    /// </summary>
    class TupleAdapter {
        /// <summary>
        /// Tuple's items
        /// </summary>
        public object[] Items { get; }


        public TupleAdapter(object[] items) {
            Items = items;
        }
    }
}

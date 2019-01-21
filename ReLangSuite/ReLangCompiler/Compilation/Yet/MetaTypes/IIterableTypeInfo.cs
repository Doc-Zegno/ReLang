using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    interface IIterableTypeInfo : ITypeInfo {
        /// <summary>
        /// Type of items obtained from this iterable
        /// </summary>
        ITypeInfo ItemType { get; }
    }
}

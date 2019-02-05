using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    [Flags]
    public enum VariableQualifier {
        None = 0,
        Final = 1,
        Mutable = 2,
        Disposable = 4,
    }
}

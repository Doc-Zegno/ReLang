using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing break or continue
    /// </summary>
    class BreakStatement : IStatement {
        public bool IsContinue { get; }

        public BreakStatement(bool isContinue) {
            IsContinue = isContinue;
        }
    }
}

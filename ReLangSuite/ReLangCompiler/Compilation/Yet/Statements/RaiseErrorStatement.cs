using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing an error raising
    /// </summary>
    class RaiseErrorStatement : IStatement {
        public IExpression ErrorExpression;

        public RaiseErrorStatement(IExpression errorExpression) {
            ErrorExpression = errorExpression;
        }
    }
}

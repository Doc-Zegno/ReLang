using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing returning a value from function
    /// </summary>
    class ReturnStatement : IStatement {
        /// <summary>
        /// Returned value
        /// </summary>
        public IExpression Operand { get; }

        public ReturnStatement(IExpression operand) {
            Operand = operand;
        }
    }
}

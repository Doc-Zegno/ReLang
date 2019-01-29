using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a do-while-loop
    /// </summary>
    class DoWhileStatement : IStatement {
        public IExpression Condition { get; }
        public List<IStatement> Statements { get; }


        public DoWhileStatement(IExpression condition, List<IStatement> statements) {
            Condition = condition;
            Statements = statements;
        }
    }
}

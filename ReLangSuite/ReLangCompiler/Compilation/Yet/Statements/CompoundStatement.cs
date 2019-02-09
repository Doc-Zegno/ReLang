using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement grouping multiple statements into one
    /// </summary>
    class CompoundStatement : IStatement {
        public List<IStatement> Statements { get; }

        public CompoundStatement(List<IStatement> statements) {
            Statements = new List<IStatement>(
                statements.Where(statement => !(statement is NopeStatement))
            );
        }
    }
}

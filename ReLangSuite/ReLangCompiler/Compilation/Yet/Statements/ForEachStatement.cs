using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a for-each loop
    /// </summary>
    class ForEachStatement : IStatement {
        public string ItemName { get; }
        public IExpression Iterable { get; }
        public List<IStatement> Statements { get; }


        public ForEachStatement(string itemName, IExpression iterable, List<IStatement> statements) {
            ItemName = itemName;
            Iterable = iterable;
            Statements = statements;
        }
    }
}

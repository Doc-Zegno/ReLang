using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// If-else block statement
    /// </summary>
    class ConditionalStatement : IStatement {
        public IExpression Condition { get; }
        public List<IStatement> IfStatements { get; }
        public List<IStatement> ElseStatements { get; }


        public ConditionalStatement(IExpression condition, List<IStatement> ifStatements, List<IStatement> elseStatements) {
            Condition = condition;
            IfStatements = ifStatements;
            ElseStatements = elseStatements;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Statement that evaluates an expression and discards its value
    /// </summary>
    class ExpressionStatement : IStatement {
        public IExpression Expression { get; }

        public ExpressionStatement(IExpression expression) {
            Expression = expression;
        }
    }
}

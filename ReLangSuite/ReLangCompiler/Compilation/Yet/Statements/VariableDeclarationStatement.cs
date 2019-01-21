using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a declaration of variable
    /// </summary>
    class VariableDeclarationStatement : IDeclarationStatement {
        public string Name { get; }
        public IExpression Value { get; }
        public bool IsMutable { get; }


        public VariableDeclarationStatement(string name, IExpression value, bool isMutable) {
            Name = name;
            Value = value;
            IsMutable = isMutable;
        }
    }
}

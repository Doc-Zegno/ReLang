using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement declaring a new function
    /// </summary>
    class FunctionDeclarationStatement : IDeclarationStatement {
        public string Name { get; }
        public List<IStatement> Body { get; }


        public FunctionDeclarationStatement(string name, List<IStatement> body) {
            Name = name;
            Body = body;
        }
    }
}

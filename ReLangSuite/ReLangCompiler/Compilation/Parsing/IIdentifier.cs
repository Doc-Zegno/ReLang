using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Generalization of variable names
    /// </summary>
    interface IIdentifier {
        Lexing.Location StartLocation { get; }
    }
}

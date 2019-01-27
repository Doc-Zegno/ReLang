using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Token of input text which is meaningful for the compiler
    /// </summary>
    public abstract class Lexeme {
        /// <summary>
        /// Location of the first character of this token
        /// </summary>
        public Location StartLocation { get; }
        
        public Lexeme(Location location) {
            StartLocation = location;
        }
    }
}

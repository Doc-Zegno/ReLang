using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    struct Location {
        public string Line { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        public Location(string line, int lineNumber, int columnNumber) {
            Line = line;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }
    }



    /// <summary>
    /// Token of input text which is meaningful for the compiler
    /// </summary>
    abstract class Lexeme {
        /// <summary>
        /// Location of the first character of this token
        /// </summary>
        public Location StartLocation { get; }
        
        public Lexeme(Location location) {
            StartLocation = location;
        }
    }
}

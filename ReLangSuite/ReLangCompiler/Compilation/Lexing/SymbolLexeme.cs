using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Lexeme representing a symbol (identifier)
    /// </summary>
    class SymbolLexeme : Lexeme {
        /// <summary>
        /// Textual representation of this symbol (that can be identifier name)
        /// </summary>
        public string Text { get; }

        public SymbolLexeme(string text, Location location) : base(location) {
            Text = text;
        }
    }
}

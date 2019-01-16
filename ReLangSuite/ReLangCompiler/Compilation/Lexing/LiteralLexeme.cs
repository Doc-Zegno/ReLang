using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Lexeme representing literal value
    /// </summary>
    class LiteralLexeme : ILexeme {
        public object Value { get; }

        public LiteralLexeme(object value) {
            Value = value;
        }
    }
}

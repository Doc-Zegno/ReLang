using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Lexeme representing one piece of format string literal
    /// </summary>
    class FormatStringLexeme : Lexeme {
        /// <summary>
        /// Format string's piece
        /// </summary>
        public string Piece { get; }

        /// <summary>
        /// Whether this piece is ending one
        /// </summary>
        public bool IsEnding { get; }


        public FormatStringLexeme(string piece, bool isEnding, Location location) : base(location) {
            Piece = piece;
            IsEnding = isEnding;
        }
    }
}

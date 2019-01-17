using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Possible semantic values of operators
    /// </summary>
    enum OperatorMeaning {
        Unknown,

        // Symbolic
        OpenParenthesis,
        CloseParenthesis,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        NewLine,
        Assignment,
        Equal,
        Comma,

        // Textual
        Var,
        Let,
        If,
        Else,
        Func,
    }



    /// <summary>
    /// Lexeme representing an operator of Re:Lang
    /// </summary>
    class OperatorLexeme : Lexeme {
        /// <summary>
        /// Semantic value of this operator
        /// </summary>
        public OperatorMeaning Meaning { get; } 
        

        public OperatorLexeme(OperatorMeaning meaning, Location location) : base(location) {
            Meaning = meaning;
        }
    }
}

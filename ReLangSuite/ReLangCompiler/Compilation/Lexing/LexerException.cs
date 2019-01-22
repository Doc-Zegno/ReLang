using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Signalizes about lexical errors
    /// </summary>
    [Serializable]
    public class LexerException : Exception {
        /// <summary>
        /// Line of code containing error
        /// </summary>
        public string Line { get; }

        /// <summary>
        /// Number of code line containing error
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Number of code column containing error
        /// </summary>
        public int ColumnNumber { get; }


        public LexerException(string message, string line, int lineNumber, int columnNumber)
            : base(message)
        {
            Line = line;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }


        protected LexerException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}

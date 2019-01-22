using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Signalizes about syntactic errors
    /// </summary>
    [Serializable]
    public class ParserException : Exception {
        /// <summary>
        /// Code line containing error
        /// </summary>
        public string Line;

        /// <summary>
        /// Number of code line containing error
        /// </summary>
        public int LineNumber;

        /// <summary>
        /// Number of code column containing error
        /// </summary>
        public int ColumnNumber;


        public ParserException(string message, string line, int lineNumber, int columnNumber)
            : base(message)
        {
            Line = line;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }


        protected ParserException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}

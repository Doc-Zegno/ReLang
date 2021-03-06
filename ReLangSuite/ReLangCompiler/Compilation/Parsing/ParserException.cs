﻿using System;
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
        public string Line { get; }

        /// <summary>
        /// Number of code line containing error
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Number of code column containing error
        /// </summary>
        public int ColumnNumber { get; }

        public bool IsSemantic { get; }


        public ParserException(string message, string line, int lineNumber, int columnNumber, bool isSemantic)
            : base(message)
        {
            Line = line;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            IsSemantic = isSemantic;
        }


        protected ParserException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}

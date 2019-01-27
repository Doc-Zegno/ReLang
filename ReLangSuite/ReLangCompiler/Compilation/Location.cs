using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation {
    public class Location {
        public string Line { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }

        public Location(string line, int lineNumber, int columnNumber) {
            Line = line;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }
    }
}

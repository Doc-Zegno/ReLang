using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Single identifier: name + location
    /// </summary>
    class SingleIdentifier : IIdentifier {
        public string Name { get; }
        public Location StartLocation { get; }


        public SingleIdentifier(string name, Location startLocation) {
            Name = name;
            StartLocation = startLocation;
        }
    }
}

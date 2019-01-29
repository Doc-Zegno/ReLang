using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Single identifier: name + location
    /// </summary>
    class SingleIdentifier : IIdentifier {
        public string Name { get; }
        public ITypeInfo ExpectedType { get; }
        public Location StartLocation { get; }


        public SingleIdentifier(string name, ITypeInfo expectedType, Location startLocation) {
            Name = name;
            ExpectedType = expectedType;
            StartLocation = startLocation;
        }
    }
}

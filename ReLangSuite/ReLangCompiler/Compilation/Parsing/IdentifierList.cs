using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// List of arbitrary identifiers
    /// </summary>
    class IdentifierList : IIdentifier {
        public Location StartLocation { get; }
        public List<IIdentifier> Identifiers { get; }


        public IdentifierList(List<IIdentifier> identifiers) {
            StartLocation = identifiers[0].StartLocation;
            Identifiers = identifiers;
        }
    }
}

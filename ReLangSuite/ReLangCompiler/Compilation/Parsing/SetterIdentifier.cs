using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    class SetterIdentifier : IIdentifier {
        public Location StartLocation { get; }
        public IFunctionDefinition FunctionDefinition { get; }
        public List<IExpression> Arguments { get; }


        public SetterIdentifier(IFunctionDefinition functionDefinition, List<IExpression> arguments, Location startLocation) {
            StartLocation = startLocation;
            FunctionDefinition = functionDefinition;
            Arguments = arguments;
        }
    }
}

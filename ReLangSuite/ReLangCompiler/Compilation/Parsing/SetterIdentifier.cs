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
        public IFunctionDefinition SetterDefinition { get; }
        public IFunctionDefinition GetterDefinition { get; }
        public List<IExpression> Arguments { get; }


        public SetterIdentifier(
            IFunctionDefinition setter,
            IFunctionDefinition getter,
            List<IExpression> arguments,
            Location startLocation) 
        {
            StartLocation = startLocation;
            SetterDefinition = setter;
            GetterDefinition = getter;
            Arguments = arguments;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class CustomFunctionDefinition : IFunctionDefinition {
        public FunctionSignature Signature { get; }
        public string ShortName => Signature.Name;
        public string FullQualification { get; }

        public int Number { get; }
        public bool IsGlobal { get; }


        public CustomFunctionDefinition(FunctionSignature signature, string fullQualification, int number, bool isGlobal) {
            Signature = signature;
            FullQualification = fullQualification;
            Number = number;
            IsGlobal = isGlobal;
        }
    }
}

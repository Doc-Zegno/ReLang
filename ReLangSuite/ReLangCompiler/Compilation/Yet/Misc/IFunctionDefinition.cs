using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    public interface IFunctionDefinition {
        FunctionSignature Signature { get; }
        string ShortName { get; }
        string FullQualification { get; }
    }
}

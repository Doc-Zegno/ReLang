using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    public interface IFunctionDefinition {
        List<ITypeInfo> ArgumentTypes { get; }
        ITypeInfo ResultType { get; }
        string Name { get; }
        string FullQualification { get; }
    }
}

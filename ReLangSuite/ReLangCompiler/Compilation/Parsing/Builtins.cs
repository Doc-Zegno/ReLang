using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    static class Builtins {
        public const string BuiltinNamespace = "ReLang";

        /// <summary>
        /// Definition of built-in function `print(object obj)`
        /// </summary>
        public static FunctionDefinition PrintDefinition =>
            new FunctionDefinition(PrimitiveTypeInfo.Void, new List<ITypeInfo> { PrimitiveTypeInfo.Object },
                                   -1, BuiltinNamespace, true);
    }
}

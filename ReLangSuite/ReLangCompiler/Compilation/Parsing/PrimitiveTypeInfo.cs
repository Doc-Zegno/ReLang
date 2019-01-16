using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Meta information about primitive types
    /// </summary>
    class PrimitiveTypeInfo : ITypeInfo {
        /// <summary>
        /// Supported primitive types
        /// </summary>
        public enum Option {
            Void,
            Bool,
            Int,
            Float,
            String,
            Object,
        }


        public Option TypeOption { get; }
        public string Name => TypeOption.ToString();


        public PrimitiveTypeInfo(Option typeOption) {
            TypeOption = typeOption;
        }

        public IExpression ConvertTo(ITypeInfo targetTypeInfo) {
            throw new NotImplementedException();
        }
    }
}

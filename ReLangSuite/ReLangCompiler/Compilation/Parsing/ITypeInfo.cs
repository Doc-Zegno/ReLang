using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Meta information about type of some expression
    /// </summary>
    interface ITypeInfo {
        /// <summary>
        /// Name of this type
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get expression that can be used for conversion
        /// from this type to the specified one
        /// </summary>
        /// <param name="targetTypeInfo">Target type</param>
        /// <returns>Converting expression if it exists and `null` otherwise</returns>
        IExpression ConvertTo(ITypeInfo targetTypeInfo);
    }
}

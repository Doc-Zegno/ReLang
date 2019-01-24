using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta information about type of some expression
    /// </summary>
    public interface ITypeInfo {
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
        IExpression ConvertTo(IExpression expression, ITypeInfo targetTypeInfo);

        /// <summary>
        /// Get an expression of current type that is constructed
        /// from given source expression (can be considered an explicit cast)
        /// **Note**: referencial types should construct a copy 
        /// </summary>
        /// <param name="expression">Source expression</param>
        /// <returns>Expression of target type constructed from a source one</returns>
        IExpression ConstructFrom(IExpression expression);
    }
}

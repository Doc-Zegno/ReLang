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
        /// Whether this type is referential or a value one
        /// </summary>
        bool IsReferential { get; }

        /// <summary>
        /// Whether a complete type could be deduced from this one
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Get a new instance of type where all generic entries are resolved to concrete types
        /// </summary>
        /// <returns>Resolved type or `null` if it's not possible</returns>
        ITypeInfo ResolveGeneric();

        /// <summary>
        /// Check if it's possible to upcast given source type to this one
        /// </summary>
        /// <param name="sourceType">Source type</param>
        /// <returns>`true` if upcasting is possible and `false` otherwise</returns>
        bool CanUpcast(ITypeInfo sourceType);

        /// <summary>
        /// Get expression of this type converted from given source expression
        /// </summary>
        /// <param name="expression">Source expression</param>
        /// <returns>Converted expression if it exists and `null` otherwise</returns>
        IExpression ConvertFrom(IExpression expression);

        /// <summary>
        /// Get an expression of current type that is constructed
        /// from given source expression (can be considered an explicit cast)
        /// **Note**: referencial types should construct a copy 
        /// </summary>
        /// <param name="expression">Source expression</param>
        /// <returns>Expression of target type constructed from a source one</returns>
        IExpression ConstructFrom(IExpression expression, Location location);

        /// <summary>
        /// Get the definition of a method with specified name
        /// </summary>
        /// <param name="name">Name of method</param>
        /// <returns>Definition of requested method</returns>
        IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable);
    }
}

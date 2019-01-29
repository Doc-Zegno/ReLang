using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Strongly typed expression (a code chunk that can be evaluated)
    /// </summary>
    public interface IExpression {
        /// <summary>
        /// Equals to `true` if evaluation of this expression **can** have a side effect 
        /// </summary>
        bool HasSideEffect { get; }

        /// <summary>
        /// Equals to `true` if this expression can be evaluated at compile-time
        /// </summary>
        bool IsCompileTime { get; }

        /// <summary>
        /// Value of expression if it's a compile-time one
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Information about type of expression
        /// </summary>
        ITypeInfo TypeInfo { get; }

        /// <summary>
        /// Whether this expression can be at the left side of assignment
        /// </summary>
        bool IsLvalue { get; }

        /// <summary>
        /// Location of the expression's main token
        /// </summary>
        Location MainLocation { get; }

        /// <summary>
        /// Change expression's type
        /// </summary>
        /// <param name="newType">New expression's type</param>
        /// <returns>Copy of this expression of new type</returns>
        IExpression ChangeType(ITypeInfo newType);
    }
}

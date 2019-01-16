using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Strongly typed expression (a code chunk that can be evaluated)
    /// </summary>
    interface IExpression {
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
    }
}

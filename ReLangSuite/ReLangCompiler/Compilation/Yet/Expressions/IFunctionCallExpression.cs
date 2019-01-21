using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a function call
    /// </summary>
    interface IFunctionCallExpression : IExpression {
        /// <summary>
        /// Arguments of function call
        /// </summary>
        List<IExpression> Arguments { get; }
    }
}

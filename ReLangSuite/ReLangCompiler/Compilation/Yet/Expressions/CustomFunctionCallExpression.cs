using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a custom function call
    /// </summary>
    class CustomFunctionCallExpression : IFunctionCallExpression {
        public List<IExpression> Arguments { get; }
        public bool HasSideEffect => true;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }

        /// <summary>
        /// Number of callable function in the program's function list
        /// </summary>
        public int Number { get; }


        public CustomFunctionCallExpression(ITypeInfo resultType, List<IExpression> arguments, int number) {
            TypeInfo = resultType;
            Arguments = arguments;
            Number = number;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a built-in function call
    /// </summary>
    class BuiltinFunctionCallExpression : IFunctionCallExpression {
        /// <summary>
        /// Possible built-ins
        /// </summary>
        public enum Option {
            Print,
            TupleGet,
        }


        public bool HasSideEffect => true;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }
        public List<IExpression> Arguments { get; }

        /// <summary>
        /// Built-in function that should be called
        /// </summary>
        public Option BuiltinOption { get; }


        public BuiltinFunctionCallExpression(ITypeInfo resultTypeInfo, List<IExpression> arguments, Option builtinOption) {
            TypeInfo = resultTypeInfo;
            Arguments = arguments;
            BuiltinOption = builtinOption;
        }
    }
}

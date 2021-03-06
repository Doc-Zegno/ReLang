﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a function call
    /// </summary>
    class FunctionCallExpression : IExpression {
        public bool HasSideEffect => true;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue { get; }
        public Location MainLocation { get; }

        /// <summary>
        /// Arguments of function call
        /// </summary>
        public List<IExpression> Arguments { get; }

        /// <summary>
        /// Definition of callable function
        /// </summary>
        public IFunctionDefinition FunctionDefinition { get; }


        public FunctionCallExpression(IFunctionDefinition functionDefinition, List<IExpression> arguments, ITypeInfo resultType,
                                      bool isLvalue, Location mainLocation)
        {
            FunctionDefinition = functionDefinition;
            Arguments = arguments;
            TypeInfo = resultType;
            IsLvalue = isLvalue;
            MainLocation = mainLocation;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (FunctionCallExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

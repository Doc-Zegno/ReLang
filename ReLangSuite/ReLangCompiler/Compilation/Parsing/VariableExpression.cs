﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Expression representing reference to a variable
    /// </summary>
    class VariableExpression : IExpression {
        /// <summary>
        /// Name of referenced variable
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Number of variable on stack
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// How many frames before this variable has been declared?
        /// (0 for current frame, -1 for the previous one)
        /// </summary>
        public int FrameOffset { get; }

        public bool HasSideEffect => false;
        public bool IsCompileTime { get; }
        public object Value { get; }
        public ITypeInfo TypeInfo { get; }


        public VariableExpression(string name, int number, int frameOffset, 
                                  bool isCompileTime, ITypeInfo typeInfo, object value = null) 
        {
            Name = name;
            Number = number;
            FrameOffset = frameOffset;
            IsCompileTime = isCompileTime;
            TypeInfo = typeInfo;
            Value = value;
        }
    }
}

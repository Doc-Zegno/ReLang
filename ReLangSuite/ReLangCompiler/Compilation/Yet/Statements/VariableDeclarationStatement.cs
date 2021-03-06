﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a declaration of variable
    /// </summary>
    class VariableDeclarationStatement : IDeclarationStatement {
        public string Name { get; }
        public ITypeInfo TypeInfo { get; }
        public IExpression Value { get; }
        public VariableQualifier Qualifier { get; }


        public VariableDeclarationStatement(string name, ITypeInfo typeInfo, IExpression value, VariableQualifier qualifier) {
            Name = name;
            TypeInfo = typeInfo;
            Value = value;
            Qualifier = qualifier;
        }
    }
}

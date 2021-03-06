﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    public class FunctionSignature {
        public string Name { get; }
        public List<string> ArgumentNames { get; }
        public List<ITypeInfo> ArgumentTypes { get; }
        public List<bool> ArgumentMutabilities { get; }
        public List<IExpression> ArgumentDefaultValues { get; }
        public ITypeInfo ResultType { get; }
        public bool ResultMutability { get; }


        public FunctionSignature(
            string name, 
            List<string> argumentNames, 
            List<ITypeInfo> argumentTypes, 
            List<bool> argumentMutabilities, 
            List<IExpression> argumentDefaultValues,
            ITypeInfo resultType, 
            bool resultMutability) 
        {
            Name = name;
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;
            ArgumentMutabilities = argumentMutabilities;
            ArgumentDefaultValues = argumentDefaultValues;
            ResultType = resultType;
            ResultMutability = resultMutability;
        }
    }
}

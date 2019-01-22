using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public class FunctionData {
        public string Name { get; }
        public string FullQualification { get; }
        public ITypeInfo ResultType { get; }
        public List<string> ArgumentNames { get; }
        public List<ITypeInfo> ArgumentTypes { get; }
        public List<IStatement> Body { get; }


        public FunctionData(string name, string fullQualification, ITypeInfo resultType, 
                            List<string> argumentNames, List<ITypeInfo> argumentTypes, List<IStatement> body)
        {
            Name = name;
            FullQualification = fullQualification;
            ResultType = resultType;
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;
            Body = body;
        }
    }


    public class ParsedProgram {
        public List<FunctionData> Functions { get; }
        public int MainFunctionNumber { get; }

        public ParsedProgram(List<FunctionData> functions, int mainFunctionNumber) {
            Functions = functions;
            MainFunctionNumber = mainFunctionNumber;
        }
    }
}

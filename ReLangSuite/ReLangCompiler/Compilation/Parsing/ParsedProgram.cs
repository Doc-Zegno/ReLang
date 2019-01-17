using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    class FunctionData {
        public string Name { get; }
        public string FullQualification { get; }
        public ITypeInfo ResultType { get; }
        public List<ITypeInfo> ArgumentTypes { get; }
        public List<IStatement> Body { get; }


        public FunctionData(string name, string fullQualification, ITypeInfo resultType, 
                            List<ITypeInfo> argumentTypes, List<IStatement> body)
        {
            Name = name;
            FullQualification = fullQualification;
            ResultType = resultType;
            ArgumentTypes = argumentTypes;
            Body = body;
        }
    }


    class ParsedProgram {
        public List<FunctionData> Functions { get; }
        public int MainFunctionNumber { get; }

        public ParsedProgram(List<FunctionData> functions, int mainFunctionNumber) {
            Functions = functions;
            MainFunctionNumber = mainFunctionNumber;
        }
    }
}

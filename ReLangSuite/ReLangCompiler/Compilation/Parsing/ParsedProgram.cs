using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public class FunctionData {
        public IFunctionDefinition Definition { get; }
        public List<string> ArgumentNames { get; }
        public List<IStatement> Body { get; }


        public FunctionData(IFunctionDefinition definition, List<string> argumentNames, List<IStatement> body) {
            Definition = definition;
            ArgumentNames = argumentNames;
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

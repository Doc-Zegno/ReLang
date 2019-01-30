using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public class FunctionData {
        public IFunctionDefinition Definition { get; }
        public List<IStatement> Body { get; }
        public bool IsProcedure { get; }


        public FunctionData(IFunctionDefinition definition, List<IStatement> body) {
            Definition = definition;
            Body = body;

            if (definition.Signature.ResultType is PrimitiveTypeInfo primitive
                && primitive.TypeOption == PrimitiveTypeInfo.Option.Void)
            {
                IsProcedure = true;
            } else {
                IsProcedure = false;
            }
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

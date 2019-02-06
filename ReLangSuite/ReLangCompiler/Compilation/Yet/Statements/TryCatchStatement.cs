using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing a try-catch block
    /// </summary>
    class TryCatchStatement : IStatement {
        public List<IStatement> TryBlock { get; }
        public List<(ErrorTypeInfo.Option, string, List<IStatement>)> CatchBlocks { get; }


        public TryCatchStatement(List<IStatement> tryBlock, List<(ErrorTypeInfo.Option, string, List<IStatement>)> catchBlocks) {
            TryBlock = tryBlock;
            CatchBlocks = catchBlocks;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Statement representing an assignment to a variable
    /// </summary>
    class AssignmentStatement : IStatement {
        /// <summary>
        /// Variable's name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Variable's number within its frame
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// Containing frame's offset
        /// </summary>
        public int FrameOffset { get; }

        /// <summary>
        /// Assigned value
        /// </summary>
        public IExpression Value { get; }


        public AssignmentStatement(string name, int number, int frameOffset, IExpression value) {
            Name = name;
            Number = number;
            FrameOffset = frameOffset;
            Value = value;
        }
    }
}

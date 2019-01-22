using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    /// <summary>
    /// Signalizes about internal errors caused by virtual machine
    /// </summary>
    [Serializable]
    public class VirtualMachineException : Exception {
        public VirtualMachineException() {
        }

        public VirtualMachineException(string message) : base(message) {
        }

        public VirtualMachineException(string message, Exception innerException) : base(message, innerException) {
        }

        protected VirtualMachineException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}

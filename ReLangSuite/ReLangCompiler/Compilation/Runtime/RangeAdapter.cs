using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    /// <summary>
    /// Class wrapper for ranges
    /// </summary>
    class RangeAdapter : IEnumerable<object> {
        /// <summary>
        /// Range's start (inclusive)
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Range's end (exclusive)
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Range's step
        /// </summary>
        public int Step { get; }


        public RangeAdapter(int start, int end, int step) {
            Start = start;
            End = end;
            Step = step;
        }


        public IEnumerator<object> GetEnumerator() {
            for (var i = Start; i < End; i += Step) {
                yield return i;
            }
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}

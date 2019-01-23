using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    /// <summary>
    /// Adapter for tuple data type
    /// </summary>
    class TupleAdapter : IEquatable<TupleAdapter> {
        /// <summary>
        /// Tuple's items
        /// </summary>
        public object[] Items { get; }


        public TupleAdapter(object[] items) {
            Items = items;
        }


        public override bool Equals(object obj) {
            if (obj is TupleAdapter tuple) {
                return Equals(tuple);
            } else {
                return false;
            }
        }


        public bool Equals(TupleAdapter other) {
            if (Items.Length != other.Items.Length) {
                return false;
            }

            for (var i = 0; i < Items.Length; i++) {
                if (!Items[i].Equals(other.Items[i])) {
                    return false;
                }
            }

            return true;
        }


        public override int GetHashCode() {
            return -604923257 + EqualityComparer<object[]>.Default.GetHashCode(Items);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    /// <summary>
    /// VM's adapter for dictionary
    /// </summary>
    class DictionaryAdapter : IEnumerable<object> {
        private Dictionary<object, object> dictionary;

        /// <summary>
        /// Internal dictionary converted to a sequence of tuples
        /// </summary>
        public IEnumerable<object> Pairs => dictionary.Select(ConvertToTuple);


        public DictionaryAdapter(IEnumerable<(object, object)> pairs) {
            dictionary = new Dictionary<object, object>();
            foreach (var (key, value) in pairs) {
                dictionary[key] = value;
            }
        }


        public IEnumerator<object> GetEnumerator() {
            foreach (var pair in dictionary) {
                yield return ConvertToTuple(pair);
            }
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        private object ConvertToTuple(KeyValuePair<object, object> pair) {
            var items = new object[] { pair.Key, pair.Value };
            return new TupleAdapter(items);
        }
    }
}

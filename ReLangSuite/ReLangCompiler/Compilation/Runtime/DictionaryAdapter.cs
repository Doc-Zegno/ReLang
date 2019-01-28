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
    class DictionaryAdapter : IEnumerable<object>, ICloneable {
        private Dictionary<object, object> dictionary;

        /// <summary>
        /// Internal dictionary converted to a sequence of tuples
        /// </summary>
        public IEnumerable<object> Pairs => dictionary.Select(ConvertToTuple);

        public int Count => dictionary.Count;

        public object this[object key] {
            get { return dictionary[key]; }
            set { dictionary[key] = value; }
        }


        public DictionaryAdapter(IEnumerable<(object, object)> pairs) {
            dictionary = new Dictionary<object, object>();
            foreach (var (key, value) in pairs) {
                dictionary[key] = value;
            }
        }


        public bool TryGetValue(object key, out object value) {
            return dictionary.TryGetValue(key, out value);
        }


        public bool ContainsKey(object key) {
            return dictionary.ContainsKey(key);
        }


        public IEnumerator<object> GetEnumerator() {
            foreach (var pair in dictionary) {
                yield return ConvertToTuple(pair);
            }
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        public object Clone() {
            return new DictionaryAdapter(new Dictionary<object, object>(dictionary));
        }


        private DictionaryAdapter(Dictionary<object, object> dictionary) {
            this.dictionary = dictionary;
        }


        private object ConvertToTuple(KeyValuePair<object, object> pair) {
            var items = new object[] { pair.Key, pair.Value };
            return new TupleAdapter(items);
        }
    }
}

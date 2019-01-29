using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    /// <summary>
    /// VM's adapter for list
    /// </summary>
    class ListAdapter : IEnumerable<object>, ICloneable {
        private List<object> list;

        public int Start { get; }
        public int End { get; private set; }
        public int Step { get; }
        public bool IsSlice { get; }
        public int Count => (End - Start) / Step;

        public object this[int index] {
            get => list[index];
            set {
                list[index] = value;
            }
        }


        public ListAdapter() {
            list = new List<object>();

            Start = 0;
            Step = 1;
            End = 0;
            IsSlice = false;
        }


        public ListAdapter(IEnumerable<object> items) {
            list = new List<object>(items);

            Start = 0;
            Step = 1;
            End = list.Count;
            IsSlice = false;
        }


        public void Add(object item) {
            if (!IsSlice) {
                list.Add(item);
                End++;
            } else {
                throw new NotSupportedException("Append is not supported for slices");
            }
        }


        public void Extend(ListAdapter items) {
            if (!IsSlice) {
                list.AddRange(items.list);
                End += items.Count;
            } else {
                throw new NotSupportedException("Extend is not supported for slices");
            }
        }


        public bool Contains(object item) {
            return list.Contains(item);
        }


        public ListAdapter GetSlice(int start, int end, int step = 1) {
            return new ListAdapter(list, Start + start * Step, Start + end * Step, Step * step, true);
        }


        public IEnumerator<object> GetEnumerator() {
            for (var i = Start; i < End; i += Step) {
                yield return list[i];
            }
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        public object Clone() {
            return new ListAdapter(this);
        }


        private ListAdapter(List<object> list, int start, int end, int step, bool isSlice) {
            this.list = list;
            Start = start;
            End = end;
            Step = step;
            IsSlice = isSlice;
        }
    }
}

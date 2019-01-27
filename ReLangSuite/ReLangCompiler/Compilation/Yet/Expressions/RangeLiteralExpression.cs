using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a range literal (0..10)
    /// </summary>
    class RangeLiteralExpression : ILiteralExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo => new RangeTypeInfo(Start.TypeInfo);
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        /// <summary>
        /// Interval's start (inclusive)
        /// </summary>
        public IExpression Start { get; }

        /// <summary>
        /// Interval's end (exclusive)
        /// </summary>
        public IExpression End { get; }


        public RangeLiteralExpression(IExpression start, IExpression end) {
            Start = start;
            End = end;

            MainLocation = start.MainLocation;
            HasSideEffect = start.HasSideEffect || end.HasSideEffect;
        }
    }
}

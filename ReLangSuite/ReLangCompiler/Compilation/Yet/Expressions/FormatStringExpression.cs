using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class FormatStringExpression : IExpression {
        public bool HasSideEffect { get; }
        public bool IsCompileTime { get; }
        public object Value { get; }
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public List<string> Pieces { get; }
        public List<IExpression> Expressions { get; }


        public FormatStringExpression(List<string> pieces, List<IExpression> expressions, Location mainLocation) {
            Pieces = pieces;
            Expressions = expressions;
            MainLocation = mainLocation;
            TypeInfo = PrimitiveTypeInfo.String;

            if (expressions.Count > 0) {
                HasSideEffect = false;
                IsCompileTime = false;
                Value = null;

                foreach (var expression in expressions) {
                    if (expression.HasSideEffect) {
                        HasSideEffect = true;
                    }
                }

            } else {
                HasSideEffect = false;
                IsCompileTime = true;
                Value = pieces[0];
            }
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (FormatStringExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

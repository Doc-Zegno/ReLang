using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing error object literal
    /// </summary>
    class ErrorLiteralExpression : ILiteralExpression {
        public bool HasSideEffect => Description.HasSideEffect;
        public bool IsCompileTime => false;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }

        public IExpression Description { get; }
        public ErrorTypeInfo.Option ErrorOption { get; }


        public ErrorLiteralExpression(ErrorTypeInfo.Option errorOption, IExpression description, Location location) {
            TypeInfo = new ErrorTypeInfo(errorOption);
            MainLocation = location;
            Description = description;
            ErrorOption = errorOption;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            throw new NotImplementedException();
        }
    }
}

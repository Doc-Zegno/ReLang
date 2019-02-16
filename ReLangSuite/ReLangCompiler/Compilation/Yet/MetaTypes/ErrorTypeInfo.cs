using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta information about program error's type
    /// </summary>
    class ErrorTypeInfo : ITypeInfo {
        /// <summary>
        /// All supported errors
        /// </summary>
        public enum Option {
            None,
            RangeError,
            KeyError,
            ValueError,
            FormatError,
            ZeroDivisionError,
            NullError,
            NoReturnValueError,
            NotSupportedError,
            IOError,
            Error,
        }


        public string Name => ErrorOption.ToString();
        public bool IsReferential => true;
        public bool IsComplete => true;
        public Option ErrorOption { get; }


        public ErrorTypeInfo(Option errorOption) {
            ErrorOption = errorOption;
        }


        public ITypeInfo ResolveGeneric() {
            return this;
        }


        public bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            return ConvertFrom(expression);
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (expression.TypeInfo is ErrorTypeInfo errorType
                && (errorType.ErrorOption == ErrorOption || ErrorOption == Option.Error))
            {
                return expression.ChangeType(this);
            } else {
                return null;
            }
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "getMessage":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ErrorGetMessage,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        new List<IExpression> { null },
                        PrimitiveTypeInfo.String);

                case "getStackTrace":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.ErrorGetStackTrace,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { false },
                        new List<IExpression> { null },
                        PrimitiveTypeInfo.String);

                default:
                    return null;
            }
        }


        public override bool Equals(object obj) {
            return obj is IncompleteTypeInfo || obj is ErrorTypeInfo errorType && errorType.ErrorOption == ErrorOption;
        }


        public override int GetHashCode() {
            var hashCode = -230602238;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + IsReferential.GetHashCode();
            hashCode = hashCode * -1521134295 + ErrorOption.GetHashCode();
            return hashCode;
        }
    }
}

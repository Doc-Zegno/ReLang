using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Type information about a functional object
    /// </summary>
    class FunctionTypeInfo : ITypeInfo {
        public string Name {
            get {
                var builder = new StringBuilder("(");
                var isFirst = true;

                for (var i = 0; i < ArgumentTypes.Count; i++) {
                    if (!isFirst) {
                        builder.Append(", ");
                    }
                    isFirst = false;
                    if (ArgumentMutabilities[i]) {
                        builder.Append("mutable ");
                    }
                    builder.Append(ArgumentTypes[i].Name);
                }

                builder.Append(") -> ");
                if (!ResultMutability) {
                    builder.Append("const ");
                }
                builder.Append(ResultType.Name);
                return builder.ToString();
            }
        }
        
        public bool IsReferential => true;
        public bool IsComplete { get; }

        /// <summary>
        /// Types of function's arguments
        /// </summary>
        public List<ITypeInfo> ArgumentTypes { get; }

        /// <summary>
        /// Mutabilities of function's arguments
        /// </summary>
        public List<bool> ArgumentMutabilities { get; }

        /// <summary>
        /// Type of function's result
        /// </summary>
        public ITypeInfo ResultType { get; }

        /// <summary>
        /// Mutability of resulting value
        /// </summary>
        public bool ResultMutability { get; }


        public FunctionTypeInfo(List<ITypeInfo> argumentTypes, List<bool> argumentMutabilities,
                                ITypeInfo resultType, bool resultMutability)
        {
            ArgumentTypes = argumentTypes;
            ArgumentMutabilities = argumentMutabilities;
            ResultType = resultType;
            ResultMutability = resultMutability;

            IsComplete = true;
            foreach (var argumentType in argumentTypes) {
                if (!argumentType.IsComplete) {
                    IsComplete = false;
                    break;
                }
            }
            if (!resultType.IsComplete) {
                IsComplete = false;
            }
        }


        public IExpression GetDefaultValue(Location location) => null;


        public ITypeInfo ResolveGeneric() {
            var resolvedResultType = ResultType.ResolveGeneric();
            if (resolvedResultType == null) {
                return null;
            }

            var resolvedArgumentTypes = new List<ITypeInfo>();
            foreach (var argumentType in ArgumentTypes) {
                var resolvedArgumentType = argumentType.ResolveGeneric();
                if (resolvedArgumentType == null) {
                    return null;
                }
                resolvedArgumentTypes.Add(resolvedArgumentType);
            }

            return new FunctionTypeInfo(resolvedArgumentTypes, ArgumentMutabilities, resolvedResultType, ResultMutability);
        }


        public bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public IExpression ConstructFrom(IExpression expression, Location location) {
            //return ConvertFrom(expression);
            return null;
        }


        public IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression.ChangeType(this); 
            } else {
                return null;
            }
        }


        public override bool Equals(object obj) {
            if (obj is IncompleteTypeInfo) {
                return true;
            } else if (obj is FunctionTypeInfo functionType) {
                if (!ResultType.Equals(functionType.ResultType) || ResultMutability != functionType.ResultMutability) {
                    return false;
                }

                if (ArgumentTypes.Count != functionType.ArgumentTypes.Count) {
                    return false;
                }

                for (var i = 0; i < ArgumentTypes.Count; i++) {
                    if (!ArgumentTypes[i].Equals(functionType.ArgumentTypes[i])
                        || ArgumentMutabilities[i] != functionType.ArgumentMutabilities[i])
                    {
                        return false;
                    }
                }

                return true;

            } else {
                return false;
            }
        }


        public override int GetHashCode() {
            var hashCode = -1113126183;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<ITypeInfo>>.Default.GetHashCode(ArgumentTypes);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITypeInfo>.Default.GetHashCode(ResultType);
            return hashCode;
        }


        public IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            return null;
        }
    }
}

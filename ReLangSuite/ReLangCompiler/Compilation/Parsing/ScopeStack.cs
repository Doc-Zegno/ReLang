using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    class VariableDefinition {
        public ITypeInfo TypeInfo { get; }
        public bool IsFinal { get; }
        public bool IsMutable { get; }
        public int Number { get; }
        public int ScopeNumber { get; }
        public IExpression Value { get; }


        public VariableDefinition(ITypeInfo typeInfo, bool isFinal, bool isMutable, int number, int scopeNumber, IExpression value) {
            TypeInfo = typeInfo;
            IsFinal = isFinal;
            IsMutable = isMutable;
            Number = number;
            ScopeNumber = scopeNumber;
            Value = value;
        }
    }


    class ScopeStack {
        private class Scope {
            private IDictionary<string, VariableDefinition> table;

            public int Number { get; }
            public Scope Parent { get; }
            public bool IsFunction { get; }
            public bool IsLoop { get; }

            public bool IsInsideLoop {
                get {
                    var scope = this;
                    while (scope != null) {
                        if (scope.IsLoop) {
                            return true;
                        }

                        if (!scope.IsFunction) {
                            scope = scope.Parent;
                        } else {
                            break;
                        }
                    }
                    return false;
                }
            }    


            public Scope(int number, Scope parent, bool isFunction, bool isLoop) {
                table = new Dictionary<string, VariableDefinition>();
                Number = number;
                Parent = parent;
                IsFunction = isFunction;
                IsLoop = isLoop;
            }


            public bool DeclareVariable(string name, ITypeInfo typeInfo, bool isFinal, bool isMutable, IExpression value) {
                var scope = this;
                while (scope != null) {
                    if (scope.table.ContainsKey(name)) {
                        return false;
                    }

                    if (!scope.IsFunction) {
                        scope = scope.Parent;
                    } else {
                        break;
                    }
                }

                // OK! No collisions
                var number = table.Count;
                table[name] = new VariableDefinition(typeInfo, isFinal, isMutable, number, Number, value);
                return true;
            }


            public VariableDefinition GetDefinition(string name) {
                var scope = this;
                while (scope != null) {
                    if (scope.table.TryGetValue(name, out VariableDefinition definition)) {
                        return definition;
                    } else {
                        if (!scope.IsFunction) {
                            scope = scope.Parent;
                        } else {
                            return null;
                        }
                    }
                }
                return null;
            }
        }


        private Scope top;

        public int Count { get; private set; }
        public bool IsInsideLoop => top.IsInsideLoop;


        public ScopeStack() {
            top = new Scope(0, null, true, false);
            Count = 1;
        }


        public void EnterScope(bool isFunction, bool isLoop) {
            var scope = new Scope(Count, top, isFunction, isLoop);
            top = scope;
            Count++;
        }


        public void LeaveScope() {
            top = top.Parent;
            Count--;
        }


        public bool DeclareVariable(string name, ITypeInfo typeInfo, bool isFinal, bool isMutable, IExpression value) {
            return top.DeclareVariable(name, typeInfo, isFinal, isMutable, value);
        }


        public VariableDefinition GetDefinition(string name) {
            return top.GetDefinition(name);
        }
    }
}

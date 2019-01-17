﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    struct VariableDefinition {
        public ITypeInfo TypeInfo { get; }
        public bool IsMutable { get; }
        public int Number { get; }
        public int ScopeNumber { get; }


        public VariableDefinition(ITypeInfo typeInfo, bool isMutable, int number, int scopeNumber) {
            TypeInfo = typeInfo;
            IsMutable = isMutable;
            Number = number;
            ScopeNumber = scopeNumber;
        }
    }


    class ScopeStack {
        private class Scope {
            private IDictionary<string, VariableDefinition> table;

            public int Number { get; }
            public Scope Parent { get; }
            public bool IsStrong { get; }


            public Scope(int number, Scope parent, bool isStrong) {
                table = new Dictionary<string, VariableDefinition>();
                Number = number;
                Parent = parent;
                IsStrong = isStrong;
            }


            public bool DeclareVariable(string name, ITypeInfo typeInfo, bool isMutable) {
                var scope = this;
                while (scope != null) {
                    if (scope.table.ContainsKey(name)) {
                        return false;
                    }

                    if (!scope.IsStrong) {
                        scope = scope.Parent;
                    } else {
                        break;
                    }
                }

                // OK! No collisions
                var number = table.Count;
                table[name] = new VariableDefinition(typeInfo, isMutable, number, Number);
                return true;
            }


            public VariableDefinition? GetDefinition(string name) {
                var scope = this;
                while (scope != null) {
                    if (scope.table.TryGetValue(name, out VariableDefinition definition)) {
                        return definition;
                    } else {
                        if (!scope.IsStrong) {
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


        public ScopeStack() {
            top = new Scope(0, null, true);
            Count = 1;
        }


        public void EnterScope(bool isStrong) {
            var scope = new Scope(Count, top, isStrong);
            top = scope;
            Count++;
        }


        public void LeaveScope() {
            top = top.Parent;
            Count--;
        }


        public bool DeclareVariable(string name, ITypeInfo typeInfo, bool isMutable) {
            return top.DeclareVariable(name, typeInfo, isMutable);
        }


        public VariableDefinition? GetDefinition(string name) {
            return top.GetDefinition(name);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    struct FunctionDefinition {
        public ITypeInfo ResultType { get; }
        public List<ITypeInfo> ArgumentTypes { get; }
        public int Number { get; }
        public string FullQualification { get; }
        public bool IsGlobal { get; }


        public FunctionDefinition(ITypeInfo resultType, List<ITypeInfo> argumentTypes,
                                  int number, string fullQualification, bool isGlobal)
        {
            ResultType = resultType;
            ArgumentTypes = argumentTypes;
            Number = number;
            FullQualification = fullQualification;
            IsGlobal = isGlobal;
        }
    }


    class FunctionTree {
        class Scope {
            private IDictionary<string, (FunctionDefinition, Scope)> table;
            private FunctionTree tree;

            public string Name { get; }
            public Scope Parent { get; }
            public bool IsGlobal { get; }


            public Scope(string name, Scope parent, bool isGlobal, FunctionTree tree) {
                table = new Dictionary<string, (FunctionDefinition, Scope)>();
                this.tree = tree;

                Name = name;
                Parent = parent;
                IsGlobal = isGlobal;
            }


            public Scope DeclareFunction(string name, ITypeInfo resultType, List<ITypeInfo> argumentTypes) {
                if (name != Name && !table.ContainsKey(name)) {
                    var scope = new Scope(name, this, false, tree);
                    var definition = new FunctionDefinition(resultType, argumentTypes, tree.Count,
                                                            GetFullQualification(), IsGlobal);
                    table[name] = (definition, scope);
                    tree.Count++;
                    return scope;
                } else {
                    return null;
                }
            }


            public Scope GetFunctionScope(string name) {
                var (_, scope) = table[name];
                return scope;
            }


            public FunctionDefinition? GetFunctionDefinition(string name) {
                var scope = this;
                do {
                    Console.WriteLine($"searching for '{name}' within {scope.GetFullQualification()}...");
                    if (scope.table.TryGetValue(name, out (FunctionDefinition, Scope) value)) {
                        return value.Item1;
                    } else {
                        scope = scope.Parent;
                    }
                } while (scope != null);
                return null;
            }


            private string GetFullQualification() {
                var names = new List<string>();
                var scope = this;
                while (scope != null) {
                    if (scope.Name.Length > 0) {
                        names.Add(scope.Name);
                    }
                    scope = scope.Parent;
                }
                names.Reverse();
                return string.Join(".", names);
            }


            public void PrintScope(int shift) {
                var padding = new string(' ', 4 * shift);
                foreach (var pair in table) {
                    var name = pair.Key;
                    var (definition, scope) = pair.Value;
                    Console.WriteLine($"{padding}func {definition.FullQualification}.{name}<{definition.Number}>() -> {definition.ResultType.Name} {{");
                    scope.PrintScope(shift + 1);
                    Console.WriteLine(padding + "}");
                }
            }
        }


        private Scope top;

        public int Count { get; private set; }


        public FunctionTree() {
            top = new Scope("", null, true, this);
        }


        public bool DeclareFunction(string name, ITypeInfo resultType, List<ITypeInfo> argumentTypes) {
            var scope = top.DeclareFunction(name, resultType, argumentTypes);
            if (scope != null) {
                top = scope;
                return true;
            } else {
                return false;
            }
        }


        public void EnterScope(string name) {
            top = top.GetFunctionScope(name);
        }


        public void LeaveScope() {
            top = top.Parent;
        }


        public FunctionDefinition? GetFunctionDefinition(string name) {
            return top.GetFunctionDefinition(name);
        }


        public FunctionDefinition GetCurrentFunctionDefinition() {
            return top.GetFunctionDefinition(top.Name).Value;
        }


        public void PrintTree() {
            top.PrintScope(0);
        }
    }
}

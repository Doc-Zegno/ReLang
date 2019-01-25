using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    class FunctionTree {
        class Scope {
            private IDictionary<string, (CustomFunctionDefinition, Scope)> table;
            private FunctionTree tree;

            public string Name { get; }
            public Scope Parent { get; }
            public bool IsGlobal { get; }


            public Scope(string name, Scope parent, bool isGlobal, FunctionTree tree) {
                table = new Dictionary<string, (CustomFunctionDefinition, Scope)>();
                this.tree = tree;

                Name = name;
                Parent = parent;
                IsGlobal = isGlobal;
            }


            public Scope DeclareFunction(string name, ITypeInfo resultType, List<ITypeInfo> argumentTypes) {
                if (name != Name && !table.ContainsKey(name)) {
                    var scope = new Scope(name, this, false, tree);
                    var definition = new CustomFunctionDefinition(argumentTypes, resultType, name,
                                                                  GetFullQualification(), tree.Count, IsGlobal);
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


            public CustomFunctionDefinition GetFunctionDefinition(string name) {
                var scope = this;
                do {
                    Console.WriteLine($"searching for '{name}' within {scope.GetFullQualification()}...");
                    if (scope.table.TryGetValue(name, out (CustomFunctionDefinition, Scope) value)) {
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
            top = new Scope("UserPackage", null, true, this);
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


        public CustomFunctionDefinition GetFunctionDefinition(string name) {
            return top.GetFunctionDefinition(name);
        }


        public CustomFunctionDefinition GetCurrentFunctionDefinition() {
            return top.GetFunctionDefinition(top.Name);
        }


        public void PrintTree() {
            top.PrintScope(0);
        }
    }
}

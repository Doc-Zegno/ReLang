using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class CustomFunctionDefinition : IFunctionDefinition {
        public ITypeInfo ResultType { get; }
        public List<ITypeInfo> ArgumentTypes { get; }
        public string FullName { get; }
        public string ShortName => FullName;
        public string FullQualification { get; }

        public int Number { get; }
        public bool IsGlobal { get; }


        public CustomFunctionDefinition(List<ITypeInfo> argumentTypes, ITypeInfo resultType, string name,
                                        string fullQualification, int number, bool isGlobal)
        {
            ArgumentTypes = argumentTypes;
            ResultType = resultType;
            FullName = name;
            FullQualification = fullQualification;
            Number = number;
            IsGlobal = isGlobal;
        }
    }
}

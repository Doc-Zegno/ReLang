using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class BuiltinFunctionDefinition : IFunctionDefinition {
        public enum Option {
            Print,
            TupleGet,
            TupleFirstGet,
            TupleSecondGet,
            TupleThirdGet,
            ListGet,
            ListLengthGet,
            SetLengthGet,
            DictionaryLengthGet,
        }


        public List<ITypeInfo> ArgumentTypes { get; }
        public ITypeInfo ResultType { get; }
        public string Name { get; }
        public string FullQualification => "ReLang";
        
        public Option BuiltinOption { get; }


        public BuiltinFunctionDefinition(Option builtinOption, List<ITypeInfo> argumentTypes, ITypeInfo resultType) {
            BuiltinOption = builtinOption;
            ArgumentTypes = argumentTypes;
            ResultType = resultType;

            var name = builtinOption.ToString();
            Name = char.ToLower(name[0]) + name.Substring(1);
        }


        public static BuiltinFunctionDefinition Print =>
            new BuiltinFunctionDefinition(
                Option.Print, 
                new List<ITypeInfo> { PrimitiveTypeInfo.Object }, 
                PrimitiveTypeInfo.Void);
    }
}

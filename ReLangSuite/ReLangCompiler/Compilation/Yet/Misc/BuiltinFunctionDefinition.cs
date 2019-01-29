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
            TupleGetFirst,
            TupleGetSecond,
            TupleGetThird,

            IterableContains,

            ListGet,
            ListSet,
            ListGetLength,
            ListAppend,
            ListExtend,
            ListContains,
            ListCopy,

            SetGetLength,
            SetAdd,
            SetRemove,
            SetUnion,
            SetIntersection,
            SetDifference,
            SetContains,
            SetCopy,

            DictionaryGet,
            DictionarySet,
            DictionaryTryGet,
            DictionaryGetLength,
            DictionaryContains,
            DictionaryCopy,

            RangeContains,
        }


        public List<ITypeInfo> ArgumentTypes { get; }
        public ITypeInfo ResultType { get; }
        public string FullName { get; }
        public string ShortName { get; }
        public string FullQualification => "ReLang";
        
        public Option BuiltinOption { get; }


        public BuiltinFunctionDefinition(string shortName, Option builtinOption, List<ITypeInfo> argumentTypes, ITypeInfo resultType) {
            BuiltinOption = builtinOption;
            ArgumentTypes = argumentTypes;
            ResultType = resultType;

            var name = builtinOption.ToString();
            FullName = char.ToLower(name[0]) + name.Substring(1);
            ShortName = shortName;
        }


        public static BuiltinFunctionDefinition Print =>
            new BuiltinFunctionDefinition(
                "print",
                Option.Print, 
                new List<ITypeInfo> { PrimitiveTypeInfo.Object }, 
                PrimitiveTypeInfo.Void);
    }
}

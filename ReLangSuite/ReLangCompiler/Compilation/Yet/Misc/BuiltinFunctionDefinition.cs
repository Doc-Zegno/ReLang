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
            ListGetSlice,
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

            StringGetLength,
            StringGetSlice,
            StringToLower,
            StringToUpper,
            StringSplit,
            StringContains,
            StringJoin,
            StringReversed,
            StringFind,
            StringFindLast,

            RangeContains,
        }


        public FunctionSignature Signature { get; }
        public string ShortName { get; }
        public string FullQualification => "ReLang";
        
        public Option BuiltinOption { get; }


        public BuiltinFunctionDefinition(
            string shortName, 
            Option builtinOption, 
            List<string> argumentNames, 
            List<ITypeInfo> argumentTypes,
            List<bool> argumentMutabilities, 
            ITypeInfo resultType, 
            bool resultMutability = true)
        {
            var name = builtinOption.ToString();
            var fullName = char.ToLower(name[0]) + name.Substring(1);
            Signature = new FunctionSignature(fullName, argumentNames, argumentTypes, argumentMutabilities, resultType, resultMutability);
            BuiltinOption = builtinOption;
            ShortName = shortName;
        }


        public static BuiltinFunctionDefinition Print =>
            new BuiltinFunctionDefinition(
                "print",
                Option.Print, 
                new List<string> { "object" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Object },
                new List<bool> { false },
                PrimitiveTypeInfo.Void);
    }
}

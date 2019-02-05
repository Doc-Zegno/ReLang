using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    class BuiltinFunctionDefinition : IFunctionDefinition {
        public enum Option {
            Print,
            Enumerate,
            Zip,

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

            StringGet,
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
            StringEndsWith,
            StringStartsWith,

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


        public static BuiltinFunctionDefinition CreateEnumerate(bool areItemsMutable) {
            var itemType = new GenericTypeInfo("T", new Dictionary<string, ITypeInfo>());
            var argumentType = new IterableTypeInfo(itemType);
            var tupleType = new TupleTypeInfo(new List<ITypeInfo> { PrimitiveTypeInfo.Int, itemType });
            var resultType = new IterableTypeInfo(tupleType);

            return new BuiltinFunctionDefinition(
                "enumerate",
                Option.Enumerate,
                new List<string> { "items" },
                new List<ITypeInfo> { argumentType },
                new List<bool> { false },
                resultType,
                areItemsMutable);    
        }
        

        public static BuiltinFunctionDefinition CreateZip(bool areItemsMutable) {
            var table = new Dictionary<string, ITypeInfo>();
            var firstItemType = new GenericTypeInfo("T", table);
            var secondItemType = new GenericTypeInfo("E", table);
            var firstArgumentType = new IterableTypeInfo(firstItemType);
            var secondArgumentType = new IterableTypeInfo(secondItemType);
            var tupleType = new TupleTypeInfo(new List<ITypeInfo> { firstItemType, secondItemType });
            var resultType = new IterableTypeInfo(tupleType);

            return new BuiltinFunctionDefinition(
                "zip",
                Option.Zip,
                new List<string> { "itemsA", "itemsB" },
                new List<ITypeInfo> { firstArgumentType, secondArgumentType },
                new List<bool> { false, false },
                resultType,
                areItemsMutable);
        }
    }
}

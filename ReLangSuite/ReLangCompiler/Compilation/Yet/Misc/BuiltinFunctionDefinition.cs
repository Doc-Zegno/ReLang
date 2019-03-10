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
            Open,
            Maxi,
            Maxf,
            Mini,
            Minf,

            TupleGet,
            TupleGetFirst,
            TupleGetSecond,
            TupleGetThird,

            IterableContains,

            ListInit,
            ListGet,
            ListSet,
            ListGetLength,
            ListGetSlice,
            ListAppend,
            ListExtend,
            ListContains,
            ListCopy,

            //SetInit,
            SetGetLength,
            SetAdd,
            SetRemove,
            SetUnion,
            SetIntersection,
            SetDifference,
            SetContains,
            SetCopy,

            //DictionaryInit,
            DictionaryGet,
            DictionarySet,
            DictionaryTryGet,
            DictionaryGetLength,
            DictionaryContains,
            DictionaryCopy,

            StringInit,
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

            RangeInit,
            RangeContains,

            FileReadLine,
            FileWrite,
            FileReset,
            FileClose,

            ErrorGetMessage,
            ErrorGetStackTrace,
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
            List<IExpression> argumentDefaultValues,
            ITypeInfo resultType, 
            bool resultMutability = true)
        {
            var name = builtinOption.ToString();
            var fullName = char.ToLower(name[0]) + name.Substring(1);
            Signature = new FunctionSignature(fullName, argumentNames, argumentTypes, argumentMutabilities,
                                              argumentDefaultValues, resultType, resultMutability);
            BuiltinOption = builtinOption;
            ShortName = shortName;
        }


        public static BuiltinFunctionDefinition Print =>
            new BuiltinFunctionDefinition(
                "print",
                Option.Print, 
                new List<string> { "object", "end" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Object, PrimitiveTypeInfo.String },
                new List<bool> { false, false },
                new List<IExpression> {
                    new PrimitiveLiteralExpression("", PrimitiveTypeInfo.Object, null),
                    new PrimitiveLiteralExpression("\n", PrimitiveTypeInfo.String, null)
                },
                PrimitiveTypeInfo.Void);


        public static BuiltinFunctionDefinition Open =>
            new BuiltinFunctionDefinition(
                "open",
                Option.Open,
                new List<string> { "path", "mode" },
                new List<ITypeInfo> { PrimitiveTypeInfo.String, PrimitiveTypeInfo.String },
                new List<bool> { false, false },
                new List<IExpression> {
                    null,
                    new PrimitiveLiteralExpression("r", PrimitiveTypeInfo.String, null)
                },
                new FileStreamTypeInfo());


        public static BuiltinFunctionDefinition Maxi =>
            new BuiltinFunctionDefinition(
                "maxi",
                Option.Maxi,
                new List<string> { "x", "y" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Int, PrimitiveTypeInfo.Int },
                new List<bool> { false, false },
                new List<IExpression> { null, null },
                PrimitiveTypeInfo.Int);


        public static BuiltinFunctionDefinition Mini =>
            new BuiltinFunctionDefinition(
                "mini",
                Option.Mini,
                new List<string> { "x", "y" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Int, PrimitiveTypeInfo.Int },
                new List<bool> { false, false },
                new List<IExpression> { null, null },
                PrimitiveTypeInfo.Int);


        public static BuiltinFunctionDefinition Maxf =>
            new BuiltinFunctionDefinition(
                "maxf",
                Option.Maxf,
                new List<string> { "x", "y" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Float, PrimitiveTypeInfo.Float },
                new List<bool> { false, false },
                new List<IExpression> { null, null },
                PrimitiveTypeInfo.Float);


        public static BuiltinFunctionDefinition Minf =>
            new BuiltinFunctionDefinition(
                "minf",
                Option.Minf,
                new List<string> { "x", "y" },
                new List<ITypeInfo> { PrimitiveTypeInfo.Float, PrimitiveTypeInfo.Float },
                new List<bool> { false, false },
                new List<IExpression> { null, null },
                PrimitiveTypeInfo.Float);


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
                new List<IExpression> { null },
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
                new List<IExpression> { null, null },
                resultType,
                areItemsMutable);
        }
    }
}

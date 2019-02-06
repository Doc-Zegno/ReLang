﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Meta information about file stream
    /// </summary>
    class FileStreamTypeInfo : IterableTypeInfo {
        public override string Name => "FileStream";


        public FileStreamTypeInfo() : base(PrimitiveTypeInfo.String) { }


        public override ITypeInfo ResolveGeneric() {
            return this;
        }


        public override bool CanUpcast(ITypeInfo sourceType) {
            return Equals(sourceType);
        }


        public override IExpression ConstructFrom(IExpression expression, Location location) {
            return ConvertFrom(expression);
        }


        public override IExpression ConvertFrom(IExpression expression) {
            if (Equals(expression.TypeInfo)) {
                return expression;
            } else {
                return null;
            }
        }


        public override IFunctionDefinition GetMethodDefinition(string name, bool isSelfMutable) {
            switch (name) {
                case "reset":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.FileReset,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { true },
                        PrimitiveTypeInfo.Void);

                /*case "readLine":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.FileReadLine,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { true },
                        new MaybeTypeInfo(PrimitiveTypeInfo.String));*/

                case "close":
                    return new BuiltinFunctionDefinition(
                        name,
                        BuiltinFunctionDefinition.Option.FileClose,
                        new List<string> { "self" },
                        new List<ITypeInfo> { this },
                        new List<bool> { true },
                        PrimitiveTypeInfo.Void);

                default:
                    return base.GetMethodDefinition(name, isSelfMutable);
            }
        }


        public override bool Equals(object obj) {
            return obj is FileStreamTypeInfo;
        }


        public override int GetHashCode() {
            var hashCode = 890389916;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}

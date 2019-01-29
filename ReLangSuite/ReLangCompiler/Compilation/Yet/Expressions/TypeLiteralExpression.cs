﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Yet {
    /// <summary>
    /// Expression representing a type literal (Int, [Bool])
    /// </summary>
    class TypeLiteralExpression : ILiteralExpression {
        public bool HasSideEffect => false;
        public bool IsCompileTime => true;
        public object Value => throw new NotImplementedException();
        public ITypeInfo TypeInfo { get; private set; }
        public bool IsLvalue => false;
        public Location MainLocation { get; }


        public TypeLiteralExpression(ITypeInfo typeInfo, Location mainLocation) {
            TypeInfo = typeInfo;
            MainLocation = mainLocation;
        }


        public IExpression ChangeType(ITypeInfo newType) {
            var copy = (TypeLiteralExpression)MemberwiseClone();
            copy.TypeInfo = newType;
            return copy;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Signalizes an error occured during program's execution
    /// </summary>
    class ProgramException : Exception {
        public ErrorTypeInfo.Option ErrorOption { get; }
        public Stack<Location> Locations { get; }


        public ProgramException(ErrorTypeInfo.Option errorOption, string message, Location location) : base(message) {
            ErrorOption = errorOption;
            Locations = new Stack<Location>();
            Locations.Push(location);
        }


        public void AddLocation(Location location) {
            Locations.Push(location);
        }


        public static ProgramException CreateRangeError(int index, int maximum, Location location) {
            var message = $"Out of range (expected index from [{-maximum}; {maximum}) but got {index})";
            return new ProgramException(ErrorTypeInfo.Option.RangeError, message, location);
        }


        public static ProgramException CreateZeroDivisionError(Location location) {
            return new ProgramException(ErrorTypeInfo.Option.ZeroDivisionError, "Division by zero", location);
        }


        public static ProgramException CreateKeyError(string key, Location location) {
            var message = $"Collection has no key {key}";
            return new ProgramException(ErrorTypeInfo.Option.KeyError, message, location);
        }


        public static ProgramException CreateNullError(Location location) {
            var message = "Expression is equal to 'null'";
            return new ProgramException(ErrorTypeInfo.Option.NullError, message, location);
        }


        public static ProgramException CreateNoReturnValueError(string name) {
            var message = $"Control flow reached the end of '{name}' but return value is undefined";
            return new ProgramException(ErrorTypeInfo.Option.NoReturnValueError, message, null);
        }


        public static ProgramException CreateNotSupportedError(string typeName, string methodName, Location location) {
            var message = $"{typeName} doesn't support '{methodName}'";
            return new ProgramException(ErrorTypeInfo.Option.NotSupportedError, message, location);
        } 
    }
}

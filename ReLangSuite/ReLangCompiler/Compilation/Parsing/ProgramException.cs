using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Parsing {
    /// <summary>
    /// Signalizes an error occured during program's execution
    /// </summary>
    class ProgramException : Exception {
        /// <summary>
        /// All supported errors
        /// </summary>
        public enum Option {
            RangeError,
            KeyError,
            FormatError,
            ZeroDivisionError,
            NullError,
        }


        public Option ErrorOption { get; }
        public Stack<Location> Locations { get; }


        public ProgramException(Option errorOption, string message, Location location) : base(message) {
            ErrorOption = errorOption;
            Locations = new Stack<Location>();
            Locations.Push(location);
        }


        public void AddLocation(Location location) {
            Locations.Push(location);
        }


        public static ProgramException CreateRangeError(int index, int maximum, Location location) {
            var message = $"Out of range (expected index from [{-maximum}; {maximum}) but got {index})";
            return new ProgramException(Option.RangeError, message, location);
        }


        public static ProgramException CreateZeroDivisionError(Location location) {
            return new ProgramException(Option.ZeroDivisionError, "Division by zero", location);
        }


        public static ProgramException CreateKeyError(string key, Location location) {
            var message = $"Collection has no key {key}";
            return new ProgramException(Option.KeyError, message, location);
        }


        public static ProgramException CreateNullError(Location location) {
            var message = "Expression is equal to 'null'";
            return new ProgramException(Option.NullError, message, location);
        }
    }
}

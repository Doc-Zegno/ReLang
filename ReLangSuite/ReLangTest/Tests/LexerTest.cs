using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Handmada.ReLang.Compilation.Lexing;


namespace Handmada.ReLang.Tests {
    [TestClass]
    public class LexerTest {
        [TestMethod]
        public void ScanIntegerTest() {
            var lexer = PrepareLexer("137");
            var lexeme = lexer.GetNextLexeme();

            Assert.IsInstanceOfType(lexeme, typeof(LiteralLexeme));
            Assert.AreEqual(137, (int)((LiteralLexeme)lexeme).Value);
        }


        [TestMethod]
        public void ScanNumericTest() {
            var lexer = PrepareLexer("4.00000123");
            var lexeme = lexer.GetNextLexeme();

            Assert.IsInstanceOfType(lexeme, typeof(LiteralLexeme));
            Assert.AreEqual(4.00000123, (double)((LiteralLexeme)lexeme).Value);
        }


        [TestMethod]
        public void ScanStringTest() {
            var lexer = PrepareLexer("\"Sample Text\"");
            var lexeme = lexer.GetNextLexeme();

            Assert.IsInstanceOfType(lexeme, typeof(LiteralLexeme));
            Assert.AreEqual("Sample Text", (string)((LiteralLexeme)lexeme).Value);
        }


        private Lexer PrepareLexer(string line) {
            var lines = new List<string> { line };
            return new Lexer(lines);
        }
    }
}

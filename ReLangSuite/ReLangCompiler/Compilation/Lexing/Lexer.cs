using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Lexing {
    /// <summary>
    /// Lexical analyser
    /// </summary>
    public class Lexer {
        private IEnumerable<string> lines;
        private IEnumerator<string> lineEnumerator;
        private IEnumerator<char> charEnumerator;
        private char? bufferedCharacter;
        private char? previousCharacter;
        private char currentCharacter;
        private bool isEofReached;
        private bool isInsideFormat;
        private int braceBalance;

        public string CurrentLine { get; private set; }
        public int CurrentLineNumber { get; private set; }
        public int CurrentCharacterNumber { get; private set; }
        public Location CurrentLocation => new Location(CurrentLine, CurrentLineNumber, CurrentCharacterNumber);


        public Lexer(IEnumerable<string> lines) {
            this.lines = lines;
            lineEnumerator = lines.GetEnumerator();
            //bufferedCharacters = new Stack<char>();
            isEofReached = false;
            isInsideFormat = false;
            braceBalance = 0;
            CurrentLineNumber = -1;

            if (!MoveNextLine()) { 
                throw new ArgumentException("no lines to parse", nameof(lines));
            }
            MoveNextCharacter();
        }


        public Lexeme GetNextLexeme() {
            if (!isEofReached) {
                var location = CurrentLocation;

                if (currentCharacter == '\n') {
                    // New line
                    MoveNextCharacter();
                    return new OperatorLexeme(OperatorMeaning.NewLine, location);
                }

                // Swallow white spaces
                while (char.IsWhiteSpace(currentCharacter)) {
                    if (!MoveNextCharacter()) {
                        return null;
                    }
                }
                location = CurrentLocation;

                if (currentCharacter == '\"') {
                    // String literal
                    return ScanString();

                } else if (currentCharacter == '@') {
                    // Verbatim string literal
                    return ScanVerbatimString();

                } else if (currentCharacter == '$') {
                    // Format string literal
                    if (isInsideFormat) {
                        RaiseError("Nested format strings are not allowed");
                    }
                    return ScanFormatStringBeginning();

                } else if (currentCharacter == '\'') {
                    // Character literal
                    return ScanGrapheme();

                } else if (char.IsDigit(currentCharacter)) {
                    // Numeric literal
                    return ScanNumeric();

                } else if (char.IsLetter(currentCharacter)) {
                    // Symbol
                    return ScanSymbol();

                } else {
                    // Character
                    var op = ScanOperator();
                    if (op is OperatorLexeme operatorLexeme && operatorLexeme.Meaning == OperatorMeaning.Commentary) {
                        while (currentCharacter != '\n') {
                            if (!MoveNextCharacter()) {
                                break;
                            }
                        }
                    }
                    return op;
                }

            } else {
                // No more lexemes
                return null;
            }
        }


        private Lexeme ScanOperator() {
            var location = CurrentLocation;
            var meaning = OperatorMeaning.Unknown;
            switch (currentCharacter) {
                case '(':
                    meaning = OperatorMeaning.OpenParenthesis;
                    break;

                case ')':
                    meaning = OperatorMeaning.CloseParenthesis;
                    break;

                case '[':
                    meaning = OperatorMeaning.OpenBracket;
                    break;

                case ']':
                    meaning = OperatorMeaning.CloseBracket;
                    break;

                case '{':
                    braceBalance++;
                    meaning = OperatorMeaning.OpenBrace;
                    break;

                case '}':
                    if (isInsideFormat && braceBalance == 0) {
                        return ScanFormatStringPiece();
                    }
                    braceBalance--;
                    meaning = OperatorMeaning.CloseBrace;
                    break;

                case '=':
                    meaning = ScanDoubleOperator(OperatorMeaning.Assignment, OperatorMeaning.Equal);
                    break;

                case ',':
                    meaning = OperatorMeaning.Comma;
                    break;

                case '.':
                    meaning = ScanDoubleOperator(OperatorMeaning.Dot, OperatorMeaning.Range);
                    break;

                case ':':
                    meaning = OperatorMeaning.Colon;
                    break;

                case '-':
                    meaning = ScanMultipleDoubleOperator(
                        OperatorMeaning.Minus, 
                        new List<(OperatorMeaning, char)> {
                            (OperatorMeaning.ThinRightArrow, '>'),
                            (OperatorMeaning.Decrement, '-')
                        }
                    );
                    break;

                case '+':
                    meaning = ScanDoubleOperator(OperatorMeaning.Plus, OperatorMeaning.Increment);
                    break;

                case '*':
                    meaning = OperatorMeaning.Asterisk;
                    break;

                case '/':
                    meaning = ScanDoubleOperator(OperatorMeaning.ForwardSlash, OperatorMeaning.Commentary);
                    break;

                case '\\':
                    meaning = OperatorMeaning.BackSlash;
                    break;

                case '&':
                    meaning = ScanDoubleOperator(OperatorMeaning.BitwiseAnd, OperatorMeaning.And);
                    break;

                case '|':
                    meaning = ScanDoubleOperator(OperatorMeaning.BitwiseOr, OperatorMeaning.Or);
                    break;

                case '!':
                    meaning = ScanDoubleOperator(OperatorMeaning.ExclamationMark, OperatorMeaning.NotEqual, '=');
                    break;

                case '<':
                    meaning = ScanDoubleOperator(OperatorMeaning.Less, OperatorMeaning.LessOrEqual, '=');
                    break;

                case '>':
                    meaning = ScanDoubleOperator(OperatorMeaning.More, OperatorMeaning.MoreOrEqual, '=');
                    break;

                case '%':
                    meaning = OperatorMeaning.Modulo;
                    break;

                case '?':
                    meaning = ScanDoubleOperator(OperatorMeaning.QuestionMark, OperatorMeaning.ValueOrDefault);
                    break;

                default:
                    RaiseError($"Unexpected character: {currentCharacter}");
                    break;
            }
            MoveNextCharacter();
            return new OperatorLexeme(meaning, location);
        }


        private OperatorMeaning ScanDoubleOperator(
            OperatorMeaning singleMeaning,
            OperatorMeaning doubleMeaning,
            char? targetCharacter = null)
        {
            var target = targetCharacter ?? currentCharacter;
            if (MoveNextCharacter()) {
                if (currentCharacter == target) {
                    return doubleMeaning;
                } else {
                    PutBack();
                }
            }
            return singleMeaning;
        }


        private OperatorMeaning ScanMultipleDoubleOperator(
            OperatorMeaning singleMeaning,
            List<(OperatorMeaning, char)> pairs)
        {
            if (MoveNextCharacter()) {
                foreach (var (meaning, target) in pairs) {
                    if (currentCharacter == target) {
                        return meaning;
                    }
                }
                PutBack();
            }
            return singleMeaning;
        }


        private Lexeme ScanSymbol() {
            var location = CurrentLocation;
            var builder = new StringBuilder();
            builder.Append(currentCharacter);

            while (true) {
                if (MoveNextCharacter()) {
                    if (char.IsLetterOrDigit(currentCharacter)) {
                        builder.Append(currentCharacter);
                    } else {
                        break;
                    }
                } else {
                    break;
                }
            }

            var text = builder.ToString();
            switch (text) {
                case "true":
                    return new LiteralLexeme(true, location);

                case "false":
                    return new LiteralLexeme(false, location);

                case "var":
                    return new OperatorLexeme(OperatorMeaning.Var, location);

                case "let":
                    return new OperatorLexeme(OperatorMeaning.Let, location);

                case "use":
                    return new OperatorLexeme(OperatorMeaning.Use, location);

                case "func":
                    return new OperatorLexeme(OperatorMeaning.Func, location);

                case "if":
                    return new OperatorLexeme(OperatorMeaning.If, location);

                case "elif":
                    return new OperatorLexeme(OperatorMeaning.Elif, location);

                case "else":
                    return new OperatorLexeme(OperatorMeaning.Else, location);

                case "for":
                    return new OperatorLexeme(OperatorMeaning.For, location);

                case "while":
                    return new OperatorLexeme(OperatorMeaning.While, location);

                case "do":
                    return new OperatorLexeme(OperatorMeaning.Do, location);

                case "in":
                    return new OperatorLexeme(OperatorMeaning.In, location);

                case "return":
                    return new OperatorLexeme(OperatorMeaning.Return, location);

                case "break":
                    return new OperatorLexeme(OperatorMeaning.Break, location);

                case "continue":
                    return new OperatorLexeme(OperatorMeaning.Continue, location);

                case "null":
                    return new OperatorLexeme(OperatorMeaning.Null, location);

                case "mutable":
                    return new OperatorLexeme(OperatorMeaning.Mutable, location);

                case "const":
                    return new OperatorLexeme(OperatorMeaning.Const, location);

                default:
                    return new SymbolLexeme(text, location);
            }
        }


        private Lexeme ScanFormatStringBeginning() {
            var location = CurrentLocation;

            if (!MoveNextCharacter() || currentCharacter != '\"') {
                RaiseError("Opening quote was expected");
            }

            isInsideFormat = true;
            return ScanFormatStringPiece(location);
        }


        private Lexeme ScanFormatStringPiece(Location location = null) {
            var loc = location ?? CurrentLocation;
            var isEnding = false;
            var builder = new StringBuilder();

            while (true) {
                if (!MoveNextCharacter() || currentCharacter == '\n') {
                    RaiseError("Either closing quote or opening brace were expected");
                }

                if (currentCharacter == '\"') {
                    MoveNextCharacter();
                    isEnding = true;
                    isInsideFormat = false;
                    break;
                }

                if (currentCharacter == '{') {
                    // May close
                    if (!MoveNextCharacter()) {
                        RaiseError("Unexpected end of file");
                    }

                    if (currentCharacter == '{') {
                        builder.Append('{');
                    } else {
                        braceBalance = 0;
                        break;
                    }

                } else if (currentCharacter == '}') {
                    if (!MoveNextCharacter() || currentCharacter != '}') {
                        RaiseError("Another one closing brace was expected");
                    }
                    builder.Append('}');

                } else {
                    builder.Append(ScanCharacter());
                }
            }

            return new FormatStringLexeme(builder.ToString(), isEnding, loc);
        }


        private Lexeme ScanVerbatimString() {
            var location = CurrentLocation;
            var builder = new StringBuilder();

            if (!MoveNextCharacter() || currentCharacter != '\"') {
                RaiseError("Opening quote was expected");
            }

            while (true) {
                if (!MoveNextCharacter() || currentCharacter == '\n') {
                    RaiseError("Closing quote was expected");
                }

                if (currentCharacter == '\"') {
                    // Either end of string or internal quote
                    if (!MoveNextCharacter()) {
                        break;
                    }

                    if (currentCharacter == '\"') {
                        builder.Append('\"');
                    } else {
                        break;
                    }
                } else {
                    builder.Append(currentCharacter);
                }
            }

            return new LiteralLexeme(builder.ToString(), location);
        }


        private Lexeme ScanString() {
            var location = CurrentLocation;
            var builder = new StringBuilder();

            while (true) {
                if (MoveNextCharacter()) {
                    if (currentCharacter == '\"') {
                        break;
                    } else if (currentCharacter == '\n') {
                        RaiseError("Closing quote was expected");
                    } else {
                        builder.Append(ScanCharacter());
                    }
                } else {
                    RaiseError("Closing quote was expected");
                }
            }

            MoveNextCharacter();
            return new LiteralLexeme(builder.ToString(), location);
        }


        private Lexeme ScanGrapheme() {
            var location = CurrentLocation;

            if (!MoveNextCharacter() || currentCharacter == '\'') {
                RaiseError("Character was expected");
            }

            var ch = ScanCharacter();
            if (!MoveNextCharacter() || currentCharacter != '\'') {
                RaiseError("Closing single quote was expected");
            }

            MoveNextCharacter();
            return new LiteralLexeme(ch, location);
        }


        private char ScanCharacter() {
            if (currentCharacter == '\\') {
                if (MoveNextCharacter()) {
                    switch (currentCharacter) {
                        case 'n':
                            return '\n';

                        case '\"':
                            return '\"';

                        case '\'':
                            return '\'';

                        case 't':
                            return '\t';

                        default:
                            RaiseError($"Unrecognized control sequence: '\\{currentCharacter}");
                            break;
                    }
                } else {
                    RaiseError("Unexpected ending of control sequence");
                }
            } else if (char.IsControl(currentCharacter)) {
                RaiseError("Printable character was expected");
            } else {
                return currentCharacter;
            }
            return (char)0;  // Useless
        }


        private Lexeme ScanNumeric() {
            var location = CurrentLocation;
            var (integer, _) = ScanInteger();

            if (isEofReached) {
                return new LiteralLexeme(integer, location);
            }
            
            if (currentCharacter == '.') {
                if (!MoveNextCharacter()) {
                    RaiseError("Digit or dot was expected");
                }

                if (currentCharacter == '.') {
                    // Range
                    PutBack();
                    return new LiteralLexeme(integer, location);
                } else if (!char.IsDigit(currentCharacter)) {
                    RaiseError("Digit or dot was expected");
                }

                var (value, power) = ScanInteger();
                var fraction = (double)value / power;
                return new LiteralLexeme(integer + fraction, location);
            } else {
                return new LiteralLexeme(integer, location);
            }
        }


        /*private double MakeFraction(int value) {
            var power = 10;
            while (value / power != 0) {
                power *= 10;
            }
            return (double)value / power;
        }*/


        private (int, int) ScanInteger() {
            var value = currentCharacter - '0';
            var power = 10;

            while (true) {
                if (MoveNextCharacter()) {
                    if (char.IsDigit(currentCharacter)) {
                        value *= 10;
                        power *= 10;
                        value += currentCharacter - '0';
                    } else {
                        break;
                    }
                } else {
                    break;
                }
            }

            return (value, power);
        }


        private bool MoveNextLine() {
            if (lineEnumerator.MoveNext()) {
                CurrentLineNumber++;
                CurrentLine = lineEnumerator.Current;
                charEnumerator = CurrentLine.GetEnumerator();
                CurrentCharacterNumber = -1;
                return true;
            } else {
                isEofReached = true;
                return false;
            }
        }


        private bool MoveNextCharacter() {
            if (bufferedCharacter != null) {
                CurrentCharacterNumber++;
                previousCharacter = currentCharacter;
                currentCharacter = bufferedCharacter.Value;
                bufferedCharacter = null;
                return true;

            } else if (charEnumerator.MoveNext()) {
                CurrentCharacterNumber++;
                previousCharacter = currentCharacter;
                currentCharacter = charEnumerator.Current;
                return true;

            } else {
                if (MoveNextLine()) {
                    return MoveNextCharacter();
                } else {
                    return false;
                }
            }
        }


        private void PutBack(char? ch = null) {
            if (bufferedCharacter == null) {
                CurrentCharacterNumber--;
                bufferedCharacter = ch ?? currentCharacter;
            } else {
                throw new NotImplementedException("Lexer buffer cannot handle more than one character");
            }
        }


        private void RaiseError(string message) {
            throw new LexerException(message, CurrentLine, CurrentLineNumber, CurrentCharacterNumber);
        }
    }
}

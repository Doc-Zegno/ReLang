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
    class Lexer {
        private IEnumerable<string> lines;
        private IEnumerator<string> lineEnumerator;
        private IEnumerator<char> charEnumerator;
        private char? bufferedCharacter;
        private char currentCharacter;

        public string CurrentLine { get; private set; }
        public int CurrentLineNumber { get; private set; }
        public int CurrentCharacterNumber { get; private set; }
        public Location CurrentLocation => new Location(CurrentLine, CurrentLineNumber, CurrentCharacterNumber);


        public Lexer(IEnumerable<string> lines) {
            this.lines = lines;
            lineEnumerator = lines.GetEnumerator();
            CurrentLineNumber = -1;

            if (!MoveNextLine()) { 
                throw new ArgumentException("no lines to parse", nameof(lines));
            }
        }


        public Lexeme GetNextLexeme() {
            while (MoveNextCharacter()) {
                var location = CurrentLocation;

                // New line
                if (currentCharacter == '\n') {
                    return new OperatorLexeme(OperatorMeaning.NewLine, location);

                    // White space
                } else if (char.IsWhiteSpace(currentCharacter)) {
                    continue;

                    // String literal
                } else if (currentCharacter == '\"') {
                    var builder = new StringBuilder();

                    while (true) {
                        if (MoveNextCharacter()) {
                            if (currentCharacter == '\"') {
                                break;
                            } else if (currentCharacter == '\n') {
                                RaiseError("Closing quote was expected");
                            } else {
                                builder.Append(currentCharacter);
                            }
                        } else {
                            RaiseError("Closing quote was expected");
                        }
                    }

                    return new LiteralLexeme(builder.ToString(), location);

                    // Numeric literal
                } else if (char.IsNumber(currentCharacter)) {
                    var value = currentCharacter - '0';

                    while (true) {
                        if (MoveNextCharacter()) {
                            if (char.IsNumber(currentCharacter)) {
                                value *= 10;
                                value += currentCharacter - '0';
                            } else {
                                PutBack();
                                break;
                            }
                        } else {
                            break;
                        }
                    }

                    return new LiteralLexeme(value, location);

                    // Symbol
                } else if (char.IsLetter(currentCharacter)) {
                    var builder = new StringBuilder();
                    builder.Append(currentCharacter);

                    while (true) {
                        if (MoveNextCharacter()) {
                            if (char.IsLetterOrDigit(currentCharacter)) {
                                builder.Append(currentCharacter);
                            } else {
                                PutBack();
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

                        case "func":
                            return new OperatorLexeme(OperatorMeaning.Func, location);

                        case "if":
                            return new OperatorLexeme(OperatorMeaning.If, location);

                        case "else":
                            return new OperatorLexeme(OperatorMeaning.Else, location);

                        default:
                            return new SymbolLexeme(text, location);
                    }

                    // Character
                } else {
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
                            meaning = OperatorMeaning.OpenBrace;
                            break;

                        case '}':
                            meaning = OperatorMeaning.CloseBrace;
                            break;

                        case '=':
                            meaning = OperatorMeaning.Assignment;
                            if (MoveNextCharacter()) {
                                if (currentCharacter == '=') {
                                    meaning = OperatorMeaning.Equal;
                                } else {
                                    PutBack();
                                }
                            }
                            break;

                        case ',':
                            meaning = OperatorMeaning.Comma;
                            break;

                        default:
                            RaiseError($"Unexpected character: {currentCharacter}");
                            break;
                    }
                    return new OperatorLexeme(meaning, location);
                }
            }

            // No more lexemes
            return null;
        }


        private bool MoveNextLine() {
            if (lineEnumerator.MoveNext()) {
                CurrentLineNumber++;
                CurrentLine = lineEnumerator.Current;
                charEnumerator = CurrentLine.GetEnumerator();
                CurrentCharacterNumber = -1;
                return true;
            } else {
                return false;
            }
        }


        private bool MoveNextCharacter() {
            if (bufferedCharacter.HasValue) {
                currentCharacter = bufferedCharacter.Value;
                bufferedCharacter = null;
                return true;
            } else if (charEnumerator.MoveNext()) {
                CurrentCharacterNumber++;
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


        private void PutBack() {
            bufferedCharacter = currentCharacter;
        }


        private void RaiseError(string message) {
            throw new LexerException(message, CurrentLine, CurrentLineNumber, CurrentCharacterNumber);
        }
    }
}

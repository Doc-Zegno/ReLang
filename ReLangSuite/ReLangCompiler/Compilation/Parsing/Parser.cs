using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Yet;


namespace Handmada.ReLang.Compilation.Parsing {
    public partial class Parser {
        private List<Lexeme> lexemes;
        private IEnumerator<Lexeme> lexemeEnumerator;
        private Lexeme currentLexeme;
        private List<FunctionData> functions;
        private int? mainFunctionNumber;
        private FunctionTree functionTree;
        private ScopeStack scopeStack;


        public Parser(IEnumerable<string> lines) {
            lexemes = new List<Lexeme>();

            var lexer = new Lexer(lines);
            while (true) {
                var lexeme = lexer.GetNextLexeme();
                if (lexeme != null) {
                    lexemes.Add(lexeme);
                } else {
                    break;
                }
            }

            functions = new List<FunctionData>();
            functionTree = new FunctionTree();
            scopeStack = new ScopeStack();
        }


        public ParsedProgram ParseProgram() {
            // Collect function definitions
            ResetLexemes();
            MoveNextLexeme();
            BuildFunctionTree();
            functionTree.PrintTree();

            // Parse program
            ResetLexemes();
            MoveNextLexeme();
            Parse();

            if (mainFunctionNumber == null) {
                RaiseError("End of file was reached but 'main' function wasn't found");
            }

            return new ParsedProgram(functions, mainFunctionNumber.Value);
        }


        private void BuildFunctionTree() {
            var balance = 0;
            do {
                if (WhetherOperator(OperatorMeaning.Func)) {
                    MoveNextLexeme();
                    StepInFunction();
                } else if (WhetherOperator(OperatorMeaning.OpenBrace)) {
                    balance++;
                } else if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                    balance--;
                    if (balance < 0) {
                        break;
                    }
                }
            } while (MoveNextLexeme());
        }


        private void StepInFunction() {
            var location = currentLexeme.StartLocation;
            var (name, _, argumentTypes, resultType) = GetFunctionSignature();
            CheckOperator(OperatorMeaning.OpenBrace);

            // Register function
            if (!functionTree.DeclareFunction(name, resultType, argumentTypes)) {
                RaiseError($"Declaration of function '{name}' interferes with another declaration", location);
            }

            // Entered a function scope
            BuildFunctionTree();
            // Leave a function scope
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
        }


        private (string, List<string>, List<ITypeInfo>, ITypeInfo) GetFunctionSignature() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);

            // Parse parameter list
            var argumentNames = new List<string>();
            var argumentTypes = new List<ITypeInfo>();
            if (!WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                while (true) {
                    argumentNames.Add(GetSymbolText("Argument name"));
                    CheckOperator(OperatorMeaning.Colon);
                    argumentTypes.Add(GetTypeInfo());

                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }
            }
            CheckOperator(OperatorMeaning.CloseParenthesis);

            // Parse return type
            var resultType = (ITypeInfo)PrimitiveTypeInfo.Void;
            if (WhetherOperator(OperatorMeaning.ThinRightArrow)) {
                MoveNextLexeme();
                resultType = GetTypeInfo();
            }

            return (name, argumentNames, argumentTypes, resultType);
        }



        // Int, [Int], {Int}, [{Int}]
        private ITypeInfo GetTypeInfo() {
            var location = currentLexeme.StartLocation;
            switch (currentLexeme) {
                case SymbolLexeme symbol:
                    MoveNextLexeme();
                    switch (symbol.Text) {
                        case "Void":
                            return PrimitiveTypeInfo.Void;

                        case "Bool":
                            return PrimitiveTypeInfo.Bool;

                        case "Int":
                            return PrimitiveTypeInfo.Int;

                        case "Float":
                            return PrimitiveTypeInfo.Float;

                        case "String":
                            return PrimitiveTypeInfo.String;

                        case "Object":
                            return PrimitiveTypeInfo.Object;

                        default:
                            RaiseError($"Unknown type: '{symbol.Text}'", location);
                            return null;
                    }

                case OperatorLexeme operatorLexeme:
                    MoveNextLexeme();
                    ITypeInfo itemType;
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.OpenBracket:
                            itemType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.CloseBracket);
                            return new ArrayListTypeInfo(itemType);

                        case OperatorMeaning.OpenBrace:
                            itemType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.CloseBrace);
                            return new HashSetTypeInfo(itemType);

                        case OperatorMeaning.OpenParenthesis:
                            var tupleType = GetTupleTypeInfo();
                            CheckOperator(OperatorMeaning.CloseParenthesis);
                            return tupleType;

                        default:
                            RaiseError("Unexpected operator lexeme", location);
                            return null;
                    }

                default:
                    RaiseError("Expected type literal", location);
                    return null;
            }
        }


        private ITypeInfo GetTupleTypeInfo() {
            var itemTypes = new List<ITypeInfo>();
            while (true) {
                itemTypes.Add(GetTypeInfo());
                if (WhetherOperator(OperatorMeaning.Comma)) {
                    MoveNextLexeme();
                } else {
                    break;
                }
            }
            return new TupleTypeInfo(itemTypes);
        }


        private void Parse() {
            do {
                if (WhetherOperator(OperatorMeaning.NewLine)) {
                    MoveNextLexeme();
                    continue;
                }

                CheckOperator(OperatorMeaning.Func);
                ParseFunction();
            } while (currentLexeme != null);
        }


        private void ParseFunction() {
            var location = currentLexeme.StartLocation;
            var (name, argumentNames, argumentTypes, resultType) = GetFunctionSignature();
            CheckOperator(OperatorMeaning.OpenBrace);

            // Reserve place for function data
            var number = functions.Count;
            functions.Add(null);

            Console.WriteLine($"parsing function '{name}'...");

            // Parse body
            functionTree.EnterScope(name);
            scopeStack.EnterScope(isStrong: true);

            // Place all arguments inside frame
            for (var i = 0; i < argumentNames.Count; i++) {
                scopeStack.DeclareVariable(argumentNames[i], argumentTypes[i], true, null);
            }

            var body = GetStatementList(true);
            scopeStack.LeaveScope();
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);

            Console.WriteLine($"stopped parsing function '{name}'");

            // Add function to list
            var maybe = functionTree.GetFunctionDefinition(name);
            var definition = maybe.Value;
            functions[number] = new FunctionData(name, definition.FullQualification, resultType,
                                                 argumentNames, argumentTypes, body);

            // Check for main
            if (definition.IsGlobal && name == "main") {
                if (argumentTypes.Count == 1
                    && argumentTypes[0] is ArrayListTypeInfo arrayListType
                    && arrayListType.ItemType is PrimitiveTypeInfo primitiveType
                    && primitiveType.TypeOption == PrimitiveTypeInfo.Option.String)
                {
                    if (resultType is PrimitiveTypeInfo primitive
                        && (primitive.TypeOption == PrimitiveTypeInfo.Option.Int
                            || primitive.TypeOption == PrimitiveTypeInfo.Option.Void)) 
                    {
                        mainFunctionNumber = definition.Number;
                    } else {
                        RaiseError($"Result type of main function must be either 'Int' or 'Void' (got '{resultType.Name}')", location);
                    }
                } else {
                    RaiseError("Main function must have one argument of type '[String]'", location);
                }
            }
        }


        private string GetSymbolText(string expected) {
            var text = "";
            if (currentLexeme is SymbolLexeme lexeme) {
                text = lexeme.Text;
            } else {
                RaiseError($"{expected} was expected");
            }
            MoveNextLexeme();
            return text;
        }


        private void CheckOperator(OperatorMeaning meaning) {
            if (!WhetherOperator(meaning)) { 
                RaiseError($"{GetOperatorName(meaning)} was expected");
            }
            MoveNextLexeme();
        }


        private bool WhetherOperator(OperatorMeaning meaning) {
            if (currentLexeme is OperatorLexeme lexeme && lexeme.Meaning == meaning) {
                return true;
            } else {
                return false;
            }
        }


        private IExpression TryConvertExpression(IExpression expression, ITypeInfo targetType) {
            return expression.TypeInfo.ConvertTo(expression, targetType);
        }


        private IExpression ForceConvertExpression(IExpression expression, ITypeInfo targetType, Location location) {
            var converted = TryConvertExpression(expression, targetType);
            if (converted != null) {
                return converted;
            } else {
                RaiseError($"Cannot convert expression of type '{expression.TypeInfo.Name}' to '{targetType.Name}'", location);
                return null;
            }
        }


        private bool WhetherPrimitiveType(IExpression expression, PrimitiveTypeInfo.Option option) {
            if (expression.TypeInfo is PrimitiveTypeInfo primitiveType && primitiveType.TypeOption == option) {
                return true;
            } else {
                return false;
            }
        }


        private string GetOperatorName(OperatorMeaning meaning) {
            switch (meaning) {
                case OperatorMeaning.Unknown:
                    return "Unknown";

                case OperatorMeaning.OpenParenthesis:
                    return "Opening parenthesis";

                case OperatorMeaning.CloseParenthesis:
                    return "Closing parenthesis";

                case OperatorMeaning.OpenBracket:
                    return "Opening bracket";

                case OperatorMeaning.CloseBracket:
                    return "Closing bracket";

                case OperatorMeaning.OpenBrace:
                    return "Opening brace";

                case OperatorMeaning.CloseBrace:
                    return "Closing brace";

                case OperatorMeaning.NewLine:
                    return "New line";

                case OperatorMeaning.Assignment:
                    return "Assignment operator";

                case OperatorMeaning.Equal:
                    return "Equality comparison operator";

                case OperatorMeaning.Var:
                    return "Variable declaration operator";

                case OperatorMeaning.Let:
                    return "Constant declaration operator";

                case OperatorMeaning.If:
                    return "Conditional If operator";

                case OperatorMeaning.Func:
                    return "Function declaration operator";

                case OperatorMeaning.Comma:
                    return "Comma";

                case OperatorMeaning.Dot:
                    return "Dot operator";

                case OperatorMeaning.Colon:
                    return "Colon";

                case OperatorMeaning.Minus:
                    return "Minus operator";

                case OperatorMeaning.Plus:
                    return "Plus operator";

                case OperatorMeaning.Asterisk:
                    return "Asterisk operator";

                case OperatorMeaning.ForwardSlash:
                    return "Forward slash";

                case OperatorMeaning.Commentary:
                    return "One-line commentary operator";

                case OperatorMeaning.BackSlash:
                    return "Back slash";

                case OperatorMeaning.BitwiseAnd:
                    return "Bitwise And operator";

                case OperatorMeaning.And:
                    return "Logical And operator";

                case OperatorMeaning.BitwiseOr:
                    return "Bitwise Or operator";

                case OperatorMeaning.Or:
                    return "Logical Or operator";

                case OperatorMeaning.Not:
                    return "Logical Not operator";

                case OperatorMeaning.Else:
                    return "Conditional Else operator";

                default:
                    throw new ArgumentException($"unknown option: {meaning}", nameof(meaning));
            }
        }


        private void RaiseError(string message, Location? location = null) {
            var loc = location ?? currentLexeme.StartLocation;
            throw new ParserException(message, loc.Line, loc.LineNumber, loc.ColumnNumber);
        }


        private void ResetLexemes() {
            lexemeEnumerator = lexemes.GetEnumerator();
        }


        private bool MoveNextLexeme() {
            if (lexemeEnumerator.MoveNext()) {
                currentLexeme = lexemeEnumerator.Current;
                return true;
            } else {
                currentLexeme = null;
                return false;
            }
        }
    }
}

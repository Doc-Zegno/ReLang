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
        private Lexeme previousLexeme;
        private Lexeme bufferedLexeme;
        private List<FunctionData> functions;
        private int? mainFunctionNumber;
        private FunctionTree functionTree;
        private ScopeStack scopeStack;
        private int numOfTemporaries;


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
            var signature = GetFunctionSignature();
            CheckOperator(OperatorMeaning.OpenBrace);

            // Register function
            if (!functionTree.DeclareFunction(signature)) {
                RaiseError($"Declaration of function '{signature.Name}' interferes with another declaration", location);
            }

            // Entered a function scope
            BuildFunctionTree();
            // Leave a function scope
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
        }


        private FunctionSignature GetFunctionSignature() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);

            // Parse parameter list
            var argumentNames = new List<string>();
            var argumentTypes = new List<ITypeInfo>();
            var argumentMutabilities = new List<bool>();
            if (!WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                while (true) {
                    // Name
                    argumentNames.Add(GetSymbolText("Argument name"));
                    CheckOperator(OperatorMeaning.Colon);

                    // Mutability
                    var location = currentLexeme.StartLocation;
                    var isMutable = false;
                    if (WhetherOperator(OperatorMeaning.Mutable)) {
                        MoveNextLexeme();
                        isMutable = true;
                    }
                    argumentMutabilities.Add(isMutable);

                    // Type
                    var argumentType = GetTypeInfo();
                    if (!argumentType.IsReferential && isMutable) {
                        RaiseError("'mutable' qualifier has no effect for non-referential types", location, true);
                    }
                    if (argumentType is PrimitiveTypeInfo primitive
                        && primitive.TypeOption == PrimitiveTypeInfo.Option.String
                        && isMutable)
                    {
                        RaiseError("Strings are immutable, this qualifier is useless", location, true);
                    }
                    argumentTypes.Add(argumentType);

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
            var resultMutability = true;
            if (WhetherOperator(OperatorMeaning.ThinRightArrow)) {
                MoveNextLexeme();

                var location = currentLexeme.StartLocation;
                if (WhetherOperator(OperatorMeaning.Const)) {
                    MoveNextLexeme();
                    resultMutability = false;
                }

                resultType = GetTypeInfoOrVoid();

                if (!resultType.IsReferential && !resultMutability) {
                    RaiseError("'const' qualifier has no effect for non-referential types", location, true);
                }
                if (resultType is PrimitiveTypeInfo primitive
                    && primitive.TypeOption == PrimitiveTypeInfo.Option.String
                    && !resultMutability)
                {
                    RaiseError("Strings are immutable, this qualifier is useless", location, true);
                }

            }

            return new FunctionSignature(name, argumentNames, argumentTypes, argumentMutabilities, resultType, resultMutability);
        }



        private ITypeInfo GetTypeInfoOrVoid() {
            if (currentLexeme is SymbolLexeme symbol && symbol.Text == "Void") {
                MoveNextLexeme();
                return PrimitiveTypeInfo.Void;
            } else {
                return GetTypeInfo();
            }
        }



        // Int, [Int], {Int}, [{Int}]
        private ITypeInfo GetTypeInfo() {
            var typeInfo = GetPrimitiveTypeInfo();
            while (true) {
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.BitwiseAnd:
                            MoveNextLexeme();
                            typeInfo = new IterableTypeInfo(typeInfo);
                            break;

                        case OperatorMeaning.QuestionMark:
                            MoveNextLexeme();
                            typeInfo = new MaybeTypeInfo(typeInfo);
                            break;

                        default:
                            return typeInfo;
                    }
                }
            }
        }


        private ITypeInfo GetPrimitiveTypeInfo() {
            var location = currentLexeme.StartLocation;
            switch (currentLexeme) {
                case SymbolLexeme symbol:
                    MoveNextLexeme();
                    switch (symbol.Text) {
                        /*case "Void":
                            return PrimitiveTypeInfo.Void;*/

                        case "Bool":
                            return PrimitiveTypeInfo.Bool;

                        case "Char":
                            return PrimitiveTypeInfo.Char;

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
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.OpenBracket:
                            var itemType = GetTypeInfo();
                            CheckOperator(OperatorMeaning.CloseBracket);
                            return new ArrayListTypeInfo(itemType);

                        case OperatorMeaning.OpenBrace:
                            var keyType = GetTypeInfo();
                            if (WhetherOperator(OperatorMeaning.Colon)) {
                                MoveNextLexeme();
                                var valueType = GetTypeInfo();
                                CheckOperator(OperatorMeaning.CloseBrace);
                                return new DictionaryTypeInfo(keyType, valueType);
                            } else {
                                CheckOperator(OperatorMeaning.CloseBrace);
                                return new HashSetTypeInfo(keyType);
                            }

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
                if (WhetherOperator(OperatorMeaning.Commentary)) {
                    do {
                        MoveNextLexeme();
                    } while (!WhetherOperator(OperatorMeaning.NewLine) && currentLexeme != null);
                } else if (WhetherOperator(OperatorMeaning.NewLine)) {
                    MoveNextLexeme();
                } else {
                    CheckOperator(OperatorMeaning.Func);
                    ParseFunction();
                }
            } while (currentLexeme != null);
        }


        private void ParseFunction() {
            var location = currentLexeme.StartLocation;
            var signature = GetFunctionSignature();
            CheckOperator(OperatorMeaning.OpenBrace);

            // Reserve place for function data
            var number = functions.Count;
            functions.Add(null);

            Console.WriteLine($"parsing function '{signature.Name}'...");

            // Parse body
            functionTree.EnterScope(signature.Name);
            scopeStack.EnterScope(true, false);

            // Place all arguments inside frame
            for (var i = 0; i < signature.ArgumentNames.Count; i++) {
                var argumentName = signature.ArgumentNames[i];
                var argumentType = signature.ArgumentTypes[i];
                var argumentMutability = signature.ArgumentMutabilities[i];
                scopeStack.DeclareVariable(argumentName, argumentType, MakeQualifier(true, argumentMutability, false), null);
            }

            var body = GetStatementList(true);
            scopeStack.LeaveScope();
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);

            Console.WriteLine($"stopped parsing function '{signature.Name}'");

            // Add function to list
            var definition = functionTree.GetFunctionDefinition(signature.Name);
            functions[number] = new FunctionData(definition, body);

            // Check for main
            if (definition.IsGlobal && signature.Name == "main") {
                if (signature.ArgumentTypes.Count == 1
                    && signature.ArgumentTypes[0] is ArrayListTypeInfo arrayListType
                    && arrayListType.ItemType is PrimitiveTypeInfo primitiveType
                    && primitiveType.TypeOption == PrimitiveTypeInfo.Option.String)
                {
                    if (signature.ResultType is PrimitiveTypeInfo primitive
                        && (primitive.TypeOption == PrimitiveTypeInfo.Option.Int
                            || primitive.TypeOption == PrimitiveTypeInfo.Option.Void)) 
                    {
                        mainFunctionNumber = definition.Number;
                    } else {
                        RaiseError($"Result type of main function must be either 'Int' or 'Void'"
                                   + " (got '{signature.ResultType.Name}')", location);
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


        private VariableExpression CreateVariableExpression(SingleIdentifier identifier) {
            var name = identifier.Name;
            var location = identifier.StartLocation;
            var definition = scopeStack.GetDefinition(name);

            if (definition == null) {
                RaiseError($"Undeclared variable '{name}'", location);
            }

            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
            return new VariableExpression(name, definition.Number, frameOffset, false, definition.TypeInfo, location);
        }


        private IExpression TryConvertExpression(IExpression expression, ITypeInfo targetType) {
            return targetType.ConvertFrom(expression);
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


        private IExpression ForceConstructFrom(IExpression expression, ITypeInfo targetType, Location location) {
            IExpression constructed = null;
            try {
                constructed = targetType.ConstructFrom(expression, location);
            } catch (FormatException e) {
                RaiseError(e.Message, location);
            }

            if (constructed != null) {
                return constructed;
            } else {
                RaiseError(
                    $"Cannot construct object of type '{targetType.Name}' from expression of type '{expression.TypeInfo.Name}'",
                    location
                );
                return null;
            }   
        }


        private ITypeInfo TryGetItemType(ITypeInfo typeInfo) {
            switch (typeInfo) {
                case IterableTypeInfo iterable:
                    return iterable.ItemType;

                case PrimitiveTypeInfo primitive when primitive.TypeOption == PrimitiveTypeInfo.Option.String:
                    return PrimitiveTypeInfo.Char;

                default:
                    return null;
            }
        }


        private bool WhetherPrimitiveType(ITypeInfo typeInfo, PrimitiveTypeInfo.Option option) {
            if (typeInfo is PrimitiveTypeInfo primitiveType && primitiveType.TypeOption == option) {
                return true;
            } else {
                return false;
            }
        }


        private VariableQualifier MakeQualifier(bool isFinal, bool isMutable, bool isDisposable) {
            var qualifier = VariableQualifier.None;
            if (isFinal) {
                qualifier |= VariableQualifier.Final;
            }
            if (isMutable) {
                qualifier |= VariableQualifier.Mutable;
            }
            if (isDisposable) {
                qualifier |= VariableQualifier.Disposable;
            }
            return qualifier;
        }


        private ITypeInfo ForceJoinTypes(IExpression x, IExpression y) {
            var joined = TryJoinTypes(x, y);
            if (joined == null) {
                RaiseError($"Cannot join expression of type '{y.TypeInfo.Name}' and '{x.TypeInfo.Name}'", y.MainLocation);
            }
            return joined;
        }


        private ITypeInfo TryJoinTypes(IExpression x, IExpression y) {
            // 1) x <- y
            var yConverted = x.TypeInfo.ConvertFrom(y);
            if (yConverted != null) {
                return yConverted.TypeInfo;
            }

            // 2) y <- x
            var xConverted = y.TypeInfo.ConvertFrom(x);
            if (xConverted != null) {
                return xConverted.TypeInfo;
            }

            // 3) x is not maybe T, y is null -> Maybe<T>
            if (!(x.TypeInfo is MaybeTypeInfo) && y.TypeInfo is NullTypeInfo) {
                return new MaybeTypeInfo(x.TypeInfo);
            }
            if (x.TypeInfo is NullTypeInfo && !(y.TypeInfo is MaybeTypeInfo)) {
                return new MaybeTypeInfo(y.TypeInfo);
            }
            
            // 4) x is Maybe<T>, y is E, E <- T
            // i.e. Float? + Object = Object?
            if (x.TypeInfo is MaybeTypeInfo maybeType && y.TypeInfo.CanUpcast(maybeType.InternalType)) {
                return new MaybeTypeInfo(y.TypeInfo);
            }
            if (y.TypeInfo is MaybeTypeInfo maybeType2 && x.TypeInfo.CanUpcast(maybeType2.InternalType)) {
                return new MaybeTypeInfo(x.TypeInfo);
            }

            // Nothing works [and nobody knows why]
            return null;
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

                case OperatorMeaning.ExclamationMark:
                    return "Logical Not operator";

                case OperatorMeaning.Else:
                    return "Conditional Else operator";

                default:
                    throw new ArgumentException($"unknown option: {meaning}", nameof(meaning));
            }
        }


        private string GetNextTmpName() {
            var name = $"_tmp{numOfTemporaries}";
            numOfTemporaries++;
            return name;
        }


        private void RaiseError(string message, Location location = null, bool isSemantic = false) {
            var loc = location ?? currentLexeme.StartLocation;
            throw new ParserException(message, loc.Line, loc.LineNumber, loc.ColumnNumber, isSemantic);
        }


        private void ResetLexemes() {
            bufferedLexeme = null;
            previousLexeme = null;
            lexemeEnumerator = lexemes.GetEnumerator();
        }


        private bool MoveNextLexeme() {
            if (bufferedLexeme != null) {
                previousLexeme = currentLexeme;
                currentLexeme = bufferedLexeme;
                bufferedLexeme = null;
                return true;

            } else if (lexemeEnumerator.MoveNext()) {
                previousLexeme = currentLexeme;
                currentLexeme = lexemeEnumerator.Current;
                return true;

            } else {
                if (currentLexeme != null) {
                    previousLexeme = currentLexeme;
                    currentLexeme = null;
                }
                return false;
            }
        }


        private void PutBack() {
            if (previousLexeme != null) {
                bufferedLexeme = currentLexeme;
                currentLexeme = previousLexeme;
                previousLexeme = null;
            } else {
                throw new NotImplementedException("Current lexeme buffer can handle only 1 buffered lexeme");
            }
        }
    }
}

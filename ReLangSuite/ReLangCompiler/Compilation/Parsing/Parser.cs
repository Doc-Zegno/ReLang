using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;


namespace Handmada.ReLang.Compilation.Parsing {
    class Parser {
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
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            var resultType = new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Void);
            var argumentTypes = new List<ITypeInfo>();
            if (!functionTree.DeclareFunction(name, resultType, argumentTypes)) {
                RaiseError($"Declaration of function '{name}' interferes with another declaration", location);
            }
            // Entered a function scope
            BuildFunctionTree();
            // Leave a function scope
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
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
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            // Reserve place for function data
            var number = functions.Count;
            functions.Add(null);

            Console.WriteLine($"parsing function '{name}'...");

            // Parse body
            functionTree.EnterScope(name);
            scopeStack.EnterScope(isStrong: true);
            var body = GetStatementList(true);
            scopeStack.LeaveScope();
            functionTree.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);

            Console.WriteLine($"stopped parsing function '{name}'");

            // Add function to list
            var maybe = functionTree.GetFunctionDefinition(name);
            var definition = maybe.Value;
            functions[number] = new FunctionData(name, definition.FullQualification, definition.ResultType,
                                                 definition.ArgumentTypes, body);

            // Check for main
            if (definition.IsGlobal && name == "main") {
                mainFunctionNumber = definition.Number;
            }
        }


        private List<IStatement> GetStatementList(bool isScopeStrong) {
            var statements = new List<IStatement>();
            var isTop = true;

            while (currentLexeme != null) { 
                if (WhetherOperator(OperatorMeaning.CloseBrace)) {
                    break;
                }

                if (WhetherOperator(OperatorMeaning.NewLine)) {
                    MoveNextLexeme();
                } else if (WhetherOperator(OperatorMeaning.Func)) {
                    if (isScopeStrong && isTop) {
                        MoveNextLexeme();
                        ParseFunction();
                    } else {
                        RaiseError("Function must be declared at the top of either global or another function's block");
                    }
                } else {
                    statements.Add(GetStatement());
                    isTop = false;
                }
            }

            Console.WriteLine("stopped parsing block");
            
            return statements;
        }


        private IStatement GetStatement() {
            switch (currentLexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        /*case OperatorMeaning.Func:
                            MoveNextLexeme();
                            return GetFunctionDeclaration();*/

                        case OperatorMeaning.If:
                            MoveNextLexeme();
                            return GetConditional();

                        case OperatorMeaning.Var:
                            MoveNextLexeme();
                            return GetVariableDeclaration(isMutable: true);

                        case OperatorMeaning.Let:
                            MoveNextLexeme();
                            return GetVariableDeclaration(isMutable: false);

                        case OperatorMeaning.For:
                            MoveNextLexeme();
                            return GetForLoop();

                        default:
                            RaiseError("Unknown operator lexeme found");
                            break;
                    }
                    break;

                case SymbolLexeme symbolLexeme:
                    var location = currentLexeme.StartLocation;
                    MoveNextLexeme();
                    return new ExpressionStatement(GetFunctionCall(symbolLexeme.Text, location));

                default:
                    RaiseError("Statement was expected");
                    break;
            }

            return null;
        }



        // for item in iterable { ... }
        private IStatement GetForLoop() {
            var name = GetSymbolText("Iterable's item name");
            CheckOperator(OperatorMeaning.In);
            var location = currentLexeme.StartLocation;
            var iterable = GetExpression();

            if (iterable.TypeInfo is IIterableTypeInfo iterableType) {
                var itemType = iterableType.ItemType;

                // Enter scope and add item variable
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                scopeStack.DeclareVariable(name, itemType, false);
                var statements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);

                return new ForEachStatement(name, iterable, statements);
            } else {
                RaiseError($"Source of items must be iterable (got '{iterable.TypeInfo.Name}')", location);
                return null;
            }
        }


        private IExpression GetFunctionCall(string name, Location location) {
            CheckOperator(OperatorMeaning.OpenParenthesis);

            // Pick all the arguments
            var arguments = new List<IExpression>();

            if (WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                MoveNextLexeme();
            } else {
                while (currentLexeme != null) {
                    arguments.Add(GetExpression());
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }
                CheckOperator(OperatorMeaning.CloseParenthesis);
            }

            Console.WriteLine($"parsing call of '{name}'...");

            // Filter built-ins
            switch (name) {
                case "print":
                    return new BuiltinFunctionCallExpression(
                        new PrimitiveTypeInfo(PrimitiveTypeInfo.Option.Void),
                        arguments,
                        BuiltinFunctionCallExpression.Option.Print);

                default:
                    // TODO: insert syntactic checks (for arguments)
                    var maybe = functionTree.GetFunctionDefinition(name);
                    if (maybe == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    } else {
                        var definition = maybe.Value;
                        return new CustomFunctionCallExpression(definition.ResultType, arguments, definition.Number);
                    }
            }
        }


        private IExpression GetExpression() {
            return GetSumExpression();
        }


        private IExpression GetAtomicExpression() {
            switch (currentLexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.OpenParenthesis:
                            MoveNextLexeme();
                            var expression = GetExpression();
                            CheckOperator(OperatorMeaning.CloseParenthesis);
                            return expression;

                        case OperatorMeaning.OpenBracket:
                            MoveNextLexeme();
                            return GetListLiteral();

                        case OperatorMeaning.OpenBrace:
                            MoveNextLexeme();
                            return GetSetLiteral();

                        default:
                            RaiseError($"Unexpected operator: {GetOperatorName(operatorLexeme.Meaning)}");
                            return null;
                    }

                case SymbolLexeme symbol:
                    var maybe = scopeStack.GetDefinition(symbol.Text);
                    if (!maybe.HasValue) {
                        RaiseError($"Undeclared identifier '{symbol.Text}'");
                    }

                    MoveNextLexeme();
                    var definition = maybe.Value;
                    var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                    return new VariableExpression(symbol.Text, definition.Number,
                                                  frameOffset, false, definition.TypeInfo);

                case LiteralLexeme literal:
                    var typeOption = PrimitiveTypeInfo.Option.Void;
                    switch (literal.Value) {
                        case bool value:
                            typeOption = PrimitiveTypeInfo.Option.Bool;
                            break;

                        case int value:
                            typeOption = PrimitiveTypeInfo.Option.Int;
                            break;

                        case double value:
                            typeOption = PrimitiveTypeInfo.Option.Float;
                            break;

                        case string value:
                            typeOption = PrimitiveTypeInfo.Option.String;
                            break;

                        default:
                            RaiseError("Unknown literal");
                            break;
                    }
                    MoveNextLexeme();
                    return new LiteralExpression(literal.Value, new PrimitiveTypeInfo(typeOption));

                default:
                    RaiseError("Expression was expected");
                    break;
            }

            // Sanity check
            return null;
        }



        // [a, b, c, d, e]
        private IExpression GetListLiteral() {
            var (items, itemType) = GetItemList(OperatorMeaning.CloseBracket);
            return new ListLiteralExpression(items, itemType, false);
        }



        // {a, b, c, d, e}
        private IExpression GetSetLiteral() {
            var (items, itemType) = GetItemList(OperatorMeaning.CloseBrace);
            return new SetLiteralExpression(items, itemType, false);
        }



        private (List<IExpression>, ITypeInfo) GetItemList(OperatorMeaning stopOperator) {
            var items = new List<IExpression>();
            ITypeInfo typeInfo = null;
            while (true) {
                var location = currentLexeme.StartLocation;
                var item = GetExpression();
                if (typeInfo != null) {
                    if (!typeInfo.Equals(item.TypeInfo)) {
                        RaiseError($"Item's type mismatch (expected '{typeInfo.Name}', got '{item.TypeInfo.Name}')", location);
                    }
                } else {
                    typeInfo = item.TypeInfo;
                }
                items.Add(item);

                if (WhetherOperator(stopOperator)) {
                    MoveNextLexeme();
                    return (items, typeInfo);
                } else {
                    CheckOperator(OperatorMeaning.Comma);
                }
            }
        }



        // a + b, a - b, a || b
        private IExpression GetSumExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetProductExpression();
            var locationMiddle = currentLexeme.StartLocation;

            while (true) {
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
                        case OperatorMeaning.Or:
                        case OperatorMeaning.Plus:
                        case OperatorMeaning.Minus:
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var right = GetProductExpression();

                    // Try to convert to each other
                    var (x, y) = CrossConvert(left, right, locationLeft);

                    if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                        switch (primitive.TypeOption) {
                            case PrimitiveTypeInfo.Option.Bool:
                                if (meaning == OperatorMeaning.Or) {
                                    left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.Or, x, y);
                                } else {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                }
                                break;

                            case PrimitiveTypeInfo.Option.Int: {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddInteger;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        option = BinaryOperatorExpression.Option.SubtractInteger;
                                    } else {
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                    break;
                                }
                                
                            case PrimitiveTypeInfo.Option.Float: {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddFloating;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        option = BinaryOperatorExpression.Option.SubtractFloating;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                    break;
                                }

                            case PrimitiveTypeInfo.Option.String: {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddString;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                    break;
                                }

                            default:
                                RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                                break;
                        }
                    } else {
                        RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
                    }

                } else {
                    return left;
                }
            }
        }


        private (IExpression, IExpression) CrossConvert(IExpression left, IExpression right, Location location) {
            var x = left.TypeInfo.ConvertTo(left, right.TypeInfo);
            if (x != null) {
                return (x, right);
            } else {
                var y = right.TypeInfo.ConvertTo(right, left.TypeInfo);
                if (y != null) {
                    return (left, y);
                } else {
                    RaiseError("Operands of binary operator are not convertible to each other", location);
                    return (null, null);
                }
            }
        }


        private IExpression GetProductExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetNegateExpression();
            var locationMiddle = currentLexeme.StartLocation;

            while (true) {
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
                        case OperatorMeaning.And:
                        case OperatorMeaning.Asterisk:
                        case OperatorMeaning.ForwardSlash:
                        case OperatorMeaning.BackSlash:
                            break;

                        default:
                            return left;
                    }

                    // Get right operand
                    MoveNextLexeme();
                    var right = GetNegateExpression();

                    // Try to convert to each other
                    var (x, y) = CrossConvert(left, right, locationLeft);

                    if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                        BinaryOperatorExpression.Option option;
                        switch (primitive.TypeOption) {
                            case PrimitiveTypeInfo.Option.Bool:
                                if (meaning != OperatorMeaning.And) {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                }
                                option = BinaryOperatorExpression.Option.And;
                                break;

                            case PrimitiveTypeInfo.Option.Int:
                                switch (meaning) {
                                    case OperatorMeaning.Asterisk:
                                        option = BinaryOperatorExpression.Option.MultiplyInteger;
                                        break;

                                    case OperatorMeaning.BackSlash:
                                        option = BinaryOperatorExpression.Option.DivideInteger;
                                        break;

                                    case OperatorMeaning.ForwardSlash:
                                        option = BinaryOperatorExpression.Option.DivideFloating;
                                        x = x.TypeInfo.ConvertTo(x, PrimitiveTypeInfo.Float);
                                        y = y.TypeInfo.ConvertTo(y, PrimitiveTypeInfo.Float);
                                        break;

                                    default:
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                break;

                            case PrimitiveTypeInfo.Option.Float:
                                if (meaning == OperatorMeaning.Asterisk) {
                                    option = BinaryOperatorExpression.Option.MultiplyFloating;
                                } else if (meaning == OperatorMeaning.ForwardSlash) {
                                    option = BinaryOperatorExpression.Option.DivideFloating;
                                } else {
                                    RaiseError("Floating operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                break;

                            default:
                                RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                                return null;
                        }
                        left = new BinaryOperatorExpression(option, x, y);
                    } else {
                        RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
                    }

                } else {
                    return left;
                }
            }
        }


        private IExpression GetNegateExpression() {
            if (currentLexeme is OperatorLexeme operatorLexeme) {
                var meaning = operatorLexeme.Meaning;
                switch (meaning) {
                    case OperatorMeaning.Not:
                    case OperatorMeaning.Minus:
                        break;

                    default:
                        return GetAtomicExpression();
                }

                MoveNextLexeme();
                var location = currentLexeme.StartLocation;
                var atomic = GetAtomicExpression();
                var typeName = atomic.TypeInfo.Name;
                IExpression converted;

                switch (meaning) {
                    case OperatorMeaning.Not:
                        converted = atomic.TypeInfo.ConvertTo(atomic, PrimitiveTypeInfo.Bool);
                        if (converted != null) {
                            return new UnaryOperatorExpression(UnaryOperatorExpression.Option.Not, converted);
                        } else {
                            RaiseError($"Operand of logical Not must be a boolean expression (got '{typeName}')");
                            return null;
                        }

                    case OperatorMeaning.Minus:
                        converted = atomic.TypeInfo.ConvertTo(atomic, PrimitiveTypeInfo.Int);
                        if (converted != null) {
                            return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateInteger, converted);
                        } else {
                            converted = atomic.TypeInfo.ConvertTo(atomic, PrimitiveTypeInfo.Float);
                            if (converted != null) {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateFloating, converted);
                            } else {
                                RaiseError($"Operand of negation operator must be a numeric expression (got '{typeName}')");
                                return null;
                            }
                        }

                    default:
                        throw new NotImplementedException();
                }
            } else {
                return GetAtomicExpression();
            }
        }



        // [let | var] name = expression
        private VariableDeclarationStatement GetVariableDeclaration(bool isMutable) {
            var name = GetSymbolText("Variable name");
            CheckOperator(OperatorMeaning.Assignment);
            var expression = GetExpression();

            // Check scope
            if (!scopeStack.DeclareVariable(name, expression.TypeInfo, isMutable)) {
                RaiseError($"Variable '{name}' has already been declared");
            }

            return new VariableDeclarationStatement(name, expression, isMutable);
        }



        // if condition {
        //     if-statements
        // } else {
        //     else-statements
        // }
        private ConditionalStatement GetConditional() {
            var condition = GetExpression();

            // if-clause
            CheckOperator(OperatorMeaning.OpenBrace);
            scopeStack.EnterScope(isStrong: false);
            var ifStatements = GetStatementList(false);
            scopeStack.LeaveScope();
            CheckOperator(OperatorMeaning.CloseBrace);

            // else-clause
            List<IStatement> elseStatements = null;
            if (WhetherOperator(OperatorMeaning.Else)) {
                MoveNextLexeme();
                CheckOperator(OperatorMeaning.OpenBrace);
                scopeStack.EnterScope(isStrong: false);
                elseStatements = GetStatementList(false);
                scopeStack.LeaveScope();
                CheckOperator(OperatorMeaning.CloseBrace);
            }

            return new ConditionalStatement(condition, ifStatements, elseStatements);
        }



        // func name() {
        //     statements
        // }
        /*private FunctionDeclarationStatement GetFunctionDeclaration() {
            var name = GetSymbolText("Function name");
            CheckOperator(OperatorMeaning.OpenParenthesis);
            CheckOperator(OperatorMeaning.CloseParenthesis);
            CheckOperator(OperatorMeaning.OpenBrace);

            scopeStack.EnterScope(isStrong: true);
            var body = GetStatementList(true);
            scopeStack.LeaveScope();

            CheckOperator(OperatorMeaning.CloseBrace);
            return new FunctionDeclarationStatement(name, body);
        }*/


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

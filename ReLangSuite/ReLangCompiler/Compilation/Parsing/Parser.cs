using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Handmada.ReLang.Compilation.Lexing;
using Handmada.ReLang.Compilation.Yet;


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

                        default:
                            RaiseError("Unexpected operator lexeme", location);
                            return null;
                    }

                default:
                    RaiseError("Expected type literal", location);
                    return null;
            }
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
            var location = currentLexeme.StartLocation;
            var lexeme = currentLexeme;
            MoveNextLexeme();

            switch (lexeme) {
                case OperatorLexeme operatorLexeme:
                    switch (operatorLexeme.Meaning) {
                        case OperatorMeaning.If:
                            return GetConditional();

                        case OperatorMeaning.Var:
                            return GetVariableDeclaration(isMutable: true);

                        case OperatorMeaning.Let:
                            return GetVariableDeclaration(isMutable: false);

                        case OperatorMeaning.For:
                            return GetForLoop();

                        case OperatorMeaning.Return:
                            return GetReturn();

                        default:
                            RaiseError("Unknown operator lexeme found", location);
                            break;
                    }
                    break;

                case SymbolLexeme symbolLexeme:
                    if (currentLexeme is OperatorLexeme op) {
                        switch (op.Meaning) {
                            case OperatorMeaning.OpenParenthesis:
                                return new ExpressionStatement(GetFunctionCall(symbolLexeme.Text, location));

                            case OperatorMeaning.Assignment:
                                return GetAssignment(symbolLexeme.Text, location);

                            default:
                                RaiseError("Unexpected operator");
                                return null;
                        }
                    } else {
                        RaiseError("Unexpected lexeme");
                        return null;
                    }

                default:
                    RaiseError("Statement was expected", location);
                    break;
            }

            return null;
        }



        // x = y + z
        private IStatement GetAssignment(string name, Location location) {
            CheckOperator(OperatorMeaning.Assignment);

            // Check variable definition
            var maybe = scopeStack.GetDefinition(name);
            if (!maybe.HasValue) {
                RaiseError($"Undeclared identifier '{name}'", location);
            }

            var definition = maybe.Value;
            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);

            // Check if it's mutable
            if (!definition.IsMutable) {
                RaiseError($"Object '{name}' was declared as immutable, assignment is impossible", location);
            }

            var loc = currentLexeme.StartLocation;
            var value = GetExpression();
            var converted = ForceConvertExpression(value, definition.TypeInfo, loc);

            return new AssignmentStatement(name, definition.Number, frameOffset, converted);
        }



        // return expr
        private IStatement GetReturn() {
            var location = currentLexeme.StartLocation;
            var operand = GetExpression();
            var definition = functionTree.GetCurrentFunctionDefinition();

            var converted = ForceConvertExpression(operand, definition.ResultType, location);
            return new ReturnStatement(converted);
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
                scopeStack.DeclareVariable(name, itemType, true, null);
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

            Console.WriteLine($"parsing call of '{name}'...");
            FunctionDefinition definition;
            BuiltinFunctionCallExpression.Option? builtinOption = null;

            // Filter built-ins
            switch (name) {
                case "print":
                    definition = Builtins.PrintDefinition;
                    builtinOption = BuiltinFunctionCallExpression.Option.Print;
                    break;

                default:
                    var maybe = functionTree.GetFunctionDefinition(name);
                    if (maybe == null) {
                        RaiseError($"Undeclared function '{name}'", location);
                        return null;
                    } else {
                        definition = maybe.Value;
                    }
                    break;
            }

            // Pick all the arguments
            var arguments = new List<IExpression>();
            var expectedTypes = definition.ArgumentTypes;

            if (WhetherOperator(OperatorMeaning.CloseParenthesis)) {
                MoveNextLexeme();
            } else {
                for (var index = 0; currentLexeme != null; index++) {
                    var loc = currentLexeme.StartLocation;
                    var argument = GetExpression();

                    // Type checks (and conversions if necessary)
                    if (index + 1 > expectedTypes.Count) {
                        RaiseError($"Too many arguments for this function call (expected {expectedTypes.Count})", loc);
                        return null;
                    } else {
                        var expectedType = expectedTypes[index];
                        var converted = TryConvertExpression(argument, expectedType);
                        if (converted == null) {
                            RaiseError($"Cannot convert given argument to target type (expected '{expectedType.Name}'"
                                       + $" but got '{argument.TypeInfo.Name}')", loc);
                            return null;
                        } else {
                            argument = converted;
                        }
                    }

                    arguments.Add(argument);
                    if (WhetherOperator(OperatorMeaning.Comma)) {
                        MoveNextLexeme();
                    } else {
                        break;
                    }
                }

                CheckOperator(OperatorMeaning.CloseParenthesis);
            }

            // Final count check
            if (arguments.Count != expectedTypes.Count) {
                RaiseError($"Wrong number of arguments for this function call (expected {expectedTypes.Count}"
                           + $" but got {arguments.Count})", location);
                return null;
            }

            // return appropriate function call expression
            if (builtinOption != null) {
                return new BuiltinFunctionCallExpression(definition.ResultType, arguments, builtinOption.Value);
            } else {
                return new CustomFunctionCallExpression(definition.ResultType, arguments, definition.Number);
            }
        }


        private IExpression GetExpression() {
            return GetOrExpression();
        }



        // variable, f(x, y, z), (a + b), [a, b, c], {a, b, c}, literal
        private IExpression GetAtomicExpression() {
            var location = currentLexeme.StartLocation;
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
                    MoveNextLexeme();
                    if (WhetherOperator(OperatorMeaning.OpenParenthesis)) {
                        return GetFunctionCall(symbol.Text, location);

                    } else {
                        var maybe = scopeStack.GetDefinition(symbol.Text);
                        if (!maybe.HasValue) {
                            RaiseError($"Undeclared identifier '{symbol.Text}'");
                        }
                        
                        var definition = maybe.Value;
                        if (!definition.IsMutable && definition.Value != null && definition.Value.IsCompileTime) {
                            // Can be evaluated at compile-time
                            var value = definition.Value;
                            return new PrimitiveLiteralExpression(value.Value, value.TypeInfo);
                        } else {
                            // Should be resolved at run-time
                            var frameOffset = definition.ScopeNumber - (scopeStack.Count - 1);
                            return new VariableExpression(symbol.Text, definition.Number,
                                                          frameOffset, false, definition.TypeInfo);
                        }
                    }

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
                    return new PrimitiveLiteralExpression(literal.Value, new PrimitiveTypeInfo(typeOption));

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
            return new ListLiteralExpression(items, itemType);
        }



        // {a, b, c, d, e}
        private IExpression GetSetLiteral() {
            var (items, itemType) = GetItemList(OperatorMeaning.CloseBrace);
            return new SetLiteralExpression(items, itemType);
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



        // a || b
        private IExpression GetOrExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetAndExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (WhetherOperator(OperatorMeaning.Or)) {
                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetAndExpression();

                    // Try to convert to boolean
                    var x = ForceConvertExpression(left, PrimitiveTypeInfo.Bool, locationLeft);
                    var y = ForceConvertExpression(right, PrimitiveTypeInfo.Bool, locationRight);

                    if (x.IsCompileTime) {
                        if ((bool)x.Value) {
                            left = new PrimitiveLiteralExpression(true, PrimitiveTypeInfo.Bool);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.Or, x, y);
                    }

                } else {
                    return left;
                }
            }
        }



        // a && b
        private IExpression GetAndExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetRelationalExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (WhetherOperator(OperatorMeaning.And)) { 
                    // Get right operand
                    MoveNextLexeme();
                    var locationRight = currentLexeme.StartLocation;
                    var right = GetRelationalExpression();

                    // Try to convert to boolean
                    var x = ForceConvertExpression(left, PrimitiveTypeInfo.Bool, locationLeft);
                    var y = ForceConvertExpression(right, PrimitiveTypeInfo.Bool, locationRight);

                    if (x.IsCompileTime) {
                        if (!(bool)x.Value) {
                            left = new PrimitiveLiteralExpression(false, PrimitiveTypeInfo.Bool);
                        } else {
                            left = y;
                        }
                    } else {
                        // Short-circuit evaluation:
                        // (y) shouldn't be evaluated before (x)
                        left = new BinaryOperatorExpression(BinaryOperatorExpression.Option.And, x, y);
                    }

                } else {
                    return left;
                }
            }
        }



        // a > b, a != b
        private IExpression GetRelationalExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetRangeExpression();

            var locationMiddle = currentLexeme.StartLocation;

            if (currentLexeme is OperatorLexeme operatorLexeme) {
                var meaning = operatorLexeme.Meaning;
                switch (meaning) {
                    case OperatorMeaning.Equal:
                    case OperatorMeaning.NotEqual:
                    case OperatorMeaning.Less:
                    case OperatorMeaning.LessOrEqual:
                    case OperatorMeaning.More:
                    case OperatorMeaning.MoreOrEqual:
                        break;

                    default:
                        return left;
                }

                // Get right operand
                MoveNextLexeme();
                var right = GetRangeExpression();

                // Try to convert to each other
                var (x, y) = CrossConvert(left, right, locationLeft);
                
                if (x.TypeInfo is PrimitiveTypeInfo primitive) {
                    switch (primitive.TypeOption) {
                        case PrimitiveTypeInfo.Option.Bool:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (bool)x.Value;
                                var b = (bool)y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualBoolean;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualBoolean;
                                } else {
                                    RaiseError("Boolean operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Int:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (int)x.Value;
                                var b = (int)y.Value;
                                var result = false;

                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        result = a == b;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        result = a != b;
                                        break;

                                    case OperatorMeaning.Less:
                                        result = a < b;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        result = a <= b;
                                        break;

                                    case OperatorMeaning.More:
                                        result = a > b;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        result = a >= b;
                                        break;

                                    default:
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        option = BinaryOperatorExpression.Option.EqualInteger;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        option = BinaryOperatorExpression.Option.NotEqualInteger;
                                        break;

                                    case OperatorMeaning.Less:
                                        option = BinaryOperatorExpression.Option.LessInteger;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        option = BinaryOperatorExpression.Option.LessOrEqualInteger;
                                        break;

                                    case OperatorMeaning.More:
                                        option = BinaryOperatorExpression.Option.MoreInteger;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        option = BinaryOperatorExpression.Option.MoreOrEqualInteger;
                                        break;

                                    default:
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Float:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (double)x.Value;
                                var b = (double)y.Value;
                                var result = false;

                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        result = a == b;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        result = a != b;
                                        break;

                                    case OperatorMeaning.Less:
                                        result = a < b;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        result = a <= b;
                                        break;

                                    case OperatorMeaning.More:
                                        result = a > b;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        result = a >= b;
                                        break;

                                    default:
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                switch (meaning) {
                                    case OperatorMeaning.Equal:
                                        option = BinaryOperatorExpression.Option.EqualFloating;
                                        break;

                                    case OperatorMeaning.NotEqual:
                                        option = BinaryOperatorExpression.Option.NotEqualFloating;
                                        break;

                                    case OperatorMeaning.Less:
                                        option = BinaryOperatorExpression.Option.LessFloating;
                                        break;

                                    case OperatorMeaning.LessOrEqual:
                                        option = BinaryOperatorExpression.Option.LessOrEqualFloating;
                                        break;

                                    case OperatorMeaning.More:
                                        option = BinaryOperatorExpression.Option.MoreFloating;
                                        break;

                                    case OperatorMeaning.MoreOrEqual:
                                        option = BinaryOperatorExpression.Option.MoreOrEqualFloating;
                                        break;

                                    default:
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.String:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = (string)x.Value;
                                var b = (string)y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("String operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

                            } else {
                                // Delay until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualString;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualString;
                                } else {
                                    RaiseError("String operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y);
                            }
                            break;


                        case PrimitiveTypeInfo.Option.Object:
                            if (x.IsCompileTime && y.IsCompileTime) {
                                // Evaluate at compile-time
                                var a = x.Value;
                                var b = y.Value;
                                var result = false;

                                if (meaning == OperatorMeaning.Equal) {
                                    result = a == b;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    result = a != b;
                                } else {
                                    RaiseError("Object operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Bool);

                            } else {
                                // Delay evaluation until run-time
                                BinaryOperatorExpression.Option option;
                                if (meaning == OperatorMeaning.Equal) {
                                    option = BinaryOperatorExpression.Option.EqualObject;
                                } else if (meaning == OperatorMeaning.NotEqual) {
                                    option = BinaryOperatorExpression.Option.NotEqualObject;
                                } else {
                                    RaiseError("Object operands don't support this operator", locationMiddle);
                                    return null;
                                }
                                left = new BinaryOperatorExpression(option, x, y);
                            } 
                            break;


                        default:
                            RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                            return null;
                    }

                } else {
                    RaiseError($"Unsupported type '{x.TypeInfo.Name}' for this operator", locationLeft);
                }
            }

            return left;
        }



        // a..b
        private IExpression GetRangeExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetSumExpression();

            if (WhetherOperator(OperatorMeaning.Range)) {
                MoveNextLexeme();

                var locationRight = currentLexeme.StartLocation;
                var right = GetSumExpression();

                var intType = PrimitiveTypeInfo.Int;
                var x = ForceConvertExpression(left, intType, locationLeft);
                var y = ForceConvertExpression(right, intType, locationRight);

                if (x.IsCompileTime && y.IsCompileTime) {
                    var a = (int)x.Value;
                    var b = (int)y.Value;

                    if (a >= b) {
                        RaiseError($"Empty range: start index (got {a}) must be smaller than end one (got {b})", locationLeft);
                    }
                }

                return new RangeLiteralExpression(x, y);
            } else {
                return left;
            }
        }



        // a + b, a - b
        private IExpression GetSumExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetProductExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
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
                            case PrimitiveTypeInfo.Option.Int:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (int)x.Value;
                                    var b = (int)y.Value;
                                    var result = 0;

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        result = a - b;
                                    } else {
                                        RaiseError("Integer operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Int);

                                } else {
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
                                }
                                break;


                            case PrimitiveTypeInfo.Option.Float:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (double)x.Value;
                                    var b = (double)y.Value;
                                    var result = 0.0;

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else if (meaning == OperatorMeaning.Minus) {
                                        result = a - b;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float);

                                } else {
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
                                }
                                break;


                            case PrimitiveTypeInfo.Option.String:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (string)x.Value;
                                    var b = (string)y.Value;
                                    var result = "";

                                    if (meaning == OperatorMeaning.Plus) {
                                        result = a + b;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.String);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Plus) {
                                        option = BinaryOperatorExpression.Option.AddString;
                                    } else {
                                        RaiseError("String operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                }
                                break;


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
                    var hint = $"('{left.TypeInfo.Name}' and '{right.TypeInfo.Name}')";
                    RaiseError($"Operands of binary operator {hint} are not convertible to each other", location);
                    return (null, null);
                }
            }
        }



        // a * b, a / b, a \ b, a % b
        private IExpression GetProductExpression() {
            var locationLeft = currentLexeme.StartLocation;
            var left = GetNegateExpression();

            while (true) {
                var locationMiddle = currentLexeme.StartLocation;
                if (currentLexeme is OperatorLexeme operatorLexeme) {
                    var meaning = operatorLexeme.Meaning;
                    switch (meaning) {
                        case OperatorMeaning.Asterisk:
                        case OperatorMeaning.ForwardSlash:
                        case OperatorMeaning.BackSlash:
                        case OperatorMeaning.Modulo:
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
                        switch (primitive.TypeOption) {
                            case PrimitiveTypeInfo.Option.Int:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (int)x.Value;
                                    var b = (int)y.Value;
                                    var typeInfo = PrimitiveTypeInfo.Int;
                                    object result;

                                    switch (meaning) {
                                        case OperatorMeaning.Asterisk:
                                            result = a * b;
                                            break;

                                        case OperatorMeaning.BackSlash:
                                            result = a / b;
                                            break;

                                        case OperatorMeaning.ForwardSlash:
                                            result = (double)a / b;
                                            typeInfo = PrimitiveTypeInfo.Float;
                                            break;

                                        case OperatorMeaning.Modulo:
                                            result = a % b;
                                            break;

                                        default:
                                            RaiseError("Integer operands don't support this operator", locationMiddle);
                                            return null;
                                    }

                                    left = new PrimitiveLiteralExpression(result, typeInfo);

                                } else {
                                    BinaryOperatorExpression.Option option;
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

                                        case OperatorMeaning.Modulo:
                                            option = BinaryOperatorExpression.Option.Modulo;
                                            break;

                                        default:
                                            RaiseError("Integer operands don't support this operator", locationMiddle);
                                            return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                }
                                break;


                            case PrimitiveTypeInfo.Option.Float:
                                if (x.IsCompileTime && y.IsCompileTime) {
                                    var a = (double)x.Value;
                                    var b = (double)y.Value;
                                    var result = 0.0;

                                    if (meaning == OperatorMeaning.Asterisk) {
                                        result = a * b;
                                    } else if (meaning == OperatorMeaning.ForwardSlash) {
                                        result = a / b;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }

                                    left = new PrimitiveLiteralExpression(result, PrimitiveTypeInfo.Float);

                                } else {
                                    BinaryOperatorExpression.Option option;
                                    if (meaning == OperatorMeaning.Asterisk) {
                                        option = BinaryOperatorExpression.Option.MultiplyFloating;
                                    } else if (meaning == OperatorMeaning.ForwardSlash) {
                                        option = BinaryOperatorExpression.Option.DivideFloating;
                                    } else {
                                        RaiseError("Floating operands don't support this operator", locationMiddle);
                                        return null;
                                    }
                                    left = new BinaryOperatorExpression(option, x, y);
                                }
                                break;


                            default:
                                RaiseError($"Unsupported type '{primitive.Name}' for this operator", locationLeft);
                                return null;
                        }

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
                            if (converted.IsCompileTime) {
                                var a = (bool)converted.Value;
                                return new PrimitiveLiteralExpression(!a, PrimitiveTypeInfo.Bool);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.Not, converted);
                            }  
                        } else {
                            RaiseError($"Operand of logical Not must be a boolean expression (got '{typeName}')");
                            return null;
                        }


                    case OperatorMeaning.Minus:
                        converted = atomic.TypeInfo.ConvertTo(atomic, PrimitiveTypeInfo.Int);
                        if (converted != null) {
                            if (converted.IsCompileTime) {
                                var a = (int)converted.Value;
                                return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Int);
                            } else {
                                return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateInteger, converted);
                            }
                            
                        } else {
                            converted = atomic.TypeInfo.ConvertTo(atomic, PrimitiveTypeInfo.Float);
                            if (converted != null) {
                                if (converted.IsCompileTime) {
                                    var a = (double)converted.Value;
                                    return new PrimitiveLiteralExpression(-a, PrimitiveTypeInfo.Float);
                                } else {
                                    return new UnaryOperatorExpression(UnaryOperatorExpression.Option.NegateFloating, converted);
                                }
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
            var value = GetExpression();

            // Check scope
            if (!scopeStack.DeclareVariable(name, value.TypeInfo, isMutable, value)) {
                RaiseError($"Variable '{name}' has already been declared");
            }

            return new VariableDeclarationStatement(name, value, isMutable);
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

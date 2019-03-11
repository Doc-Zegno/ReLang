# Tutorial
## Version info
The content of this tutorial is relevant only for the 1st Revision of Re:Lang.
Upcoming 2nd one is going to bring some dramatic changes to language



## Basic syntax
### Program structure
Program is a set of global functions. Their order doesn't matter.
**Please note that global data is not allowed**.

Program's entry point is always a global function called `main` which
must also have a list of strings as the only parameter


### Naming rules
Names of *variables*, *constants*, *function's parameters* and *functions*
**must start with an lowercased letter**.

Names of *types* always start with **an Uppercased letter**.

Keep in mind that you cannot use underscore (`_`) for names composing.
You should use *camelCase* convention instead


### Function definition
Functions are defined like this:
```swift
func parseJson(json: String) -> {String: String} {
    statement1
    statement2
    statement3
}
```

Please note that unlike C++ parameter's type is placed just after its name
and not in front of it. The same is true for function's return type.

Also note that you don't have to separate statements with a semicolon (`:`)
and actually it's a syntax error to do so.

It's completely legal to define a function inside another function:
```swift
func reverseLispStype(items: [Int]) -> [Int] {
    func iter(remaining: [Int], result: [Int]) -> [Int] {
        // Some code
    }
    return iter(items, [])
}
```

Keep in mind that such a definition is not going to generate a *closure*
(unlike JS) and it was introduced only to hide
helper functions from the outside world


### Variable definition
In Re:Lang you can define *variables*, *constants* and *disposables*:
```swift
var number = 3  // Can be reassigned later
let indices = [0, 1, 3]  // Cannot be reassigned, cannot change list
use fin = open("input.txt")  // Will be automatically closed when scope ends
```

By declaring a *variable* you are allowed to reassing it and also change the
state of object it's pointing to.

By declaring a *constant* you are not allowed to do anything.

By declaring a *disposable* you are allowed to change object's state
but not to reassign a reference


### Control flow
`if`-statement is used like this:
```swift
if login in blackList {
    print("Sorry but you're banned")
}
```

`else`- and `elif`-brances can also be used with it:
```swift
if x > 0 {
    print("Positive")
} elif x == 0 {
    print("Not so positive")
} else {
    print("Not positive at all")
}
```

`for`-loop is used like this:
```swift
let numbers = [0, 1, 3]
for number in numbers {
    print(number)
}
```

C++-like `while`- and `do-while`-loops are also available:
```swift
while true {
    runService()
}
```

You can use `break` and `continue` to interrupt loop flow:
```swift
labeled = {}  // some set
for i in 0..10 {
    if items[i] in labeled {
        break
    }
}
```



## Type system
The table below is a short overview of built-in data types commonly used in Re:Lang

Type's name (shorthand) | Example of type's literal | Default value
----------------------- | ------------------------- | -------------
`Void` | *(missing)* | *(missing)*
`Bool` | `true` | `false`
`Char` | *(null character)* | `A`
`Int` | `2451` | `0`
`Float` | `-137.42` | `0.0`
`String` | `"Sample text"` | `""`
`ArrayList<T>` (`[T]`) | `[1, 2, 3, 4, 5]` | `[]`
`HashSet<T>` (`{T}`) | `{"Sample", "Text"}` | `{}`
`Dictionary<K, V>` (`{K: V}`) | `{"Sample": 137}` | `{:}`
`Iterable<T>` (`T*`) | *(missing)* | *(missing)*
`Maybe<T>` (`T?`) | *(missing)* | `null`
`Range` | `0..10` | *(missing)*
`Object` | *(missing)* | *(missing)*

 
### Conversions
Usually you must perform conversion explicitly
through the construction of a brend new object, like this:

```swift
let string = "Sample text"
let letters = [Char](string)
```

Despite this, there are a few implicit conversions still available:
 * `Int` -> `Float`
 * *(Subclass)* -> *(Super class)*
 * `T` -> `T?`

Notable case is for iterables:
```swift
let numbers = [0, 1, 3]
let sequence: Object* = numbers  // Note nested upcasting from Int to Object
```



## Finality and mutability
### Base principles
 * Identifier is **final** if it can be reassigned to another object
 * Identifier is **mutable** if it's possible to change
   the state of object it's pointing to
 * Finality is a property of *identifier* and not *type*
 * Mutability is a property of referenced *object* and not *type*
 * Immutability is a transitive relation: you cannot define
   a variable and make it point to a constant

### Functions
 * Parameters are final and immutable by default
 * Resulting object is mutable by default
 
The function's central philosophy beyond these conventions is the following:
> I don't change given arguments if I don't have to
> and I don't care what you will do with the resulting object.

To make parameter mutable, use `mutable` keyword:
```swift
func sortInPlace(items: mutable [Int]) { ... }
```

To make resulting object immutable, use `const` keyword:
```swift
func getObjectList() -> const [Object] { ... }
```



## Examples
### Factorial
**Featuring:** `if-elif-else`-branching, error handling with `raise`,
string interpolation with `$"{}"`
```swift
func main(args: [String]) {
    print(fact(0))
    print(fact(10))
    print(fact(-5))
}


// Fun fact: "func fact" can be misread as "fun fact"
func fact(n: Int) -> Int {
    if n > 0 {
        return n * fact(n - 1)
    } elif n == 0 {
        return 1
    } else {
        raise ValueError($"Argument must be positive (got {n})")
    }
}
```

Output:
```
1
3628800
Error has occured during program's execution:
  at line 4 at column 15
        print(fact(-5))
                  ^
  at line 15 at column 19
                raise ValueError($"Argument must be positive (got {n})")
                      ^
ValueError: Argument must be positive (got -5)
```


### Fibonacci
**Featuring:** range, `for-each`-loop, tuple, tuple unpacking
```swift
func main(args: [String]) {
    for i in 0..10 {
        print(fib(i))
    }
}


func fib(n: Int) -> Int {
    var x, y = 0, 1
    for i in 0..n {
        x, y = y, x + y
    }
    return x
}
```

Output:
```
0
1
1
2
3
5
8
13
21
34
```



### Tail recursion. How do you like it, Lisp?
**Featuring:** nested functions, string concatenation
```swift
func main(args: [String]) {
    print(repeat("AAA", 3))
    print(repeat("Hello", 5))
}


func repeat(message: String, times: Int) -> String {
    func iter(message: String, times: Int, result: String) -> String {
        if times <= 0 {
            return result + ""
        } else {
            return iter(message, times - 1, result + message)
        }
    }

    return iter(message, times, "")
}
```

Output:
```
AAAAAAAAA
HelloHelloHelloHelloHello
```


### Iterating over sequences
**Featuring:** list, set and iterable interface
```swift
func main(args: [String]) {
    let numbers = [0, 1, 3]  // List of Int's
    let words = {"Sample", "Text"}  // List of String's
    var collections: [Object*] = []  // Empty list of Iterable<Object>
    collections.append(numbers)
    collections.append(words)

    for collection in collections {
        print(collection)
    }
}
```

Output:
```
[0, 1, 3]
{"Sample", "Text"}
```


### Iterating over sequences 2
**Featuring:** dictionary
```swift
func main(args: [String]) {
    let logins2stats = {"Kunsar": (1, 88.5), "n00b": (2, 70.6)}
    for login, (id, score) in logins2stats {
        print($"login: {login}, id: {id}, score: {score}")
    }
}
```

Output:
```
login: Kunsar, id: 1, score: 88.5
login: n00b, id: 2, score: 70.6
```


### Iterating over sequences 3
**Featuring:** `enumerate`, `zip`
```swift
func main(args: [String]) {
    let logins = ["Kunsar", "n00b"]
    let scores = {88.5, 70.6}

    for id, (login, score) in enumerate(zip(logins, scores)) {
        print($"id: {id}, login: {login}, score: {score}")
    }
}
```

Output:
```
id: 0, login: Kunsar, score: 88.5
id: 1, login: n00b, score: 70.6
```


### Slices
**Featuring:** list slices
```swift
func main(args: [String]) {
    let numbers = List(0..10)  // Convert range to list
    let evens = numbers[::2]
    let odds = numbers[1::2]
    let firstTwoOdds = odds[:2]

    print(evens)
    print(odds)
    print(firstTwoOdds)
}
```

Output:
```
[0, 2, 4, 6, 8]
[1, 3, 5, 7, 9]
[1, 3]
```


### Null-safety
**Featuring:** maybe, retrieving value from maybe, value or default
```swift
func main(args: [String]) {
    print(fact(10)!)
    print(fact(-5) ?? -1)
}


func fact(n: Int) -> Int? {
    if n > 0 {
        return n * fact(n - 1)!
    } elif n == 0 {
        return 1
    } else {
        return null
    }
}
```

Output:
```
3628800
-1
```


### Null-safety with `if`
**Featuring:** conditional unwrapping of maybe
```swift
func main(args: [String]) {
    let cheaters2bans = {"Anti Cheater": 2, "Best Quickscoper": 137}
    let names = ["Kunsar", "Admin", "Anti Cheater", "n00b", "Best Quickscoper"]

    for name in names {
        if let numBans = cheaters2bans.tryGet(name) {
            print($"{name} has been banned {numBans} times")
        }
    }
}
```

Output:
```
Anti Cheater has been banned 2 times
Best Quickscoper has been banned 137 times
```


### Simple text file viewer (Holy sh*t!)
**Featuring:** disposables, files
```swift
func main(args: [String]) {
    use fin = open("input.txt")
    for lineNumber, line in enumerate(fin) {
        print($"#{lineNumber}\t{line}")
    }
}
```

Output:
```
#0      func main(args: [String]) {
#1          use fin = open("input.txt")
#2          for lineNumber, line in enumerate(fin) {
#3              print($"#{lineNumber}\t{line}")
#4          }
#5      }
```
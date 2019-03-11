# ReLang
![Big logo](https://i.imgur.com/Xc607oz.png)
![Textual logo](https://i.imgur.com/fEvpgxB.png)

(logo was remastered by [@razergom](https://github.com/razergom))

Compiler and interpreter for Re:Lang


## About Re:Lang
### Overview
Re:Lang is a modern statically typed imperative programming language.

Main goal of Re:Lang is to ease process of programming and make code listings
much more readable.

If you want to become more familiar with Re:Lang, consider reading
[this tutorial](Docs/TUTORIAL)


### Version
Current version is 0.1.0, also known as "The First Revision".

Please note that there is a certain lack of essential functionality in this revision
and current mutability system seems to be out of date
so it's not suitable for "serious" programming.

If you are interested in version which is compiled into fast native code
and supplied with lots of features, you may want to give this repo a star.

If there are enough stars, I'll start preparing the 2nd Revision of Re:Lang
which is going to be a much better one


## How to use it
### Prerequisites
You will need a Windows PC with VS 2017 on board


### How to build
Open VS solution `ReLangSuite` and build project `ReLangCompiler`.
After you've done, please, proceed to output directory
(which should be `ReLangCompiler/bin/Debug`) and make sure there is
an executable `ReLangCompiler`


### How to compile custom program
Create a text file `"input.txt"` *near the compiler's executable*,
write down some code here and launch compiler via PowerShell or cmd.

You may want to specify program parameters as well:
```
.\ReLangCompiler.exe hello world
```

Your program will be compiled and immediately executed by built-in virtual machine
so you should see an output in console between `Executing main()...`
and `Process finished with exit code: 0`


## Overall TODO List
- [x] Variable resolution
- [x] Function resolution
- [x] Custom functions with arguments
- [x] Type checks and implicit conversions
- [x] Complex expressions
- [x] Explicit conversions (`Int("5")`, `String(5)`)
- [x] `Char` data type (and iterating over string)
- [x] Shorthand operators (`+=`, `++`)
- [x] `for-each`-style loop
- [x] `do-while` and `while` loops
- [ ] C-style `for` loop
- [x] Tuples and tuple unpacking
- [x] Collections (lists, sets and dictionaries)
- [x] Maybes, conditional unwrapping
- [x] Checks for immutable objects
- [ ] Built-in high-order functions (`filter`, `map`, `reduce`, `zip`)
- [x] Built-in methods for collections (including indexing)
- [x] Slices for lists
- [x] Built-in methods and slices for strings
- [x] Files and managable disposables (`use f = open("input.txt")`)
- [ ] Namespaces with util functions
- [x] Runtime! 


## Expansion Packs
### Lord of the strings (String DLC)
![progress bar](http://progressed.io/bar/100?title=released)

This pack includes:
- [x] Basic methods for strings (like `contains`, `toUpper`, `toLower`, `join`)
- [x] Slices for strings
- [x] Verbatim strings (`@"C:\Program Files (x86)\Handmada\Yet Studio"`)
- [x] String interpolation (`$"({x}, {y})"`)


### Ready to work (Disposable DLC)
![progress bar](http://progressed.io/bar/100?title=released)

This pack includes:
- [x] Generic wrappers for sequences (`enumerate` and `zip`)
- [x] Files: tools for opening, reading, iterating and disposing
- [x] VM's support for `Disposable` interface
- [x] Exception handling with `try`-`catch`
- [x] Raising errors with `raise`
- [x] Optional arguments (especially for `print` and `open`)
- [x] Overloaded util functions (like `min`, `max`)
- [x] Empty collections' literals (`[]`, `{}`, `{:}`)
- [x] Constructors for collections (`[Int]()`, `[Int](n)`)
- [x] Special "I don't care" identifier `_`
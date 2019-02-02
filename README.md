# ReLang
![Big logo](https://i.imgur.com/oyGhM25.png)
![Textual logo](https://i.imgur.com/fEvpgxB.png)

Compiler and interpreter for Re:Lang


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
- [ ] Built-in methods and slices for strings
- [ ] Files and managable disposables (`use f = open("input.txt")`)
- [ ] Namespaces with util functions
- [x] Runtime! 


## Expansion Packs
### Lord of the strings (String DLC)
![progress bar](http://progressed.io/bar/67?title=progress)

This pack includes:
- [x] Basic methods for strings (like `contains`, `toUpper`, `toLower`, `join`)
- [x] Slices for strings
- [x] Verbatim strings (`@"C:\Program Files (x86)\Handmada\Yet Studio"`)
- [ ] String interpolation (`$"({x}, {y})"`)
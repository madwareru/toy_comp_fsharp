## toy_comp_fsharp
This is an implementation of [toy_comp](https://github.com/true-grue/toy_comp) using the F# language.

### What's inside
The project includes:
- [x] Specification for a **memory-to-memory**-based ISA
- [x] Specification for a **stack**-based ISA
- [x] Specification for a **register**-based ISA
- [x] Grammar definition for a simple imperative language (let's call it **L**)
- [x] Semantics definition for each construct of **L**
- [x] Parser for a **memory-to-memory**-based ISA assembly
- [x] Parser for a **stack**-based ISA assembly
- [x] Parser for a **register**-based ISA assembly
- [x] Interpreter for a **memory-to-memory**-based ISA assembly
- [x] Interpreter for a **stack**-based ISA assembly
- [x] Interpreter for a **register**-based ISA assembly
- [x] Parser for **L**
- [x] Interpreter for **L**
- [x] Compiler from **L** to a **memory-to-memory**-based ISA assembly, both as text and as ISA types
- [x] Compiler from **L** to a **stack**-based ISA assembly, both as text and as ISA types
- [x] Compiler from **L** to a **register**-based ISA assembly, both as text and as ISA types
- [x] Simple constant-folding optimization to make generated assembly more compact

### What this project does differently compared to the original toy_comp project
The key difference from the original project is the syntax of language **L**.

While **L** is a subset of **Python** in the original project and is parsed by Python's standard **ast** module,
this is not convenient to represent in an implementation that is not written in Python.

To address this, the grammar was changed slightly to make the syntax closer to **Ruby**
(with the **end** keyword as a closing token for a block). A semicolon (**';'**) is also mandatory at the end of each
statement to simplify parsing. The syntax is parsed using the parser combinator library **FParsec**.

So, if the original code looked like this:
```python
limit = 50
n = 2

while n <= limit:
    d = 2
    prime = 1
    while d < n:
        rest = n
        while d <= rest:
            rest = rest - d
        prime = prime * (0 < rest)
        d = d + 1
    if prime == 1:
        out(n)
    n = n + 1
```

Then the code in this implementation will look like this:
```ruby
limit = 50;
n = 2;

while n <= limit:
    d = 2;
    prime = 1;
    
    while d < n:
        rest = n;
        
        while d <= rest:
            rest = rest - d;
        end;
        
        prime = prime * (0 < rest);
        d = d + 1;
    end;
    
    if prime == 1:
        outn(n);
    end;
    
    n = n + 1;
end;
```

There is also an additional `OUTN` operation
to distinguish between printing an ASCII character
and printing a decimal number followed by a newline.

The file extension for program source files was changed from
**'.mpy'** to **'.mrb'** to reflect the changes mentioned earlier.

This project also allows users to write negative integer literals
in assembly files (for example, `#-42` represents `-42`). This was added
to support constant folding in cases such as `0 - 9` in the Mandelbrot example.

### Future plans
There are some interesting extra work that could be made with this project:
- [ ] Semantic code analyzer to detect an access to undefined variables
- [ ] Bytecode design for a **memory-to-memory**-based ISA
- [ ] Bytecode design for a **stack**-based ISA
- [ ] Bytecode design for a **register**-based ISA
- [ ] Assembly -> bytecode compiler for a **memory-to-memory**-based ISA
- [ ] Assembly -> bytecode compiler for a **stack**-based ISA
- [ ] Assembly -> bytecode compiler for a **register**-based ISA
- [ ] Bytecode VM for a **memory-to-memory**-based ISA in C
- [ ] Bytecode VM for a **stack**-based ISA in C
- [ ] Bytecode VM for a **register**-based ISA in C

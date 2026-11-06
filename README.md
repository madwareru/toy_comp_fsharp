## toy_comp_fsharp
This is implementation of a [toy_comp](https://github.com/true-grue/toy_comp) using F# language

### What's inside
The project includes:
- [x] Specification for a **memory-to-memory** based ISA
- [ ] Bytecode design for a **memory-to-memory** based ISA
- [x] Specification for a **stack** based ISA
- [ ] Bytecode design for a **stack** based ISA
- [x] Specification for a **register** based ISA
- [ ] Bytecode design for a **register** based ISA
- [x] Grammar definition for a simple imperative language (let's call it **L**)
- [x] Semantics definition for each construct of **L**
- [x] Parser for a **memory-to-memory** based ISA assembly
- [x] Parser for a **stack** based ISA assembly
- [x] Parser for a **register** based ISA assembly
- [x] Interpreter for a **memory-to-memory** based ISA assembly
- [x] Interpreter for a **stack** based ISA assembly
- [x] Interpreter for a **register** based ISA assembly
- [x] Parser for **L**
- [x] Interpreter of **L**
- [x] Compiler from **L** to a **memory-to-memory** based 
      ISA assembly both in text and into ISA types 
- [x] Compiler from **L** to a **stack** based
      ISA assembly both in text and into ISA types
- [x] Compiler from **L** to a **register** based
      ISA assembly both in text and into ISA types
- [x] Simple constant folding optimization to make a generated assembly more compact
- [ ] Assembly -> bytecode compiler for a **memory-to-memory** based ISA
- [ ] Assembly -> bytecode compiler for a **stack** based ISA
- [ ] Assembly -> bytecode compiler for a **register** based ISA
- [ ] Bytecode VM for a **memory-to-memory** based ISA in C language
- [ ] Bytecode VM for a **stack** based ISA in C language
- [ ] Bytecode VM for a **register** based ISA in C language

### What this project does differently in comparison to original toy_comp project
The key difference from an original project is a syntax for a language **L**.

While **L** is a subset of a **python** in an original project, which is being 
parsed by standard python **ast** module, this is not convenient to represent in 
an implementation which is not written in python.

By this fact a slightl change in a grammar was made, making syntax closer to **ruby** 
(with **end** keyword as a closing token for a block). There is also a mandatory **';'** and the end of each 
statement to simplify parsing. The syntax is being parsed by the parser combinator 
library **FParsec**.

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

There are also additional operation `OUTN` introduced 
to differentiate between the need to put ASCII character
or to print a line with a decimal number.

File extension for program texts are changed from 
**'.mpy'** to **'.mrb'** to reflect changes mentioned earlier.

For this project there are also a possibility for the user to write 
negative integer literals in assembly files (which will have a look 
like #-42 for -42, for example). This was made to add constant 
folding for cases like 0 - 9 in a mandelbrot example.
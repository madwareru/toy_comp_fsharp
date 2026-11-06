### Language overview
The language is a small imperative language with global mutable variables, signed 64-bit integer values, arithmetic and comparison expressions, conditionals, loops, and buffered text output. Source files use the `.mrb` extension.

A program is a sequence of statements. There are no declarations, functions, types, or local scopes. Indentation is insignificant, and blocks are delimited by `if`, `else`, `while`, and `end` keywords.

Syntactically valid programs must also access only variables that have been defined. A program that accesses an undefined variable is considered incorrect and must never be executed or compiled.

### Lexical elements
- **Identifier**

  Identifiers name global variables. They start with an ASCII letter and continue with zero or more ASCII letters, digits, or underscores.

  ```regexp
  [A-Za-z][A-Za-z0-9_]*
  ```

  Examples are `n`, `result`, `value2`, and `very_long_name`. An identifier cannot start with a digit or an underscore.

- **Integer literal**

  An integer literal is an optional sign followed by one or more decimal digits. Its value must fit in a signed 64-bit integer.

  ```regexp
  [+-]?[0-9]+
  ```

  Examples are `0`, `42`, `-42`, and `+42`.

- **Keywords**

  The following words introduce language constructs:

  ```text
  if else end while out outn
  ```

  They should not be used as variable names.

- **Punctuation and operators**

  ```text
  = ; : ( ) == <= < + - *
  ```

  Assignment uses `=`, while equality comparison uses `==`.

- **Whitespace**

  Spaces, tabs, and newlines may appear between tokens. Newlines and indentation improve readability but do not determine block structure. The language has no comment syntax.

### Grammar notation
The grammar below uses Extended Backus–Naur Form:

- `|` separates alternatives.
- `{ element }` repeats an element zero or more times.
- `[ element ]` makes an element optional.
- Parentheses group grammar elements.
- Text in double quotes is literal source text.

### Program and statement grammar
```ebnf
program        = block, end-of-input ;

block          = { statement } ;

statement      = ( assignment
                 | character-output
                 | number-output
                 | if-statement
                 | while-statement
                 ), ";" ;

assignment     = identifier, "=", expression ;

character-output
               = "out", "(", expression, ")" ;

number-output  = "outn", "(", expression, ")" ;

if-statement   = "if", expression, ":", block,
                 [ "else", ":", block ],
                 "end" ;

while-statement
               = "while", expression, ":", block, "end" ;

identifier     = letter, { letter | digit | "_" } ;

letter         = "A" | ... | "Z" | "a" | ... | "z" ;
digit          = "0" | ... | "9" ;
```

Every statement ends with a semicolon. This includes compound statements, whose final `end` must therefore be written as `end;`. When an `else` clause is present, its colon introduces the alternative block; no semicolon follows the colon.

Blocks may be empty. In particular, an empty source file is a valid program, and constructs such as `if 1: else: end;` are syntactically valid.

### Expression grammar
Expressions contain integer literals, variable references, parentheses, and binary operators.

```ebnf
expression     = comparison-expression ;

comparison-expression
               = additive-expression,
                 { ("==" | "<=" | "<"), additive-expression } ;

additive-expression
               = multiplicative-expression,
                 { ("+" | "-"), multiplicative-expression } ;

multiplicative-expression
               = primary-expression, { "*", primary-expression } ;

primary-expression
               = identifier
               | integer-literal
               | "(", expression, ")" ;

integer-literal
               = [ "+" | "-" ], digit, { digit } ;
```

There are no general unary operators. A leading sign is part of an integer literal, so `-42` is valid but `-variable` is not. Arithmetic negation of a variable can be written as `0 - variable`.

### Operator precedence and associativity
Operators are grouped into the following precedence levels, from highest to lowest:

| Precedence | Operators | Associativity | Meaning |
|------------|-----------|---------------|---------|
| 1 | `*` | Left | Multiplication |
| 2 | `+`, `-` | Left | Addition and subtraction |
| 3 | `==`, `<=`, `<` | Left | Equality, less than or equal, and less than |

Parentheses override precedence. For example:

```text
1 + 2 * 3       means 1 + (2 * 3)
10 - 3 - 2      means (10 - 3) - 2
1 < 2 == 1      means (1 < 2) == 1
(1 + 2) * 3     uses the parenthesized sum as the left operand
```

All comparison operators have the same precedence. There are no Boolean literals or logical operators; comparisons produce the integer values `1` and `0`.

### Simple example of source code
This program calculates the factorial of 5 and prints the result:
```ruby
n = 5;
r = 1;

while 1 < n:
    r = r * n;
    n = n - 1;
end;

outn(r);
```

### Program state and evaluation model
A running program has two pieces of observable state:
- **Global variable storage**, associating identifiers with signed 64-bit integer values.
- **A text output buffer**, which is returned or printed after program completion.

All values have the same signed 64-bit integer type. There are no separate Boolean, character, or string values. Variables do not need declarations and have global scope: a variable assigned inside a conditional or loop body is also visible after that construct.

Every variable access must refer to a variable that has already been defined. A program that accesses an undefined variable is considered incorrect and must never be executed or compiled. Assigning to an existing variable replaces its previous value.

Statements in a block execute sequentially from first to last. Expression operands are evaluated from left to right. Expressions only calculate values and read variables; they do not themselves produce output or change variable storage.

### Values and expressions
- **Integer literal**

  An integer literal evaluates to its signed 64-bit integer value. For example, `0`, `27`, and `-3` evaluate to those respective values.

- **Variable reference**

  A variable reference evaluates to the value currently associated with its identifier. The referenced variable must already be defined; otherwise, the source program is invalid and must not be executed or compiled.

- **Parenthesized expression**

  Parentheses do not change the value of an expression. They only override the normal operator precedence.

- **Binary expression**

  Both operands are evaluated, with the left operand evaluated first. The operator then combines the two signed 64-bit integer results.

The complete operation table is:

| Syntax | Result |
|--------|--------|
| `lhs + rhs` | Integer sum of `lhs` and `rhs`. |
| `lhs - rhs` | Integer result of `lhs - rhs`. |
| `lhs * rhs` | Integer product of `lhs` and `rhs`. |
| `lhs == rhs` | `1` if the operands are equal, otherwise `0`. |
| `lhs <= rhs` | `1` if `lhs` is less than or equal to `rhs`, otherwise `0`. |
| `lhs < rhs` | `1` if `lhs` is less than `rhs`, otherwise `0`. |

Comparisons use integers rather than Boolean values. Consequently, their results can be used in arithmetic, assignments, output, and other comparisons:

```ruby
is_small = value < 10;
penalty = (value < 0) * 5;
```

### Truth and conditions
Conditional and loop constructs interpret integer values as conditions:
- `0` is false.
- Every nonzero value, including a negative value, is true.

A condition is an ordinary expression and does not have to contain a comparison. For example, `while n:` loops while `n` is nonzero.

### Assignment
```ruby
variable = expression;
```

The expression is evaluated first, and its result is then stored in the named global variable. If the variable already has a value, that value is replaced.

Because evaluation finishes before storage is updated, assignments such as the following are well-defined:

```ruby
n = n + 1;
a = b - a;
```

Assignment is a statement, not an expression, and therefore cannot appear inside another expression.

### Conditional statement
```ruby
if condition:
    then-statements
else:
    else-statements
end;
```

The condition is evaluated exactly once. If it is nonzero, the `then` block executes and the `else` block is skipped. If it is zero, the `then` block is skipped and the `else` block executes.

The `else` clause is optional:

```ruby
if condition:
    statements
end;
```

Without an `else` clause, a false condition causes execution to continue immediately after `end;`. An omitted `else` block is therefore equivalent to an empty one.

Only the selected block executes. Statements in the other block do not update variables or produce output.

### While statement
```ruby
while condition:
    body-statements
end;
```

The condition is evaluated before every possible iteration, including the first:

1. Evaluate the condition.
2. If it is zero, leave the loop and continue after `end;`.
3. If it is nonzero, execute the body once.
4. Return to step 1 and evaluate the condition again.

A condition that is initially zero prevents the body from executing at all. A condition that never becomes zero produces an infinite loop. Variable assignments in the body remain in effect for subsequent condition evaluations and for code after the loop.

### Character output
```ruby
out(expression);
```

The expression is evaluated and converted to a character using its value modulo 128. That character is appended to the output buffer without a newline.

For example, the following appends `A`, whose ASCII code is 65:

```ruby
out(65);
```

Despite the name `out`, the argument is an integer expression, not a string.

### Decimal number output
```ruby
outn(expression);
```

The expression is evaluated, converted to its decimal representation, and appended to the output buffer together with a newline.

For example:

```ruby
outn(40 + 2);
```

produces:

```text
42
```

Each `outn` call adds its own newline. In contrast, `out` appends only its character.

### Blocks and program completion
A block is a sequence of zero or more statements. An empty block performs no actions. Blocks do not introduce scopes, and there is no explicit return or halt statement.

A program finishes after its final top-level statement completes. Its accumulated text buffer is then available as the program output. Programs that do not terminate, such as loops whose conditions remain nonzero, do not reach this final output step.

### Simple example of program behavior
This program calculates the factorial of 5:

```ruby
n = 5;
r = 1;

while 1 < n:
    r = r * n;
    n = n - 1;
end;

if r == 120:
    outn(r);
else:
    outn(0);
end;
```

Execution performs the following high-level steps:

1. `n` receives 5 and `r` receives 1.
2. The loop executes for `n` values 5, 4, 3, and 2, multiplying them into `r` and decrementing `n`.
3. When `n` becomes 1, `1 < n` evaluates to 0 and the loop ends.
4. `r == 120` evaluates to 1, so the first branch executes.
5. The final output is `120` followed by a newline.

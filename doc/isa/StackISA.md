### Hardware capabilities
A virtual computer for the Stack ISA has access to the following peripherals and storage:
- A text buffer which is printed on a terminal screen _after program completion_.
- Global storage associating string keys with signed 64-bit integer values.
- A value stack containing signed 64-bit integers. Arithmetic, output, storage, and conditional jump instructions implicitly read values from this stack.

### Instruction operand types
Instructions can have the following explicit operands:
- **Imm**

  A hash (`#`) symbol followed by an integer literal.

  This rule can be written as a regular expression:
  ```regexp
  #-?[0-9]+
  ```

  Examples are `#0`, `#67`, and `#-42`, representing `0`, `67`, and `-42` respectively.

- **Mem**

  A string identifier or a number between square brackets (`[` and `]`) which points to a **memory location**.

  This rule can be written as a regular expression:
  ```regexp
  (\[[0-9]+\])|(\[[_a-zA-Z][_a-zA-Z0-9]*\])
  ```

  Examples are `[0]`, `[1]`, `[10]`, `[_]`, `[location]`, `[super_location]`, and `[_location]`.

- **Label**

  A pointer to a location in the code, used for control flow.

  This rule can be written as a regular expression:
  ```regexp
  (@[0-9]+)|(@[_a-zA-Z][_a-zA-Z0-9]*)
  ```

  Examples are `@0`, `@start`, and `@program_end`.

- **NoOp**

  No explicit operand is expected at this position. Instructions can still implicitly consume or produce stack values.

### Universal instruction format
All instructions follow this format:

| op | dst | lhs | rhs | example_usage_in_asm | description |
|----|-----|-----|-----|----------------------|-------------|
|    |     |     |     |                      |             |

Here **op** is a short textual mnemonic for the operation, **dst** identifies where an explicit result is stored, **lhs** is the left-hand side of a binary operation or the source of a single-input instruction, and **rhs** is the right-hand side of a binary operation. The **example_usage_in_asm** column shows assembly syntax, and **description** explains the operation.

A syntax rule for an instruction is:
```
instruction ::= mnemonic (operand,)? (operand,)? operand? $line_end
```

The last operand is followed by a newline, while preceding operands are followed by commas. Positions marked **NoOp** do not appear in the comma-separated operand list.

The complete table of operations is:

| op   | dst  | lhs  | rhs  | example_usage_in_asm | description |
|------|------|------|------|----------------------|-------------|
| PUSH | NoOp | Imm  | NoOp | `PUSH #27`           | Pushes the immediate value onto the stack. |
| LD   | NoOp | Mem  | NoOp | `LD [0]`             | Reads the value at `lhs` and pushes it onto the stack. |
| ST   | Mem  | NoOp | NoOp | `ST [0]`             | Pops the top stack value and stores it at `dst`. |
| ADD  | NoOp | NoOp | NoOp | `ADD`                | Pops `rhs` and then `lhs`, and pushes `lhs + rhs`. |
| SUB  | NoOp | NoOp | NoOp | `SUB`                | Pops `rhs` and then `lhs`, and pushes `lhs - rhs`. |
| MUL  | NoOp | NoOp | NoOp | `MUL`                | Pops `rhs` and then `lhs`, and pushes `lhs * rhs`. |
| EQ   | NoOp | NoOp | NoOp | `EQ`                 | Pops `rhs` and then `lhs`; pushes 1 if they are equal, otherwise 0. |
| LE   | NoOp | NoOp | NoOp | `LE`                 | Pops `rhs` and then `lhs`; pushes 1 if `lhs <= rhs`, otherwise 0. |
| LT   | NoOp | NoOp | NoOp | `LT`                 | Pops `rhs` and then `lhs`; pushes 1 if `lhs < rhs`, otherwise 0. |
| JZ   | Label | NoOp | NoOp | `JZ @110`           | Pops the top stack value and jumps to `dst` if it is 0. |
| JMP  | Label | NoOp | NoOp | `JMP @110`          | Unconditionally jumps to `dst`. |
| OUT  | NoOp | NoOp | NoOp | `OUT`                | Pops a value and appends the character corresponding to its value modulo 128 to the text buffer. |
| OUTN | NoOp | NoOp | NoOp | `OUTN`               | Pops a value and appends its decimal representation followed by `\n` to the text buffer. |

All arithmetic and comparison operations use signed 64-bit integer values. Since operands are taken from the stack, their order matters for noncommutative operations: `SUB`, `LE`, and `LT` treat the deeper value as `lhs` and the top value as `rhs`.

An instruction that pops more values than are currently on the stack causes stack underflow. Reading a memory location that has never been written returns the default value 0.

### Labels in the code
A label definition is a label followed by `:` and a newline. It marks a position in the code to which `JZ` or `JMP` can transfer control. A label definition does not perform an operation itself.

### Simple example of assembly code
This program calculates the factorial of 5:
```asm
    PUSH #5
    ST [n]
    PUSH #1
    ST [r]
@1:
    PUSH #1
    LD [n]
    LT
    JZ @2
    LD [r]
    LD [n]
    MUL
    ST [r]
    LD [n]
    PUSH #1
    SUB
    ST [n]
    JMP @1
@2:
    LD [r]
    OUTN
```

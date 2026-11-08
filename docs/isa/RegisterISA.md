### Hardware capabilities
A virtual computer for the Register ISA has access to the following peripherals and storage:
- A text buffer which is printed on a terminal screen _after program completion_.
- Global storage associating string keys with signed 64-bit integer values.
- 256 registers named `r0` through `r255`, each containing a signed 64-bit integer. All registers are initialized to 0.

### Instruction operand types
Instructions can have the following operands:
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

- **Reg**

  The letter `r` followed by a register number from 0 through 255.

  Its basic syntax can be written as:
  ```regexp
  r[0-9]+
  ```

  The numeric value must be in the range `0`–`255`. Examples are `r0`, `r1`, `r42`, and `r255`.

- **Label**

  A pointer to a location in the code, used for control flow.

  This rule can be written as a regular expression:
  ```regexp
  (@[0-9]+)|(@[_a-zA-Z][_a-zA-Z0-9]*)
  ```

  Examples are `@0`, `@start`, and `@program_end`.

- **NoOp**

  No operand is expected at this position.

### Universal instruction format
All instructions follow this format:

| op | dst | lhs | rhs | example_usage_in_asm | description |
|----|-----|-----|-----|----------------------|-------------|
|    |     |     |     |                      |             |

Here **op** is a short textual mnemonic for the operation, **dst** identifies where the result is stored, **lhs** is the left-hand side of a binary operation or the source of a single-input instruction, and **rhs** is the right-hand side of a binary operation. The **example_usage_in_asm** column shows assembly syntax, and **description** explains the operation.

A syntax rule for an instruction is:
```
instruction ::= mnemonic (operand,)? (operand,)? operand? $line_end
```

The last operand is followed by a newline, while preceding operands are followed by commas. Positions marked **NoOp** do not appear in the comma-separated operand list.

The complete table of operations is:

| op   | dst  | lhs  | rhs  | example_usage_in_asm      | description |
|------|------|------|------|---------------------------|-------------|
| LI   | Reg  | Imm  | NoOp | `LI r0, #27`              | Stores the immediate value in `dst`. |
| LD   | Reg  | Mem  | NoOp | `LD r0, [0]`              | Reads the value at `lhs` and stores it in `dst`. |
| ST   | Mem  | Reg  | NoOp | `ST [0], r0`              | Stores the value from `lhs` at the `dst` memory location. |
| ADD  | Reg  | Reg  | Reg  | `ADD r0, r0, r1`          | Stores `lhs + rhs` in `dst`. |
| SUB  | Reg  | Reg  | Reg  | `SUB r0, r0, r1`          | Stores `lhs - rhs` in `dst`. |
| MUL  | Reg  | Reg  | Reg  | `MUL r0, r0, r1`          | Stores `lhs * rhs` in `dst`. |
| EQ   | Reg  | Reg  | Reg  | `EQ r0, r0, r1`           | Stores 1 in `dst` if `lhs = rhs`, otherwise 0. |
| LE   | Reg  | Reg  | Reg  | `LE r0, r0, r1`           | Stores 1 in `dst` if `lhs <= rhs`, otherwise 0. |
| LT   | Reg  | Reg  | Reg  | `LT r0, r0, r1`           | Stores 1 in `dst` if `lhs < rhs`, otherwise 0. |
| JZ   | Label | Reg | NoOp | `JZ @110, r7`             | Jumps to `dst` if `lhs` contains 0. |
| JMP  | Label | NoOp | NoOp | `JMP @110`               | Unconditionally jumps to `dst`. |
| OUT  | NoOp | Reg  | NoOp | `OUT r13`                 | Appends the character corresponding to the value in `lhs` modulo 128 to the text buffer. |
| OUTN | NoOp | Reg  | NoOp | `OUTN r13`                | Appends the decimal value in `lhs` followed by `\n` to the text buffer. |

All arithmetic and comparison operations use signed 64-bit integer values. A destination register may also be one of the source registers; both source values are read before the result is written.

Registers and memory are independent storage. Reading a memory location that has never been written returns the default value 0, and every register is also initialized to 0.

### Labels in the code
A label definition is a label followed by `:` and a newline. It marks a position in the code to which `JZ` or `JMP` can transfer control. A label definition does not perform an operation itself.

### Simple example of assembly code
This program calculates the factorial of 5:
```asm
    LI r0, #5
    ST [n], r0
    LI r0, #1
    ST [r], r0
@1:
    LI r0, #1
    LD r1, [n]
    LT r0, r0, r1
    JZ @2, r0
    LD r0, [r]
    LD r1, [n]
    MUL r0, r0, r1
    ST [r], r0
    LD r0, [n]
    LI r1, #1
    SUB r0, r0, r1
    ST [n], r0
    JMP @1
@2:
    LD r0, [r]
    OUTN r0
```

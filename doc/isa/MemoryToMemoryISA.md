### Hardware capabilities
Virtual computer for a Memory-to-Memory ISA has access for next set of a perifery:
- Text buffer which will be printed on a terminal screen _after the program completion_
- A global storage of string keys assotiated with values of a signed 64 bit integer type

### Instruction operand types
Instructions can have next set of different operands:
- **Imm** 
  
  Looks like a hash (`#`) symbol followed by an integer literal.
  
  This rule could be written as a regular expression 
  ```regexp
  #-?[0-9]+
  ```

  Examples are `#0`, `#67`, `#-42` for `0`, `67` and `-42` respectively

- **Mem** 

  Looks like a string identifier or a number between square brackets 
  (`[` and `]`) and points to a **memory location**.

  This rule could be written as a regular expression
  ```regexp
  (\[[0-9]+\])|(\[[_a-zA-Z][_a-zA-Z0-9]*\])
  ```
  
  Examples are `[0]`, `[1]`, `[10]`, `[_]`, `[location]`, `[super_location]`, 
  `[_location]`, etc...

- **Label**
  
  Surves as a pointer to a Location in a code (used for control flow).

  Syntax rule for this operand could be written as a regular expression
  ```regexp
  (@[0-9]+)|(@[_a-zA-Z][_a-zA-Z0-9]*)
  ```
  
  Examples are `@0`, `@start`, `@program_end`, etc...

- **NoOp**
  
  This just means that an instruction has no operand expected at a particular place

### Universal instruction format
All insructions follow the format

| op | dst | lhs | rhs | example_usage_in_asm | description |
|----|----|---|----|---------|-------------|
|    |    |   |    |  |  |

where **op** is a short textual mnemonic for the operation (like `ADD` or `JZ` etc), 
**dst** is an operand which points where the result of an instruction must be stored,
**lhs** is a **left-hand side** of a binary operation or a **source** for an instruction 
which expect only one input argument, **rhs** is a **right-hand side** of a binary operation, 
**example_usage_in_asm** is an example of an intruction in an assembly code and **description** is a 
commentary on what this operation does.

A syntax rule for instruction could be written as follows:
```
instruction ::= mnemonic (operand,)? (operand,)? operand? $line_end
```
meaning that the last operand should be followed by an `\n` character, while others should be 
followed by `,`. In the case if an instruction has *NoOperand* in a particular position, it means
that this operand does not appears in a chain of comma separated operands.

Complete table of operations are follows:

| op   | dst   | lhs  | rhs  | example_usage_in_asm | description                                                       |
|------|-------|------|------|----------------------|-------------------------------------------------------------------|
| LI   | Mem   | Imm  | NoOp | `LI [0], #27`        | Stores immediate value from `lhs` in `dst` memory location        |
| MOV  | Mem   | Mem  | NoOp | `MOV [0], [1]`       | Stores value from `lhs` in a `dst` memory location                |
| ADD  | Mem   | Mem  | Mem  | `ADD [0], [0], [1]`  | Stores `lhs` + `rhs` in a `dst` memory location                   |
| SUB  | Mem   | Mem  | Mem  | `SUB [0], [0], [1]`  | Stores `lhs` - `rhs` in a `dst` memory location                   |
| MUL  | Mem   | Mem  | Mem  | `MUL [0], [0], [1]`  | Stores `lhs` * `rhs` in a `dst` memory location                   |
| EQ   | Mem   | Mem  | Mem  | `EQ [0], [0], [1]`   | If `lhs` = `rhs` then put 1 in a `dst`, otherwise put 0 there     |
| LE   | Mem   | Mem  | Mem  | `LE [0], [0], [1]`   | If `lhs` <= `rhs` then put 1 in a `dst`, otherwise put 0 there    |
| LT   | Mem   | Mem  | Mem  | `LT [0], [0], [1]`   | If `lhs` < `rhs` then put 1 in a `dst`, otherwise put 0 there     |
| JZ   | Label | Mem  | NoOp | `JZ @110, [7]`       | If `lhs` = 0, jump at a `dst` label in the code                   |
| JMP  | NoOp  | NoOp | NoOp | `JMP @110`           | Unconditionally jump at a `dst` label in the code                 |
| OUT  | NoOp  | Mem  | NoOp | `OUT [13]`           | Put ASCII character for a `lhs` in a text buffer                  |
| OUTN | NoOp  | Mem  | NoOp | `OUTN [13]`          | Print a decimal int for `lhs` followed by '\n' into a text buffer | 

All operations with an exception to **LI** operates on memory locations, which is a main meaning of this architecture. 

When there is attempt to read from a memory location which were never written to earlier, it means a default value of
0 must be used.

### Labels in the code
There are also special label instructions in the code which looks like a label followed by `:` and a new line,
these instructions are surved exactly one purpose, which is to mark position in the code where it would be possible to 
jump using `JZ` or `JMP`.

### Simple example of an assembly code
Here is a simple program calculating a factorial of 5
```asm
    LI [n], #5
    LI [r], #1
@1:
    LI [5], #1
    LT [4], [5], [n]
    JZ @2, [4]
    MUL [6], [r], [n]
    MOV [r], [6]
    LI [8], #1
    SUB [7], [n], [8]
    MOV [n], [7]
    JMP @1
@2:
    OUTN [r]
```
# Coverage Sample Programs Design

## Intent

Add small, deterministic language samples with expected outputs to exercise finite constant-folding and memory-to-memory compiler paths that the current samples miss. Run every sample through every existing test suite so the same semantics are checked at language, compiler, assembly-parser, and ISA-interpreter levels.

## Goals

- Add an `expressions` sample covering every constant-folding rule, nonconstant expressions, direct and nested memory-compiler expressions, and nonvariable output expressions.
- Add a `control_flow` sample covering constant and nonconstant branches, empty branch blocks, zero-trip loops, and a finite computed-condition loop.
- Include both samples in all existing language, compiler, ISA-parser, and ISA-interpreter sample lists.
- Provide memory, stack, and register assembly fixtures so the ISA suites can consume the same samples.

## Non-goals

- Do not add infinite-loop samples or expected-output tests for nonterminating programs.
- Do not cover compiler guard failures or malformed parser input in this change.
- Do not add separate unit-test suites for paths unsuited to samples.
- Do not add dependencies or new test infrastructure.

## Constraints

- Use the existing `.mrb` syntax and deterministic numeric/ASCII output.
- Use the existing resource-loading and sample-enumeration mechanisms.
- Expected outputs must be identical for language interpretation, all compiler backends in ISA and text modes, and all checked-in ISA assembly fixtures.
- Keep the samples finite and independent of undefined-variable behavior.

## Approaches

- **Shared samples only, across every suite — selected.** This maximizes integration coverage and keeps one semantic fixture per scenario. It requires six additional assembly files and deliberately leaves nonterminating and failure paths uncovered.
- Shared language/compiler samples without ISA assembly fixtures — rejected because the user wants every existing suite included.
- Hybrid samples plus focused unit tests — rejected for this change; the user selected shared samples only.

## Sample Design

### `expressions`

Use `x = 7` and `y = 5`. Include:

- Constant `+`, `-`, `*`, `==`, `<`, and `<=` expressions.
- Both forms of addition by zero, multiplication by one, and multiplication by zero, plus subtraction of zero.
- Nonconstant variable/variable, constant/variable, variable/constant, and nested binary expressions.
- Direct assignments of constants, variables, and each binary-expression shape.
- Nonconstant `<`, `<=`, and `==` outputs so comparison operators survive folding and reach assembly generation.
- A nonvariable `out` expression, `out(x + 61)`, at the end so the final character is `D` without a trailing newline.

### `control_flow`

Include:

- Constant-zero and constant-nonzero branches, each with and without `else`.
- An empty `then` block and an empty `else` block.
- Variable and computed branch conditions.
- A constant-zero loop and a zero-trip loop with a variable condition.
- A finite loop whose condition is a computed expression.

The expected numeric output is one value per line: `1, 2, 3, 4, 5, 6, 7, 0, 1`.

## Resource and Test Integration

Add these embedded resources:

- `Programs/expressions.mrb` and `Programs/control_flow.mrb`.
- `ExpectedOuts/expressions.txt` and `ExpectedOuts/control_flow.txt`.
- `Asm/expressions.asmm`, `Asm/expressions.asms`, `Asm/expressions.asmr`.
- `Asm/control_flow.asmm`, `Asm/control_flow.asms`, `Asm/control_flow.asmr`.

Generate the assembly fixtures with the existing text compilers after constant folding, then check them in as deterministic inputs for ISA parser and interpreter tests. Extend `SampleAsm` and its filename mapping with both sample names, and add both cases to every existing sample list.

## Verification

Run the complete solution test suite with coverage. All tests must pass, all ten resources must load, and the new samples must eliminate the covered finite constant-folding and memory-compiler gaps without attempting the documented non-goals.

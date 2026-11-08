# Undefined Variable Semantic Analyzer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reject every source-language program that can read a variable before that variable is guaranteed to have been assigned.

**Architecture:** Add a standalone, single-pass definite-assignment analyzer over the existing AST. `analyze_for_unassigned_variables` validates a program, raises `FailureException("Undefined variable: <name>")` on the first invalid read, and returns the original `Program` so callers can compose it explicitly in pass chains. It does not invoke, wrap, or otherwise affect interpretation, optimization, or compilation passes. Constant conditions are used to analyze only the selected branch, skip zero-trip loop bodies, and retain definitions from infinite-loop bodies.

**Tech Stack:** F#, .NET 8, NUnit, existing `toy_comp_core.AST` types; no new dependencies.

**Spec:** `doc/language/Grammar.md` and `doc/language/Semantics.md`

## Global Constraints

- Variables have global scope; there are no declarations, functions, types, or local scopes.
- Pipelines that execute or compile source programs must explicitly include `analyze_for_unassigned_variables` after parsing and before later passes when undefined-variable rejection is required.
- The analyzer is a standalone pass: interpreters, optimizers, and compilers do not call it implicitly.
- Within each block, a variable's first assignment must not come after its first read. If an outer block guarantees that the variable was written before the current block executes, this rule is lifted for reads in the current block.
- Assignment evaluates its right-hand side before updating the destination.
- Blocks execute sequentially, expression operands are read left to right, and conditions are checked before their bodies.
- Nonconstant conditionals intersect branch definitions; nonconstant loops do not export body definitions.
- Constant conditionals analyze only the selected branch; zero-trip loops skip their bodies; nonzero constant loops are infinite and export body definitions for analysis of the following unreachable statements.
- The AST has no source spans, so diagnostics identify only the first offending `MemoryLocation`.
- `analyze_for_unassigned_variables : Program -> Program` returns its input on success and fails with exactly `Undefined variable: <name>` on invalid input.

## Review Focus

- A self-referencing first assignment must report the destination as undefined; pinned by Task 1's assignment test.
- A read inside a child block may use an earlier assignment in that same block or a definition guaranteed by an outer block; pinned by Task 2's hierarchical-block test.
- A variable defined on only one path of a nonconstant conditional must remain undefined afterward; pinned by Task 2's nonconstant-branch test.
- A constant conditional uses only the selected branch and ignores the other; pinned by Task 2's constant-branch test.
- Nonconstant loops retain entry definitions only, zero-trip loops skip their bodies, and infinite loops export body definitions; pinned by Task 2's three loop tests.
- A read at the start of a loop body cannot use an assignment from a later statement or a hypothetical previous iteration; pinned by Task 2's first-iteration test.

## File Structure

- Create `toy_comp_compiler/UndefinedVariableAnalyzer.fs`: standalone analysis pass returning the input program on success.
- Create `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`: expression, statement, branch, loop, and constant-control-flow tests.
- Modify `toy_comp_compiler/toy_comp_compiler.fsproj`: compile the analyzer before the test pipelines consume it.
- Modify `toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj`: include the analyzer tests.
- Modify `toy_comp_compiler_tests/InterpreterTests.fs`, `ConstantFoldingTests.fs`, `ISAStackCompilerTests.fs`, `ISARegCompilerTests.fs`, and `ISAMemCompilerTests.fs`: place the analyzer explicitly in sample pass chains.
- Modify `README.md`: mark the undefined-variable semantic analyzer as implemented after verification.

---

### Task 1: Analyze Expressions and Sequential Assignments

**Files:**
- Create: `toy_comp_compiler/UndefinedVariableAnalyzer.fs`
- Modify: `toy_comp_compiler/toy_comp_compiler.fsproj:12-21`
- Create: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`
- Modify: `toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj:13-21`

**Interfaces:**
- Consumes: `Program`, `Block`, `Statement`, `Expr`, and `MemoryLocation` from `toy_comp_core`.
- Produces: `analyze_for_unassigned_variables : Program -> Program`, returning the input on success and raising `FailureException("Undefined variable: <name>")` for the first undefined read in evaluation order.

- [ ] **Step 1: Write failing expression and sequential-block tests**

Create `toy_comp_compiler_tests.UndefinedVariableAnalyzerTests` with a helper that asserts the exception message and these exact cases:

```fsharp
let private assert_undefined (MemoryLocation name) program =
    let ex = Assert.Catch(fun () -> analyze_for_unassigned_variables program |> ignore)
    Assert.AreEqual($"Undefined variable: {name}", ex.Message)

[<Test>]
let ``assignment defines its destination only after checking rhs`` () =
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 1L)); PrintNumber(Variable x) ])

    let invalid =
        Program(Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ])

    analyze_for_unassigned_variables valid
    assert_undefined x invalid

[<Test>]
let ``binary expressions report the leftmost undefined read`` () =
    let left = MemoryLocation "left"
    let right = MemoryLocation "right"
    let program = Program(Block [ PrintNumber(Binary(Add, Variable left, Variable right)) ])
    assert_undefined left program

[<Test>]
let ``output statements check their expressions`` () =
    let putChar = Program(Block [ PutChar(Variable x) ])
    let printNumber = Program(Block [ PrintNumber(Variable x) ])
    assert_undefined x putChar
    assert_undefined x printNumber
```

Include `UndefinedVariableAnalyzerTests.fs` after `ParserTests.fs` in the test project.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: build failure because `toy_comp_compiler.UndefinedVariableAnalyzer` does not exist.

- [ ] **Step 3: Implement the minimal expression and sequential analyzer**

Create `module toy_comp_compiler.UndefinedVariableAnalyzer` and include it after `Parser.fs` but before `Interpreter.fs` in `toy_comp_compiler.fsproj`.

Implement this exact signature:

```fsharp
let analyze_for_unassigned_variables (program: Program) : Program
```

Use private recursive helpers equivalent to:

```fsharp
check_expr : Set<MemoryLocation> -> Expr -> unit
check_block : Set<MemoryLocation> -> Block -> Set<MemoryLocation>
```

`check_expr` accepts constants, checks variables with `Set.contains`, raises `failwith $"Undefined variable: {name}"` for an undefined read, and checks binary operands left then right. `check_block` threads its set sequentially. For `Assignment(dst, expr)`, check `expr` before adding `dst`. For `PutChar` and `PrintNumber`, check the expression and preserve the set. Leave `Branch` and `WhileLoop` as explicit `failwith "Task 2"` cases for this task only. After the root block passes, return the original `Program` unchanged.

- [ ] **Step 4: Run the focused tests to verify they pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the three tests pass.

- [ ] **Step 5: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler/toy_comp_compiler.fsproj toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj
git commit -m "feat: analyze undefined variables in sequential programs"
```

### Task 2: Analyze Conditionals and Loops with Constant Control Flow

**Files:**
- Modify: `toy_comp_compiler/UndefinedVariableAnalyzer.fs`
- Test: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`

**Interfaces:**
- Consumes: `analyze_for_unassigned_variables` and the private recursive helpers from Task 1.
- Produces: complete analysis for every existing `Statement` and `Expr` case; no new public API.

- [ ] **Step 1: Write failing branch and loop tests**

Using Task 1's helpers, add tests for nonconstant branch intersection, selected constant branches, nonconstant and constant loops, sequential child blocks, and inherited definitions. Use these exact distinguishing cases:

```fsharp
[<Test>]
let ``nonconstant conditional definitions must be present on both paths`` () =
    let flag = MemoryLocation "flag"
    let assignFlag = Assignment(flag, Constant(ImmValue 1L))
    let condition = Variable flag
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let readX = PrintNumber(Variable x)
    let bothBranches =
        Program(Block [ assignFlag; Branch(condition, Block [ assignX ], Block [ assignX ]); readX ])
    let thenOnly = Program(Block [ assignFlag; Branch(condition, Block [ assignX ], Block []); readX ])
    analyze_for_unassigned_variables bothBranches |> ignore
    assert_undefined x thenOnly

[<Test>]
let ``constant conditional uses only the selected branch`` () =
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let readX = PrintNumber(Variable x)
    let thenOnly = Program(Block [ Branch(Constant(ImmValue 1L), Block [ assignX ], Block []); readX ])
    let skippedRead = Program(Block [ Branch(Constant(ImmValue 0L), Block [ readX ], Block []) ])
    analyze_for_unassigned_variables thenOnly |> ignore
    analyze_for_unassigned_variables skippedRead |> ignore

[<Test>]
let ``nonconstant loop body definitions do not escape the loop`` () =
    let flag = MemoryLocation "flag"
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let program =
        Program(Block [ Assignment(flag, Constant(ImmValue 1L)); WhileLoop(Variable flag, Block [ assignX ]); PrintNumber(Variable x) ])
    assert_undefined x program

[<Test>]
let ``infinite loop exports definitions from its body`` () =
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let program = Program(Block [ WhileLoop(Constant(ImmValue 1L), Block [ assignX ]); PrintNumber(Variable x) ])
    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``zero trip loop skips its body`` () =
    let program = Program(Block [ WhileLoop(Constant(ImmValue 0L), Block [ PrintNumber(Variable x) ]) ])
    analyze_for_unassigned_variables program |> ignore
```

Also retain tests proving that nonconstant conditions are checked before bodies, child blocks allow an earlier local assignment, a body read cannot use a later assignment, and nested blocks inherit definitions guaranteed on entry.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the nine new tests fail at the Task 1 placeholders.

- [ ] **Step 3: Implement hierarchical and constant-aware control-flow analysis**

Replace both placeholders in `check_block`'s statement handling:

- `Branch(Constant(ImmValue 0L), _, elseBlock)`: analyze only `elseBlock` and return its definitions.
- `Branch(Constant(ImmValue _), thenBlock, _)`: analyze only `thenBlock` and return its definitions.
- `Branch(cond, thenBlock, elseBlock)`: check `cond`; analyze both child blocks from the input set; return `Set.intersect thenDefined elseDefined`.
- `WhileLoop(Constant(ImmValue 0L), _)`: return the input set without analyzing the body.
- `WhileLoop(Constant(ImmValue _), body)`: analyze `body` and return its definitions; the loop is infinite, so subsequent statements are unreachable.
- `WhileLoop(cond, body)`: check `cond`; analyze `body` once from the input set; discard its output set and return the input set.

Because `check_block` threads definitions sequentially, an assignment earlier in a child block satisfies later reads in that block. Do not add fixed-point iteration, scope objects, or diagnostics collections.

- [ ] **Step 4: Run all analyzer tests to verify they pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: all twelve analyzer tests pass.

- [ ] **Step 5: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs
git commit -m "feat: analyze undefined variables across control flow"
```

### Task 3: Compose Analysis Explicitly in Pass Chains

**Files:**
- Modify: `toy_comp_compiler_tests/InterpreterTests.fs`
- Modify: `toy_comp_compiler_tests/ConstantFoldingTests.fs`
- Modify: `toy_comp_compiler_tests/ISAStackCompilerTests.fs`
- Modify: `toy_comp_compiler_tests/ISARegCompilerTests.fs`
- Modify: `toy_comp_compiler_tests/ISAMemCompilerTests.fs`
- Modify: `README.md:92-95`

**Interfaces:**
- Consumes: `analyze_for_unassigned_variables : Program -> Program` from Task 1.
- Produces: explicit sample pipelines containing analysis; no production pass gains a hidden analyzer dependency.

- [ ] **Step 1: Insert the analyzer into every sample source-language pipeline**

In interpreter and constant-folding tests, use `parse_program |> analyze_for_unassigned_variables` before interpretation or folding. In all three compiler test files, use `parse_program |> analyze_for_unassigned_variables |> fold_consts_in_program` before both ISA and text compilation. Do not add analyzer calls to `Interpreter.fs`, `ConstantFolding.fs`, or compiler modules.

- [ ] **Step 2: Run the complete solution test suite**

Run: `dotnet test toy_comp_fsharp.slnx`

Expected: all analyzer, interpreter, compiler, parser, and sample-program tests pass. The samples exercise explicit pass-chain composition without coupling the analyzer to later passes.

- [ ] **Step 3: Update project status and commit**

Change the README's `Semantic code analyzer to detect an access to undefined variables` checkbox from unchecked to checked, then commit:

```bash
git add toy_comp_compiler_tests/InterpreterTests.fs toy_comp_compiler_tests/ConstantFoldingTests.fs toy_comp_compiler_tests/ISAStackCompilerTests.fs toy_comp_compiler_tests/ISARegCompilerTests.fs toy_comp_compiler_tests/ISAMemCompilerTests.fs README.md
git commit -m "test: compose undefined variable analysis in pass chains"
```

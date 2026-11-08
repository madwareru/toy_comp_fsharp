# Undefined Variable Semantic Analyzer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reject every source-language program that can read a variable before that variable is guaranteed to have been assigned.

**Architecture:** Add a single-pass definite-assignment analyzer over the existing AST. It threads a `Set<MemoryLocation>` through statements and checks blocks hierarchically: a read is valid when its variable was assigned earlier in the same block or was guaranteed to be written by an outer block before the current block executes. It intersects definitions after conditionals and excludes loop-body-only definitions after loops. Undefined reads immediately raise `FailureException("Undefined variable: <name>")`; existing interpreter and compiler APIs remain unchanged.

**Tech Stack:** F#, .NET 8, NUnit, existing `toy_comp_core.AST` types; no new dependencies.

**Spec:** `doc/language/Grammar.md` and `doc/language/Semantics.md`

## Global Constraints

- Variables have global scope; there are no declarations, functions, types, or local scopes.
- Every variable read must refer to a variable already defined by assignment; invalid programs must never be executed or compiled.
- Within each block, a variable's first assignment must not come after its first read. If an outer block guarantees that the variable was written before the current block executes, this rule is lifted for reads in the current block.
- Assignment evaluates its right-hand side before updating the destination.
- Blocks execute sequentially, expression operands are read left to right, and conditions are checked before their bodies.
- A conditional executes only one branch; a loop body may execute zero times.
- Analyze all syntactically present branches and bodies without constant-condition folding or reachability inference.
- The AST has no source spans, so diagnostics identify only the first offending `MemoryLocation`.
- Preserve existing interpreter and compiler return types; invalid programs fail with exactly `Undefined variable: <name>`.

## Review Focus

- A self-referencing first assignment must report the destination as undefined; pinned by Task 1's assignment test.
- A read inside a child block may use an earlier assignment in that same block or a definition guaranteed by an outer block; pinned by Task 2's hierarchical-block test.
- A variable defined in only one conditional branch must remain undefined afterward; pinned by Task 2's one-branch test.
- A variable defined in both branches must be defined afterward; pinned by Task 2's both-branches test.
- A variable defined only in a loop body must remain undefined after the loop; pinned by Task 2's zero-iteration test.
- A read at the start of a loop body cannot use an assignment from a later statement or a hypothetical previous iteration; pinned by Task 2's first-iteration test.

## File Structure

- Create `toy_comp_compiler/UndefinedVariableAnalyzer.fs`: analysis and throwing validation used by consumers.
- Create `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`: expression, statement, branch, loop, and integration tests.
- Modify `toy_comp_compiler/toy_comp_compiler.fsproj`: compile the analyzer before all consumers.
- Modify `toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj`: include the analyzer tests.
- Modify `toy_comp_compiler/Interpreter.fs`: reject invalid programs before creating runtime state.
- Modify `toy_comp_compiler/ISAStackCompiler.fs`, `ISARegCompiler.fs`, and `ISAMemCompiler.fs`: reject invalid programs before compilation in both modes.
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
- Produces: `analyze_program : Program -> unit`, raising `FailureException("Undefined variable: <name>")` for the first undefined read in evaluation order.

- [ ] **Step 1: Write failing expression and sequential-block tests**

Create `toy_comp_compiler_tests.UndefinedVariableAnalyzerTests` with a helper that asserts the exception message and these exact cases:

```fsharp
let private assert_undefined (MemoryLocation name) program =
    let ex = Assert.Catch(fun () -> analyze_program program |> ignore)
    Assert.AreEqual($"Undefined variable: {name}", ex.Message)

[<Test>]
let ``assignment defines its destination only after checking rhs`` () =
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 1L)); PrintNumber(Variable x) ])

    let invalid =
        Program(Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ])

    analyze_program valid
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
let analyze_program (program: Program) : unit
```

Use private recursive helpers equivalent to:

```fsharp
check_expr : Set<MemoryLocation> -> Expr -> unit
check_block : Set<MemoryLocation> -> Block -> Set<MemoryLocation>
```

`check_expr` accepts constants, checks variables with `Set.contains`, raises `failwith $"Undefined variable: {name}"` for an undefined read, and checks binary operands left then right. `check_block` threads its set sequentially. For `Assignment(dst, expr)`, check `expr` before adding `dst`. For `PutChar` and `PrintNumber`, check the expression and preserve the set. Leave `Branch` and `WhileLoop` as explicit `failwith "Task 2"` cases for this task only.

- [ ] **Step 4: Run the focused tests to verify they pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the three tests pass.

- [ ] **Step 5: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler/toy_comp_compiler.fsproj toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj
git commit -m "feat: analyze undefined variables in sequential programs"
```

### Task 2: Analyze Conditionals and Loops Conservatively

**Files:**
- Modify: `toy_comp_compiler/UndefinedVariableAnalyzer.fs`
- Test: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`

**Interfaces:**
- Consumes: `analyze_program` and the private recursive helpers from Task 1.
- Produces: complete analysis for every existing `Statement` and `Expr` case; no new public API.

- [ ] **Step 1: Write failing branch and loop tests**

Using Task 1's helpers, add tests with these exact programs and assertions:

```fsharp
[<Test>]
let ``conditional definitions must be present on both paths`` () =
    let condition = Constant(ImmValue 1L)
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let readX = PrintNumber(Variable x)

    let bothBranches =
        Program(Block [ Branch(condition, Block [ assignX ], Block [ assignX ]); readX ])

    let thenOnly = Program(Block [ Branch(condition, Block [ assignX ], Block []); readX ])

    analyze_program bothBranches
    assert_undefined x thenOnly

[<Test>]
let ``conditional and loop conditions are checked before their bodies`` () =
    let branch = Program(Block [ Branch(Variable x, Block [], Block []) ])
    let loop = Program(Block [ WhileLoop(Variable x, Block []) ])
    assert_undefined x branch
    assert_undefined x loop

[<Test>]
let ``child blocks allow an earlier assignment in the same block`` () =
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let readX = PrintNumber(Variable x)
    let branch = Program(Block [ Branch(Constant(ImmValue 1L), Block [ assignX; readX ], Block []) ])
    let loop = Program(Block [ WhileLoop(Constant(ImmValue 1L), Block [ assignX; readX ]) ])
    analyze_program branch
    analyze_program loop

[<Test>]
let ``loop body definitions do not escape the loop`` () =
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let program =
        Program(Block [ WhileLoop(Constant(ImmValue 1L), Block [ assignX ]); PrintNumber(Variable x) ])

    assert_undefined x program

[<Test>]
let ``loop body reads cannot use a later body assignment`` () =
    let body = Block [ PrintNumber(Variable x); Assignment(x, Constant(ImmValue 1L)) ]
    let program = Program(Block [ WhileLoop(Constant(ImmValue 1L), body) ])
    assert_undefined x program

[<Test>]
let ``nested blocks use definitions guaranteed on entry`` () =
    let body = Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ]
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 0L)); WhileLoop(Variable x, body); PrintNumber(Variable x) ])

    analyze_program valid
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the six new tests fail at the Task 1 placeholders.

- [ ] **Step 3: Implement hierarchical block checks and definition merging**

Replace both placeholders in `check_block`'s statement handling:

- `Branch(cond, thenBlock, elseBlock)`: check `cond` against the current block's definitions; analyze both child blocks independently from that same set; return `Set.intersect thenDefined elseDefined`. Propagate the first error in condition, then-branch, else-branch order.
- `WhileLoop(cond, body)`: check `cond` against the current block's definitions; analyze the child body once from that same set; discard the body's output set and return the original input set.

Because `check_block` threads definitions sequentially, an assignment earlier in a child block satisfies later reads in that block. The input set represents writes guaranteed by outer blocks before the child block executes. Do not add fixed-point iteration, constant evaluation, scope objects, or diagnostics collections.

- [ ] **Step 4: Run all analyzer tests to verify they pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: all nine analyzer tests pass.

- [ ] **Step 5: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs
git commit -m "feat: analyze undefined variables across control flow"
```

### Task 3: Enforce Analysis in Every Consumer

**Files:**
- Modify: `toy_comp_compiler/Interpreter.fs:7-10`
- Modify: `toy_comp_compiler/ISAStackCompiler.fs:16-18`
- Modify: `toy_comp_compiler/ISARegCompiler.fs:16-18`
- Modify: `toy_comp_compiler/ISAMemCompiler.fs:16-18`
- Test: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`
- Modify: `README.md:92-95`

**Interfaces:**
- Consumes: `analyze_program : Program -> unit` from Task 1.
- Produces: no new public API; every consumer raises the analyzer's existing `FailureException` for invalid programs.

- [ ] **Step 1: Write failing interpreter and compiler enforcement tests**

Add one invalid program that prints undefined `x`. Assert that `interp_program` and every compilation mode of every backend throw with the exact message:

```fsharp
[<Test>]
let ``interpreter rejects undefined variables`` () =
    let ex = Assert.Catch(fun () -> interp_program invalidProgram |> ignore)
    Assert.AreEqual("Undefined variable: x", ex.Message)

[<Test>]
let ``all compiler modes reject undefined variables`` () =
    let compilations : (unit -> unit) list =
        [ fun () -> compile_program_isa_mem CompilationMode.ISA invalidProgram |> ignore
          fun () -> compile_program_isa_mem CompilationMode.Text invalidProgram |> ignore
          fun () -> compile_program_isa_reg CompilationMode.ISA invalidProgram |> ignore
          fun () -> compile_program_isa_reg CompilationMode.Text invalidProgram |> ignore
          fun () -> compile_program_isa_stack CompilationMode.ISA invalidProgram |> ignore
          fun () -> compile_program_isa_stack CompilationMode.Text invalidProgram |> ignore ]

    for compile in compilations do
        let ex = Assert.Catch(fun () -> compile ())
        Assert.AreEqual("Undefined variable: x", ex.Message)
```

Add the required module opens without changing existing test files.

- [ ] **Step 2: Run the tests to verify enforcement is missing**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the interpreter test fails with its existing missing-map-key behavior, and the compiler test completes compilation instead of reporting `Undefined variable: x`.

- [ ] **Step 3: Call the analyzer at every public entry point**

Call `analyze_program program` first in `interp_program` and at the start of `compile_program_isa_stack`, `compile_program_isa_reg`, and `compile_program_isa_mem`, before matching or mutating any compilation state. Preserve all existing signatures and valid-program behavior.

- [ ] **Step 4: Run the complete solution test suite**

Run: `dotnet test toy_comp_fsharp.slnx`

Expected: all analyzer, interpreter, compiler, parser, and existing sample-program tests pass. The sample suites prove that valid nested assignments remain accepted and that output is unchanged.

- [ ] **Step 5: Update project status and commit**

Change the README's `Semantic code analyzer to detect an access to undefined variables` checkbox from unchecked to checked, then commit:

```bash
git add toy_comp_compiler/Interpreter.fs toy_comp_compiler/ISAStackCompiler.fs toy_comp_compiler/ISARegCompiler.fs toy_comp_compiler/ISAMemCompiler.fs toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs README.md
git commit -m "feat: reject programs with undefined variables"
```

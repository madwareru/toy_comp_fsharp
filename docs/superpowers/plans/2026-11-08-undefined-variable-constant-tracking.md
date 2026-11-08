# Undefined Variable Constant Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enhance undefined-variable analysis with conservative constant tracking so conditions stored in variables or computed by expressions can select branches and determine loop entry behavior.

**Architecture:** Replace the analyzer's definition set with a map from each defined variable to `Known int64` or `Unknown`. Reuse the existing sequential traversal and public pass signature; add pointwise branch merging, entry-only loop decisions, and invalidation of values assigned in loop bodies.

**Tech Stack:** F#, .NET 8, NUnit, existing `toy_comp_core.AST` and `BinOp` types; no new dependencies.

**Spec:** `docs/superpowers/specs/2026-11-08-undefined-variable-constant-tracking-design.md`

## Global Constraints

- Preserve `analyze_for_unassigned_variables : Program -> Program`; return the original AST unchanged on success and raise `FailureException("Undefined variable: <name>")` on the first invalid read.
- Keep the analyzer standalone; do not invoke it implicitly from interpreters, optimizers, or compilers.
- Map membership means defined. `Unknown` is defined but has no statically known value; a missing key is undefined.
- Evaluate expression operands left to right, and evaluate an assignment's right-hand side before updating its destination.
- Use signed 64-bit arithmetic matching `Interpreter.fs`; `Eq`, `Lt`, and `Leq` produce known `0L` or `1L` when both operands are known.
- Only `Known 0L` is false; every other known integer, including negative values, is true.
- Unknown branches analyze both paths from the same entry state. Definitions must occur on both paths, and constants survive only when both paths agree.
- Analyze loop conditions only at entry. No fixed point, widening engine, trip-count calculation, optimization, source spans, or aggregated diagnostics.
- After a loop whose body is analyzed, every defined variable assigned anywhere in that body becomes `Unknown`; untouched variables retain their entry values.

## Review Focus

- An unknown left operand must not hide an undefined right operand; pinned by Task 1's existing left-to-right diagnostic test and evaluator requirements.
- Arithmetic overflow and comparisons must match the interpreter's signed 64-bit operations rather than introducing a separate value semantics; pinned by Task 1's exact arithmetic/comparison test.
- A negative known condition is true, while zero alone is false; pinned by Task 1's known-variable condition test.
- Different constants on unknown branch paths must become `Unknown`, while identical constants remain known; pinned by Task 1's branch-merge test.
- Assignments nested anywhere in a loop body must invalidate that variable without invalidating untouched variables; pinned by Task 2's loop-invalidation tests.

## File Structure

- Modify `toy_comp_compiler/UndefinedVariableAnalyzer.fs`: tracked state, expression evaluation, branch merging, and loop invalidation.
- Modify `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`: constant-propagation tests and updated unknown-condition fixtures.
- Modify `doc/analyzers/UndefinedVariableAnalyzer.md`: concise description of the enhanced algorithm.

---

### Task 1: Track Values Through Expressions and Conditionals

**Files:**
- Modify: `toy_comp_compiler/UndefinedVariableAnalyzer.fs`
- Test: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`

**Interfaces:**
- Consumes: existing `Program`, `Block`, `Statement`, `Expr`, `BinOp`, `ImmValue`, and `MemoryLocation` types.
- Produces: private `TrackedValue = Known of int64 | Unknown`, private expression evaluation over `Map<MemoryLocation, TrackedValue>`, and private recursive collection of assignments in a block. The public analyzer signature is unchanged.

- [ ] **Step 1: Add failing constant-expression and branch-merge tests**

Keep all existing tests, but make fixtures that need an unknown condition genuinely unknown. Add this helper and use it instead of `Assignment(flag, Constant(ImmValue 1L))` in the nonconstant-branch and nonconstant-loop tests, and instead of the initial constant assignment in the nested-block inherited-definition test:

```fsharp
let private define_unknown location =
    WhileLoop(Constant(ImmValue 1L), Block [ Assignment(location, Constant(ImmValue 0L)) ])
```

The analyzer intentionally examines statements after this infinite loop; the body executes once for analysis and the assignment invalidation implemented below makes `location` defined but `Unknown`.

Add these tests:

```fsharp
[<Test>]
let ``known arithmetic and comparisons produce exact values`` () =
    let cases =
        [ Add, 7L, 5L, 12L
          Sub, 7L, 5L, 2L
          Mul, 7L, 5L, 35L
          Eq, 7L, 5L, 0L
          Lt, 7L, 5L, 0L
          Leq, 7L, 7L, 1L ]

    for op, lhs, rhs, expected in cases do
        let result = Binary(op, Constant(ImmValue lhs), Constant(ImmValue rhs))
        let condition = Binary(Eq, result, Constant(ImmValue expected))
        let program = Program(Block [ Branch(condition, Block [], Block [ PrintNumber(Variable x) ]) ])
        analyze_for_unassigned_variables program |> ignore
```

Add `System.Int64.MaxValue, 1L, System.Int64.MinValue` as an `Add` case to pin the interpreter's wrapping signed 64-bit behavior.

```fsharp
[<Test>]
let ``known variable conditions treat only zero as false`` () =
    let skippedRead = Block [ PrintNumber(Variable x) ]
    let nonzero =
        Program(Block [ Assignment(x, Constant(ImmValue -1L)); Branch(Variable x, Block [], skippedRead) ])
    let zero =
        Program(Block [ Assignment(x, Constant(ImmValue 0L)); Branch(Variable x, skippedRead, Block []) ])

    analyze_for_unassigned_variables nonzero |> ignore
    analyze_for_unassigned_variables zero |> ignore

[<Test>]
let ``assignment stores a value computed from the old destination`` () =
    let condition = Binary(Eq, Variable x, Constant(ImmValue 2L))
    let program =
        Program(Block
            [ Assignment(x, Constant(ImmValue 1L))
              Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L)))
              Branch(condition, Block [], Block [ PrintNumber(Variable x) ]) ])

    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``unknown branches keep only matching known values`` () =
    let flag = MemoryLocation "flag"
    let assignX value = Assignment(x, Constant(ImmValue value))
    let condition = Binary(Eq, Variable x, Constant(ImmValue 1L))
    let selected = Program(Block
        [ define_unknown flag
          Branch(Variable flag, Block [ assignX 1L ], Block [ assignX 1L ])
          Branch(condition, Block [], Block [ PrintNumber(Variable x) ]) ])
    let conflicting = Program(Block
        [ define_unknown flag
          Branch(Variable flag, Block [ assignX 1L ], Block [ assignX 2L ])
          Branch(condition, Block [], Block [ PrintNumber(Variable x) ]) ])

    analyze_for_unassigned_variables selected |> ignore
    assert_undefined x conflicting

[<Test>]
let ``unknown left operand does not hide an undefined right operand`` () =
    let flag = MemoryLocation "flag"
    let program = Program(Block
        [ define_unknown flag
          PrintNumber(Binary(Add, Variable flag, Variable x)) ])

    assert_undefined x program
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: the new constant-propagation and conflicting-branch-value tests fail. Updated unknown-condition fixtures and the unknown-left-operand diagnostic test may already pass and serve as regression coverage.

- [ ] **Step 3: Implement tracked state, expression evaluation, and branch merging**

In `toy_comp_compiler/UndefinedVariableAnalyzer.fs`, add:

```fsharp
type TrackedValue =
    | Known of int64
    | Unknown
```

Change the private helpers to these shapes:

```fsharp
eval_expr : Map<MemoryLocation, TrackedValue> -> Expr -> TrackedValue
check_block : Map<MemoryLocation, TrackedValue> -> Block -> Map<MemoryLocation, TrackedValue>
assigned_locations : Block -> Set<MemoryLocation>
```

`eval_expr` returns the constant or tracked variable value, raising the existing diagnostic for an absent key. Always evaluate both binary operands left then right; only combine two `Known` operands. Use the same `+`, `-`, `*`, `=`, `<`, and `<=` operations as `Interpreter.fs`, converting comparison results to `Known 1L` or `Known 0L`.

`check_block` threads the map sequentially. Assignment evaluates before `Map.add dst value`; output statements evaluate without changing state. For `Branch`, evaluate the condition and analyze only the selected block for `Known 0L` or `Known _`. For `Unknown`, analyze both from the entry map and fold the then-result into a merged map: retain a location only when the else-result also contains it, use `Known value` only when both values are the same constant, and otherwise use `Unknown`.

`assigned_locations` recursively collects destinations from assignments inside nested branches and loops. Add a private invalidation helper that changes only already-defined keys present in that set to `Unknown`.

For this task, retain the existing three literal-loop cases: zero preserves state; nonzero analyzes the body, exports its definitions, then invalidates body-assigned values; nonconstant analyzes the body once, discards new definitions, and invalidates body-assigned values in the entry state. This supports the unknown fixtures without changing literal-loop behavior.

- [ ] **Step 4: Run the focused tests to verify they pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: all existing and new analyzer tests pass.

- [ ] **Step 5: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs
git commit -m "feat: track constants through branches"
```

### Task 2: Analyze Loop Entry Conditions Conservatively

**Files:**
- Modify: `toy_comp_compiler/UndefinedVariableAnalyzer.fs`
- Test: `toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs`
- Modify: `doc/analyzers/UndefinedVariableAnalyzer.md`

**Interfaces:**
- Consumes: Task 1's `eval_expr`, `check_block`, `assigned_locations`, and invalidation helper.
- Produces: complete entry-only constant-aware loop analysis; no new public API.

- [ ] **Step 1: Add failing loop-entry and invalidation tests**

Use Task 1's `define_unknown` helper to create unknown loop conditions. Add tests with these exact programs and assertions:

```fsharp
[<Test>]
let ``computed zero loop condition skips body and preserves entry values`` () =
    let y = MemoryLocation "y"
    let loopCondition = Binary(Eq, Variable x, Constant(ImmValue 1L))
    let afterCondition = Binary(Eq, Variable x, Constant(ImmValue 0L))
    let program = Program(Block
        [ Assignment(x, Constant(ImmValue 0L))
          WhileLoop(loopCondition, Block [ PrintNumber(Variable y) ])
          Branch(afterCondition, Block [], Block [ PrintNumber(Variable y) ]) ])

    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``computed nonzero loop exports definitions but not assigned constants`` () =
    let missing = MemoryLocation "missing"
    let loopCondition = Binary(Lt, Constant(ImmValue 0L), Constant(ImmValue 1L))
    let defined = Program(Block
        [ WhileLoop(loopCondition, Block [ Assignment(x, Constant(ImmValue 1L)) ])
          PrintNumber(Variable x) ])
    let unknown = Program(Block
        [ WhileLoop(loopCondition, Block [ Assignment(x, Constant(ImmValue 1L)) ])
          Branch(Binary(Eq, Variable x, Constant(ImmValue 1L)), Block [ PrintNumber(Variable missing) ], Block []) ])

    analyze_for_unassigned_variables defined |> ignore
    assert_undefined missing unknown
```

In `unknown`, the invalid read is reached only if the analyzer incorrectly retains `Known 1L`; if `x` is correctly `Unknown`, it analyzes that branch and reports `missing` as undefined.

```fsharp
[<Test>]
let ``loops invalidate nested assignments and preserve untouched values`` () =
    let flag = MemoryLocation "flag"
    let y = MemoryLocation "y"
    let missing = MemoryLocation "missing"
    let nestedAssignment = Branch(Constant(ImmValue 1L), Block [ Assignment(x, Constant(ImmValue 3L)) ], Block [])
    let invalidated = Program(Block
        [ define_unknown flag
          Assignment(x, Constant(ImmValue 1L))
          WhileLoop(Variable flag, Block [ nestedAssignment ])
          Branch(Binary(Eq, Variable x, Constant(ImmValue 3L)), Block [ PrintNumber(Variable missing) ], Block []) ])
    let untouched = Program(Block
        [ define_unknown flag
          Assignment(y, Constant(ImmValue 2L))
          WhileLoop(Variable flag, Block [ Assignment(x, Constant(ImmValue 3L)) ])
          Branch(Binary(Eq, Variable y, Constant(ImmValue 2L)), Block [], Block [ PrintNumber(Variable missing) ]) ])

    assert_undefined missing invalidated
    analyze_for_unassigned_variables untouched |> ignore

[<Test>]
let ``loop analysis does not assume exactly one iteration`` () =
    let i = MemoryLocation "i"
    let condition = Binary(Lt, Variable i, Constant(ImmValue 2L))
    let body = Block [ Assignment(i, Binary(Add, Variable i, Constant(ImmValue 1L))) ]
    let program = Program(Block
        [ Assignment(i, Constant(ImmValue 0L))
          WhileLoop(condition, body)
          Branch(Binary(Eq, Variable i, Constant(ImmValue 1L)), Block [ PrintNumber(Variable x) ], Block []) ])

    assert_undefined x program
```

Retain the existing nonconstant-loop test proving body-only definitions do not escape an unknown loop.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: computed loop conditions still take the old nonconstant path, so the zero-trip and known-nonzero tests fail. The nested-invalidation and single-iteration tests may already pass after Task 1 and serve as regression coverage.

- [ ] **Step 3: Replace literal loop matching with entry-condition evaluation**

For `WhileLoop(cond, body)`, call `eval_expr` once on the entry state and use Task 1's helpers:

- `Known 0L`: return the entry map without analyzing the body or invalidating values.
- `Known _`: analyze the body once, retain its resulting definitions, and invalidate every defined location collected from the body.
- `Unknown`: analyze the body once for diagnostics, discard its resulting map, and invalidate body-assigned locations in the entry map.

Do not reevaluate the condition, iterate the body, or merge per-iteration states.

- [ ] **Step 4: Update the algorithm explanation**

In `doc/analyzers/UndefinedVariableAnalyzer.md`, replace the definition-set description with the `Map<MemoryLocation, TrackedValue>` model. Briefly document known expression evaluation, pointwise branch merging, entry-only loop decisions, body-assignment invalidation, and retention of untouched values. Keep the standalone-pass and diagnostic descriptions unchanged.

- [ ] **Step 5: Run focused and complete verification**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj --filter FullyQualifiedName~UndefinedVariableAnalyzerTests`

Expected: all analyzer tests pass.

Run: `dotnet test toy_comp_fsharp.slnx`

Expected: the complete solution test suite passes with no new dependencies or generated source files.

- [ ] **Step 6: Commit**

```bash
git add toy_comp_compiler/UndefinedVariableAnalyzer.fs toy_comp_compiler_tests/UndefinedVariableAnalyzerTests.fs doc/analyzers/UndefinedVariableAnalyzer.md
git commit -m "feat: analyze constant loop entry conditions"
```

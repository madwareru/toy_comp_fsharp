# Undefined Variable Analyzer Constant Tracking Design

## Intent

Enhance the standalone undefined-variable analyzer with conservative constant tracking so it can recognize constant branch outcomes and whether a loop's first iteration is guaranteed. The analyzer remains a separate `Program -> Program` pass and does not implicitly invoke or modify other passes.

## Goals

- Track whether each defined variable contains a statically known signed 64-bit integer.
- Evaluate arithmetic and comparison expressions when their operands are known.
- Analyze only the selected branch of a known-condition conditional.
- Skip a loop known to execute zero times.
- Recognize a known-nonzero constant loop as infinite under the current language.
- Preserve existing undefined-variable diagnostics and explicit pass-chain composition.

## Non-goals

- No iterative loop-body analysis, fixed-point computation, widening engine, or trip-count calculation.
- No exact constant values after loops whose bodies modify those values.
- No optimization or AST rewriting; this pass only validates and returns its input.
- No source locations or aggregated diagnostics.

## State and Expression Evaluation

Replace `Set<MemoryLocation>` with `Map<MemoryLocation, TrackedValue>`:

```fsharp
type TrackedValue =
    | Known of int64
    | Unknown
```

Map membership means the variable is defined. `Unknown` is defined but has no statically known value. A missing key remains an undefined-variable error.

Expression evaluation returns `Known value` only when the result can be computed from known operands. Constants are known; defined variables return their tracked value; any unknown operand makes a binary result unknown. Comparisons produce known `0` or `1` when both operands are known. Evaluation still checks operands left to right.

Assignment evaluates the right-hand side before updating the destination. Output statements evaluate their expressions without changing the state.

## Control Flow

### Conditionals

- `Known 0`: analyze only the else block.
- `Known nonzero`: analyze only the then block.
- `Unknown`: analyze both blocks from the same entry state and merge them pointwise.

For an unknown conditional, a variable is defined afterward only if both branches define it. Its value remains `Known` only when both results contain the same constant; otherwise it is `Unknown`.

### Loops

Evaluate only the condition's entry value:

- `Known 0`: skip the body and preserve the entry state.
- `Known nonzero`: analyze the body once. Definitions created in the body may be exported, matching the current infinite-loop behavior.
- `Unknown`: analyze the body once from the entry state, but do not export definitions created only in the body.

Any variable assigned in a loop body becomes `Unknown` afterward. Variables untouched by the body retain their entry values. This avoids assuming that a loop executes exactly once: for example, `i = 0; while i < 2: i = i + 1;` must not produce `Known 1` for `i` after the loop.

## Approaches Considered

- **Entry-only analysis — selected.** Evaluate conditions from the entry state and conservatively invalidate loop-assigned constants. It provides useful control-flow inference with a contained change.
- **Iterative analysis with widening — rejected for this increment.** Reanalyzing loop bodies would infer more, but requires state merging, convergence rules, and widening to avoid nontermination when repeatedly producing new constants.
- **Literal-only conditions — rejected.** The existing implementation already handles literal conditions and cannot use constants stored in variables.

## Builder Discretion

The implementation may choose private helper names and the exact map-merge and loop-invalidation functions, provided the public signature, diagnostic order, and conservative rules above remain unchanged.

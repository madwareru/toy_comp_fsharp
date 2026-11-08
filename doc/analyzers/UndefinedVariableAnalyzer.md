# Undefined Variable Analyzer

`analyze_for_unassigned_variables` is a standalone AST pass. It checks that every variable read has a guaranteed preceding assignment, raises `FailureException("Undefined variable: <name>")` for the first violation, and returns the original program on success. Callers explicitly compose it with other passes.

## Algorithm

The analyzer traverses the AST with a `Map<MemoryLocation, TrackedValue>`. Map membership means a variable is defined; `Known int64` records a statically known value, while `Unknown` means defined without a known value.

- **Expressions:** constants are known, variable references return their tracked value or report an undefined read, and binary operands are evaluated left to right. Arithmetic and comparisons produce a known result only when both operands are known.
- **Assignments:** evaluate the right-hand side first, then update the destination's tracked value.
- **Output statements:** evaluate the expression without changing the state.
- **Blocks:** process statements sequentially. Child blocks inherit the state guaranteed at their entry, but may also use assignments made earlier in their own block.
- **Branches:** a known-zero condition selects only the else block, and a known-nonzero condition selects only the then block. For an unknown condition, analyze both blocks from the same entry state. A variable remains defined only if both paths define it, and remains known only if both paths produce the same constant.
- **Loops:** evaluate only the entry condition. Known zero skips the body; known nonzero analyzes the body once and retains its definitions; unknown analyzes the body once for diagnostics but discards definitions created only in the body.
- **Loop invalidation:** after an analyzed body, every defined variable assigned anywhere in that body becomes `Unknown`. Untouched variables retain their values, avoiding the assumption that a loop executes exactly once.

The traversal stops at the first undefined read. No source locations or aggregated diagnostics are produced.

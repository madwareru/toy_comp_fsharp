# Documentation

This directory contains the language reference, instruction-set specifications, analyzer notes, and design history for the project. For a project overview and feature checklist, see the [root README](../README.md).

## Start Here

1. Read the [language grammar](language/Grammar.md) for syntax and the [language semantics](language/Semantics.md) for evaluation rules.
2. Read the [ISA overview](isa/README.md) to understand the available instruction-set architectures.
3. Open the specification for the architecture you want to use:
   - Stack: [ISA](isa/StackISA.md) and [bytecode](isa/StackBytecode.md)
   - Register: [ISA](isa/RegisterISA.md) and [bytecode](isa/RegisterBytecode.md)
   - Memory-to-memory: [ISA](isa/MemoryToMemoryISA.md) and [bytecode](isa/MemoryToMemoryBytecode.md)
4. Read [Undefined Variable Analyzer](analyzers/UndefinedVariableAnalyzer.md) for the semantic-analysis algorithm and its control-flow diagrams.

## AI-assisted Design and Implementation Notes

Design history and implementation tasks on various project improvements made with `superpowers`:
- [Original undefined variable analyzer implementation plan](superpowers/plans/UndefinedVariableAnalyzerImplementationPlan.md)
- [Constant-tracking design for undefined variable analyzer](superpowers/specs/2026-11-08-undefined-variable-constant-tracking-design.md)
- [Constant-tracking undefined variable analyzer implementation plan](superpowers/plans/2026-11-08-undefined-variable-constant-tracking.md)
- [Additional samples for code coverage improvement design](superpowers/specs/2026-11-09-coverage-sample-programs-design.md)
- [Additional samples for code coverage improvement plan](superpowers/plans/2026-11-09-coverage-sample-programs.md)

## Updating Documentation

- Use relative links so navigation works both on GitHub and in local editors.
- Update the relevant reference document whenever behavior changes.
- Keep design history unchanged unless correcting the record or completing its implementation status.
- Use Mermaid for new diagrams where a visual explanation would help.

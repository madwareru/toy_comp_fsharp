# Coverage Sample Programs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add expression and control-flow samples with expected outputs and assembly variants to improve coverage in every existing test suite.

**Architecture:** Add two deterministic language programs as embedded resources and include them in the existing sample-driven tests. Generate their memory, stack, and register assembly forms with the existing text compilers so ISA parser and interpreter suites exercise the same programs and expected outputs.

**Tech Stack:** F#, .NET 8, NUnit, FParsec, Coverlet

**Spec:** `docs/superpowers/specs/2026-11-09-coverage-sample-programs-design.md`

## Global Constraints

- Use existing `.mrb` syntax, resource loading, and sample lists; add no dependencies or test infrastructure.
- All programs must be finite, deterministic, and free of undefined-variable reads.
- Expected outputs must match in language, compiler, ISA-parser, and ISA-interpreter suites.
- Do not cover infinite loops, compiler guard failures, or malformed parser input in this change.
- Generate assembly fixtures with the existing text compilers after constant folding.

## Review Focus

- Folding identities can hide the generic constant-operation path: Task 1 includes separate constant arithmetic and nonconstant identity cases with exact outputs.
- Empty blocks can change `if`/`else` parsing: Task 1 includes an empty `then` and an empty `else`, then Task 2 checks their generated assembly.
- `PutChar` does not append a newline: Task 1 pins `D` as the final byte of `expressions.txt` with no trailing newline.
- Generated labels and block layout can differ between backends: Task 2 runs every generated assembly through its parser and interpreter instead of comparing assembly text.
- A missing embedded resource can fail only one test project: Tasks 1 and 2 run each affected project, and Task 2 finishes with the full solution.

---

### Task 1: Language Samples and Compiler Coverage

**Files:**
- Create: `toy_comp_core/Resources/Programs/expressions.mrb`
- Create: `toy_comp_core/Resources/Programs/control_flow.mrb`
- Create: `toy_comp_core/Resources/ExpectedOuts/expressions.txt`
- Create: `toy_comp_core/Resources/ExpectedOuts/control_flow.txt`
- Modify: `toy_comp_core/ResourceUtility.fs`
- Modify: `toy_comp_core/toy_comp_core.fsproj`
- Modify: `toy_comp_compiler_tests/ParserTests.fs`
- Modify: `toy_comp_compiler_tests/ConstantFoldingTests.fs`
- Modify: `toy_comp_compiler_tests/InterpreterTests.fs`
- Modify: `toy_comp_compiler_tests/ISAStackCompilerTests.fs`
- Modify: `toy_comp_compiler_tests/ISARegCompilerTests.fs`
- Modify: `toy_comp_compiler_tests/ISAMemCompilerTests.fs`

**Interfaces:**
- Consumes: existing `read_sample_program`, `read_expected_out`, and sample-driven NUnit tests.
- Produces: `SampleAsm.Expressions` and `SampleAsm.ControlFlow`, mapped respectively to `"expressions"` and `"control_flow"` by `get_file_name`.

- [ ] **Step 1: Add the failing sample references**

Add `Expressions` and `ControlFlow` to `SampleAsm` and their exact filename mappings. Add both cases to every sample list in the six compiler-test files listed above, including both tests in each compiler backend file.

- [ ] **Step 2: Run the compiler tests to verify the missing resources fail**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj`

Expected: FAIL while loading `Programs.expressions.mrb` or `Programs.control_flow.mrb`; no assertion should yet fail.

- [ ] **Step 3: Add `expressions.mrb` exactly**

```ruby
x = 7;
y = 5;
copy = x;
sum = x + y;
left = 10 + x;
right = x + 10;
nested = (x + 1) * (y + 2);

outn(7 + 5);
outn(7 - 5);
outn(7 * 5);
outn(7 == 5);
outn(7 < 5);
outn(7 <= 7);

outn(x + 0);
outn(0 + x);
outn(x - 0);
outn(1 * x);
outn(x * 1);
outn(0 * x);
outn(x * 0);

outn(x + y);
outn(10 + x);
outn(x + 10);
outn((x + 1) * (y + 2));
outn(x == y);
outn(x < y);
outn(x <= y);

outn(copy);
outn(sum);
outn(left);
outn(right);
outn(nested);
out(x + 61);
```

- [ ] **Step 4: Add `expressions.txt` exactly**

The file ends in `D` with no trailing newline.

```text
12
2
35
0
0
1
7
7
7
7
7
0
0
12
17
17
56
0
0
0
7
12
17
17
56
D
```

- [ ] **Step 5: Add `control_flow.mrb` exactly**

```ruby
zero = 0;
one = 1;

if 0:
    outn(100);
else:
    outn(1);
end;

if 2:
    outn(2);
else:
    outn(100);
end;

if 0:
    outn(100);
end;

if 0:
    outn(100);
else:
end;

if 1:
end;

if 1:
else:
    outn(100);
end;

if zero:
    outn(100);
end;

if one:
    outn(3);
end;

if zero:
    outn(100);
else:
    outn(4);
end;

if one:
    outn(5);
else:
    outn(100);
end;

if zero + one:
    outn(6);
end;

if zero + one:
    outn(7);
else:
    outn(100);
end;

while 0:
    outn(100);
end;

while zero:
    outn(100);
end;

i = 0;
while i < 2:
    outn(i);
    i = i + 1;
end;
```

- [ ] **Step 6: Add `control_flow.txt` exactly**

The final line has a trailing newline.

```text
1
2
3
4
5
6
7
0
1
```

- [ ] **Step 7: Embed the four resources**

Add `Programs\expressions.mrb`, `Programs\control_flow.mrb`, `ExpectedOuts\expressions.txt`, and `ExpectedOuts\control_flow.txt` to the existing `EmbeddedResource` item group in `toy_comp_core/toy_comp_core.fsproj`.

- [ ] **Step 8: Run the compiler tests to verify the samples pass**

Run: `dotnet test toy_comp_compiler_tests/toy_comp_compiler_tests.fsproj`

Expected: **31/31 tests pass**, including both new samples in every existing sample loop.

- [ ] **Step 9: Commit**

```bash
git add toy_comp_core/ResourceUtility.fs toy_comp_core/toy_comp_core.fsproj toy_comp_core/Resources/Programs/expressions.mrb toy_comp_core/Resources/Programs/control_flow.mrb toy_comp_core/Resources/ExpectedOuts/expressions.txt toy_comp_core/Resources/ExpectedOuts/control_flow.txt toy_comp_compiler_tests
git commit -m "test: add expression and control-flow samples"
```

### Task 2: Assembly Fixtures and ISA Coverage

**Files:**
- Create: `toy_comp_core/Resources/Asm/expressions.asmm`
- Create: `toy_comp_core/Resources/Asm/expressions.asms`
- Create: `toy_comp_core/Resources/Asm/expressions.asmr`
- Create: `toy_comp_core/Resources/Asm/control_flow.asmm`
- Create: `toy_comp_core/Resources/Asm/control_flow.asms`
- Create: `toy_comp_core/Resources/Asm/control_flow.asmr`
- Modify: `toy_comp_core/toy_comp_core.fsproj`
- Modify: `toy_comp_isa_parser_tests/ISAMemParserTests.fs`
- Modify: `toy_comp_isa_parser_tests/ISAStackParserTests.fs`
- Modify: `toy_comp_isa_parser_tests/ISARegParserTests.fs`
- Modify: `toy_comp_isa_interpreter_tests/ISAMemInterpreterTests.fs`
- Modify: `toy_comp_isa_interpreter_tests/ISAStackInterpreterTests.fs`
- Modify: `toy_comp_isa_interpreter_tests/ISARegInterpreterTests.fs`

**Interfaces:**
- Consumes: Task 1's `SampleAsm.Expressions` and `SampleAsm.ControlFlow`, language resources, expected outputs, and `compile_program_isa_mem`, `compile_program_isa_stack`, and `compile_program_isa_reg` in `CompilationMode.Text` after `fold_consts_in_program`.
- Produces: `read_sample_asm` support for both names in all three existing `ISAKind` modes.

- [ ] **Step 1: Add the failing ISA sample references**

Add `SampleAsm.Expressions` and `SampleAsm.ControlFlow` to the sample list in each of the six ISA parser/interpreter test files listed above.

- [ ] **Step 2: Run the ISA tests to verify the missing assembly resources fail**

Run: `dotnet test toy_comp_isa_parser_tests/toy_comp_isa_parser_tests.fsproj`
Run: `dotnet test toy_comp_isa_interpreter_tests/toy_comp_isa_interpreter_tests.fsproj`

Expected: FAIL while loading the new `.asmm`, `.asms`, or `.asmr` resources.

- [ ] **Step 3: Generate the six assembly fixtures**

Build Task 1, then use a temporary F# Interactive script referencing the built core/compiler assemblies and FParsec. For each sample, run `read_sample_program |> parse_program |> analyze_for_unassigned_variables |> fold_consts_in_program`, invoke each compiler with `CompilationMode.Text`, extract `CompiledText`, and write it to the corresponding `Resources/Asm` path. Delete the temporary script after generation.

Expected: six nonempty assembly files whose labels and operations come from the existing compilers, with no hand-written optimization or reformatting.

- [ ] **Step 4: Embed the six assembly resources**

Add all six paths to the existing `EmbeddedResource` item group in `toy_comp_core/toy_comp_core.fsproj`, using the same backslash path convention as the current entries.

- [ ] **Step 5: Run the ISA tests to verify every fixture**

Run: `dotnet test toy_comp_isa_parser_tests/toy_comp_isa_parser_tests.fsproj`
Run: `dotnet test toy_comp_isa_interpreter_tests/toy_comp_isa_interpreter_tests.fsproj`

Expected: **6/6 parser tests** and **3/3 interpreter tests pass**, with both samples producing their Task 1 expected outputs.

- [ ] **Step 6: Run full solution verification with coverage**

Run: `dotnet test toy_comp_fsharp.slnx --collect:"XPlat Code Coverage"`

Expected: **40/40 tests pass**. Inspect the merged production reports: `ConstantFolding.fs` and the finite paths added to `ISAMemCompiler.fs` are covered; infinite-loop, guard-failure, and intentionally unreachable paths remain out of scope.

- [ ] **Step 7: Remove generated coverage output**

Remove only the `TestResults` directories created by Step 6 and confirm `git status --short` shows only intended source, resource, test, and documentation changes.

- [ ] **Step 8: Commit**

```bash
git add toy_comp_core/toy_comp_core.fsproj toy_comp_core/Resources/Asm/expressions.asmm toy_comp_core/Resources/Asm/expressions.asms toy_comp_core/Resources/Asm/expressions.asmr toy_comp_core/Resources/Asm/control_flow.asmm toy_comp_core/Resources/Asm/control_flow.asms toy_comp_core/Resources/Asm/control_flow.asmr toy_comp_isa_parser_tests toy_comp_isa_interpreter_tests
git commit -m "test: cover new samples across ISA suites"
```

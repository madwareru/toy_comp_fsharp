module toy_comp_compiler_tests.UndefinedVariableAnalyzerTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_compiler.UndefinedVariableAnalyzer
open toy_comp_compiler.Interpreter
open toy_comp_compiler.CompilerShared
open toy_comp_compiler.ISAMemCompiler
open toy_comp_compiler.ISARegCompiler
open toy_comp_compiler.ISAStackCompiler

let private x = MemoryLocation "x"
let private invalid_program = Program(Block [ PrintNumber(Variable x) ])

let private assert_undefined (MemoryLocation name) program =
    let ex = Assert.Catch(fun () -> analyze_for_unassigned_variables program |> ignore)
    Assert.AreEqual($"Undefined variable: {name}", ex.Message)

[<Test>]
let ``assignment defines its destination only after checking rhs`` () =
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 1L)); PrintNumber(Variable x) ])

    let invalid =
        Program(Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ])

    analyze_for_unassigned_variables valid |> ignore
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

[<Test>]
let ``conditional definitions must be present on both paths`` () =
    let condition = Constant(ImmValue 1L)
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let readX = PrintNumber(Variable x)

    let bothBranches =
        Program(Block [ Branch(condition, Block [ assignX ], Block [ assignX ]); readX ])

    let thenOnly = Program(Block [ Branch(condition, Block [ assignX ], Block []); readX ])

    analyze_for_unassigned_variables bothBranches |> ignore
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
    analyze_for_unassigned_variables branch |> ignore
    analyze_for_unassigned_variables loop |> ignore

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

    analyze_for_unassigned_variables valid |> ignore

[<Test>]
let ``interpreter rejects undefined variables`` () =
    let ex = Assert.Catch(fun () -> interp_program invalid_program |> ignore)
    Assert.AreEqual("Undefined variable: x", ex.Message)

[<Test>]
let ``all compiler modes reject undefined variables`` () =
    let compilations : (unit -> unit) list =
        [ fun () -> compile_program_isa_mem CompilationMode.ISA invalid_program |> ignore
          fun () -> compile_program_isa_mem CompilationMode.Text invalid_program |> ignore
          fun () -> compile_program_isa_reg CompilationMode.ISA invalid_program |> ignore
          fun () -> compile_program_isa_reg CompilationMode.Text invalid_program |> ignore
          fun () -> compile_program_isa_stack CompilationMode.ISA invalid_program |> ignore
          fun () -> compile_program_isa_stack CompilationMode.Text invalid_program |> ignore ]

    for compile in compilations do
        let ex = Assert.Catch(fun () -> compile ())
        Assert.AreEqual("Undefined variable: x", ex.Message)

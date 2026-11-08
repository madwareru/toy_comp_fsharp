module toy_comp_compiler_tests.UndefinedVariableAnalyzerTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_compiler.UndefinedVariableAnalyzer

let private x = MemoryLocation "x"

let private assert_undefined (MemoryLocation name) program =
    let ex = Assert.Catch(fun () -> analyze_program program |> ignore)
    Assert.AreEqual($"Undefined variable: {name}", ex.Message)

[<Test>]
let ``assignment defines its destination only after checking rhs`` () =
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 1L)); PrintNumber(Variable x) ])

    let invalid =
        Program(Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ])

    analyze_program valid |> ignore
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

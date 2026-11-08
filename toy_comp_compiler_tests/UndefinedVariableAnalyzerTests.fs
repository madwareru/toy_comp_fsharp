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

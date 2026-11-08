module toy_comp_compiler_tests.UndefinedVariableAnalyzerTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_compiler.UndefinedVariableAnalyzer

let private x = MemoryLocation "x"

let private define_unknown location =
    WhileLoop(Constant(ImmValue 1L), Block [ Assignment(location, Constant(ImmValue 0L)) ])

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
let ``nonconstant conditional definitions must be present on both paths`` () =
    let flag = MemoryLocation "flag"
    let assignFlag = define_unknown flag
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
let ``nonconstant loop body definitions do not escape the loop`` () =
    let flag = MemoryLocation "flag"
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let program =
        Program(
            Block
                [ define_unknown flag
                  WhileLoop(Variable flag, Block [ assignX ])
                  PrintNumber(Variable x) ]
        )

    assert_undefined x program

[<Test>]
let ``infinite loop exports definitions from its body`` () =
    let assignX = Assignment(x, Constant(ImmValue 1L))
    let program =
        Program(Block [ WhileLoop(Constant(ImmValue 1L), Block [ assignX ]); PrintNumber(Variable x) ])

    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``zero trip loop skips its body`` () =
    let program = Program(Block [ WhileLoop(Constant(ImmValue 0L), Block [ PrintNumber(Variable x) ]) ])
    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``loop body reads cannot use a later body assignment`` () =
    let body = Block [ PrintNumber(Variable x); Assignment(x, Constant(ImmValue 1L)) ]
    let program = Program(Block [ WhileLoop(Constant(ImmValue 1L), body) ])
    assert_undefined x program

[<Test>]
let ``nested blocks use definitions guaranteed on entry`` () =
    let body = Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ]
    let valid =
        Program(Block [ define_unknown x; WhileLoop(Variable x, body); PrintNumber(Variable x) ])

    analyze_for_unassigned_variables valid |> ignore

[<Test>]
let ``known arithmetic and comparisons produce exact values`` () =
    let cases =
        [ Add, 7L, 5L, 12L
          Sub, 7L, 5L, 2L
          Mul, 7L, 5L, 35L
          Eq, 7L, 5L, 0L
          Lt, 7L, 5L, 0L
          Leq, 7L, 7L, 1L
          Add, System.Int64.MaxValue, 1L, System.Int64.MinValue ]

    for op, lhs, rhs, expected in cases do
        let result = Binary(op, Constant(ImmValue lhs), Constant(ImmValue rhs))
        let condition = Binary(Eq, result, Constant(ImmValue expected))
        let program = Program(Block [ Branch(condition, Block [], Block [ PrintNumber(Variable x) ]) ])
        analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``known variable conditions treat only zero as false`` () =
    let missing = MemoryLocation "missing"
    let skippedRead = Block [ PrintNumber(Variable missing) ]
    let nonzero =
        Program(Block [ Assignment(x, Constant(ImmValue -1L)); Branch(Variable x, Block [], skippedRead) ])
    let zero =
        Program(Block [ Assignment(x, Constant(ImmValue 0L)); Branch(Variable x, skippedRead, Block []) ])

    analyze_for_unassigned_variables nonzero |> ignore
    analyze_for_unassigned_variables zero |> ignore

[<Test>]
let ``assignment stores a value computed from the old destination`` () =
    let missing = MemoryLocation "missing"
    let condition = Binary(Eq, Variable x, Constant(ImmValue 2L))
    let program =
        Program(
            Block
                [ Assignment(x, Constant(ImmValue 1L))
                  Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L)))
                  Branch(condition, Block [], Block [ PrintNumber(Variable missing) ]) ]
        )

    analyze_for_unassigned_variables program |> ignore

[<Test>]
let ``unknown branches keep only matching known values`` () =
    let flag = MemoryLocation "flag"
    let missing = MemoryLocation "missing"
    let assignX value = Assignment(x, Constant(ImmValue value))
    let condition = Binary(Eq, Variable x, Constant(ImmValue 1L))

    let selected =
        Program(
            Block
                [ define_unknown flag
                  Branch(Variable flag, Block [ assignX 1L ], Block [ assignX 1L ])
                  Branch(condition, Block [], Block [ PrintNumber(Variable missing) ]) ]
        )

    let conflicting =
        Program(
            Block
                [ define_unknown flag
                  Branch(Variable flag, Block [ assignX 1L ], Block [ assignX 2L ])
                  Branch(condition, Block [], Block [ PrintNumber(Variable missing) ]) ]
        )

    analyze_for_unassigned_variables selected |> ignore
    assert_undefined missing conflicting

[<Test>]
let ``unknown left operand does not hide an undefined right operand`` () =
    let flag = MemoryLocation "flag"

    let program =
        Program(
            Block
                [ define_unknown flag
                  PrintNumber(Binary(Add, Variable flag, Variable x)) ]
        )

    assert_undefined x program

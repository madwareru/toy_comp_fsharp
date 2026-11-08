module toy_comp_compiler_tests.UndefinedVariableAnalyzerTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_compiler.UndefinedVariableAnalyzer

let private x = MemoryLocation "x"

[<Test>]
let ``assignment defines its destination only after checking rhs`` () =
    let valid =
        Program(Block [ Assignment(x, Constant(ImmValue 1L)); PrintNumber(Variable x) ])

    let invalid =
        Program(Block [ Assignment(x, Binary(Add, Variable x, Constant(ImmValue 1L))) ])

    Assert.AreEqual((Ok () : Result<unit, MemoryLocation>), analyze_program valid)
    Assert.AreEqual((Error x : Result<unit, MemoryLocation>), analyze_program invalid)

[<Test>]
let ``binary expressions report the leftmost undefined read`` () =
    let left = MemoryLocation "left"
    let right = MemoryLocation "right"
    let program = Program(Block [ PrintNumber(Binary(Add, Variable left, Variable right)) ])
    Assert.AreEqual((Error left : Result<unit, MemoryLocation>), analyze_program program)

[<Test>]
let ``output statements check their expressions`` () =
    let putChar = Program(Block [ PutChar(Variable x) ])
    let printNumber = Program(Block [ PrintNumber(Variable x) ])
    Assert.AreEqual((Error x : Result<unit, MemoryLocation>), analyze_program putChar)
    Assert.AreEqual((Error x : Result<unit, MemoryLocation>), analyze_program printNumber)

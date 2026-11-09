module toy_comp_compiler_tests.ParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ResourceUtility
open toy_comp_core.AST
open toy_comp_compiler.Parser

let factorialAssembly = SampleAsm.Fact |> read_sample_program

let factorialProgram =
    Program(
        Block
            [ Assignment(MemoryLocation "n", Constant(ImmValue 5))
              Assignment(MemoryLocation "r", Constant(ImmValue 1))

              WhileLoop(
                  Binary(BinOp.Lt, Constant(ImmValue 1), Variable(MemoryLocation "n")),
                  Block
                      [ Assignment(
                            MemoryLocation "r",
                            Binary(BinOp.Mul, Variable(MemoryLocation "r"), Variable(MemoryLocation "n"))
                        )
                        Assignment(
                            MemoryLocation "n",
                            Binary(BinOp.Sub, Variable(MemoryLocation "n"), Constant(ImmValue 1))
                        ) ]
              )

              PrintNumber(Variable(MemoryLocation "r")) ]
    )

[<Test>]
let testIfFactorialProgramParsesCorrectly () =
    Assert.AreEqual(factorialProgram, factorialAssembly |> parse_program)

[<Test>]
let testIfAllSampleProgramsParses () =
    for sample in
        [ SampleAsm.Fact
          SampleAsm.Fib
          SampleAsm.Sierpinski
          SampleAsm.Carpet
          SampleAsm.Mandelbrot
          SampleAsm.Primes
          SampleAsm.Expressions
          SampleAsm.ControlFlow ] do
        let _ = sample |> read_sample_program |> parse_program
        ()

    Assert.Pass()

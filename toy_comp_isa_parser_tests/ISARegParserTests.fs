module toy_comp_isa_parser_tests.ISARegParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ResourceUtility
open toy_comp_core.ISAReg
open toy_comp_isa_parsers.ISARegParser

[<Test>]
let testIfFactorialAssemblyParses () =
    let factorial_program =
        Program(
            Map
                [ ProgramStartLabel,
                  { Ops =
                      [ Li(Register 0uy, ImmValue 5)
                        St(MemoryLocation "n", Register 0uy)
                        Li(Register 0uy, ImmValue 1)
                        St(MemoryLocation "r", Register 0uy) ]
                    NextBlockLabel = Label "4" }
                  Label "4",
                  { Ops =
                      [ Li(Register 0uy, ImmValue 1)
                        Ld(Register 1uy, MemoryLocation "n")
                        Bin(BinOp.Lt, Register 0uy, Register 0uy, Register 1uy)
                        Jz(Label "18", Register 0uy)
                        Ld(Register 0uy, MemoryLocation "r")
                        Ld(Register 1uy, MemoryLocation "n")
                        Bin(BinOp.Mul, Register 0uy, Register 0uy, Register 1uy)
                        St(MemoryLocation "r", Register 0uy)
                        Ld(Register 0uy, MemoryLocation "n")
                        Li(Register 1uy, ImmValue 1)
                        Bin(BinOp.Sub, Register 0uy, Register 0uy, Register 1uy)
                        St(MemoryLocation "n", Register 0uy)
                        Jmp(Label "4") ]
                    NextBlockLabel = Label "18" }
                  Label "18",
                  { Ops = [ Ld(Register 0uy, MemoryLocation "r"); OutN(Register 0uy) ]
                    NextBlockLabel = ProgramEndLabel } ]
        )
    let factorial_assembly = Sample.Fact |> read_sample_asm ISAKind.Reg
    Assert.AreEqual(factorial_program, factorial_assembly |> parse_isa_reg)

[<Test>]
let testIfAllSampleAssemblyParses () =
    for sample in
        [ Sample.Fact
          Sample.Fib
          Sample.Sierpinski
          Sample.Carpet
          Sample.Mandelbrot
          Sample.Primes
          Sample.Expressions
          Sample.ControlFlow ] do
        let _ = sample |> read_sample_asm ISAKind.Reg |> parse_isa_reg
        ()

    Assert.Pass()

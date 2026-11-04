module toy_comp_isa_parser_tests.ISAStackParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ResourceUtility
open toy_comp_core.ISAStack
open toy_comp_isa_parsers.ISAStackParser

let factorialAssembly = SampleAsm.Fact |> read_sample_asm ISAKind.Stack
let factorialProgram = Program (Map [
    ProgramStartLabel, {
        Ops = [
            Push(ImmValue 5)
            St(MemoryLocation "n")
            Push(ImmValue 1)
            St(MemoryLocation "r")
        ]
        NextBlockLabel = Label "4"
    }
    Label "4", {
        Ops = [
            Push(ImmValue 1)
            Ld(MemoryLocation "n")
            Bin BinOp.Lt
            Jz(Label "18")
            Ld(MemoryLocation "r")
            Ld(MemoryLocation "n")
            Bin BinOp.Mul
            St(MemoryLocation "r")
            Ld(MemoryLocation "n")
            Push(ImmValue 1)
            Bin BinOp.Sub
            St(MemoryLocation "n")
            Jmp(Label "4")
        ]
        NextBlockLabel = Label "18"
    }
    Label "18", {
        Ops = [
            Ld(MemoryLocation "r")
            OutN
        ]
        NextBlockLabel = ProgramEndLabel
    }
])

[<Test>]
let testIfFactorialAssemblyParses () =
    Assert.AreEqual(factorialProgram, factorialAssembly |> parse_isa_stack)

[<Test>]
let testIfAllSampleAssemblyParses () =
    for sample in [
        SampleAsm.Fact
        SampleAsm.Fib
        SampleAsm.Sierpinski
        SampleAsm.Carpet
        SampleAsm.Mandelbrot
        SampleAsm.Primes
    ] do
        let _ = sample |> read_sample_asm ISAKind.Stack |> parse_isa_stack
        ()
    Assert.Pass()
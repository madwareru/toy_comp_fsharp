module toy_comp_isa_parser_tests.ISAMemParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ResourceUtility
open toy_comp_core.ISAMem
open toy_comp_isa_parsers.ISAMemParser

[<Test>]
let testIfFactorialAssemblyParses () =
    let factorial_program =
        Program(
            Map
                [ ProgramStartLabel,
                  { Ops =
                      [ Li(MemoryLocation "0", ImmValue 5)
                        Mov(MemoryLocation "n", MemoryLocation "0")
                        Li(MemoryLocation "1", ImmValue 1)
                        Mov(MemoryLocation "r", MemoryLocation "1") ]
                    NextBlockLabel = Label "4" }
                  Label "4",
                  { Ops =
                      [ Li(MemoryLocation "5", ImmValue 1)
                        Bin(BinOp.Lt, MemoryLocation "4", MemoryLocation "5", MemoryLocation "n")
                        Jz(Label "14", MemoryLocation "4")
                        Bin(BinOp.Mul, MemoryLocation "6", MemoryLocation "r", MemoryLocation "n")
                        Mov(MemoryLocation "r", MemoryLocation "6")
                        Li(MemoryLocation "8", ImmValue 1)
                        Bin(BinOp.Sub, MemoryLocation "7", MemoryLocation "n", MemoryLocation "8")
                        Mov(MemoryLocation "n", MemoryLocation "7")
                        Jmp(Label "4") ]
                    NextBlockLabel = Label "14" }
                  Label "14",
                  { Ops = [ OutN(MemoryLocation "r") ]
                    NextBlockLabel = ProgramEndLabel } ]
        )
    let factorial_assembly = Sample.Fact |> read_sample_asm ISAKind.Mem
    
    Assert.AreEqual(factorial_program, factorial_assembly |> parse_isa_mem)

[<Test>]
let testIfAllSampleAssemblyParses () =
    for sample in sample_collection () do
        let _ = sample |> read_sample_asm ISAKind.Mem |> parse_isa_mem
        ()

    Assert.Pass()

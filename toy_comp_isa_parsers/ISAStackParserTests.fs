module toy_comp_isa_parsers.ISAStackParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ISAStack
open toy_comp_isa_parsers.ISAStackParser

let factorialAssembly = @"
    PUSH #5
    ST [n]
    PUSH #1
    ST [r]
@4:
    PUSH #1
    LD [n]
    LT
    JZ @18
    LD [r]
    LD [n]
    MUL
    ST [r]
    LD [n]
    PUSH #1
    SUB
    ST [n]
    JMP @4
@18:
    LD [r]
    OUT"

let factorialProgram = Program (Map [
    ProgramStartLabel, {
        Ops = [
            Push { Value = ImmValue 5 }
            St { Destination = MemoryLocation "n" }
            Push { Value = ImmValue 1 }
            St { Destination = MemoryLocation "r" }
        ]
        NextBlockLabel = Label "4"
    }
    Label "4", {
        Ops = [
            Push { Value = ImmValue 1 }
            Ld { Source = MemoryLocation "n" }
            Bin BinOp.Lt
            Jz { DestinationLabel = Label "18" }
            Ld { Source = MemoryLocation "r" }
            Ld { Source = MemoryLocation "n" }
            Bin BinOp.Mul
            St { Destination = MemoryLocation "r" }
            Ld { Source = MemoryLocation "n" }
            Push { Value = ImmValue 1 }
            Bin BinOp.Sub
            St { Destination = MemoryLocation "n" }
            Jmp { DestinationLabel = Label "4" }
        ]
        NextBlockLabel = Label "18"
    }
    Label "18", {
        Ops = [
            Ld { Source = MemoryLocation "r" }
            Out
        ]
        NextBlockLabel = ProgramEndLabel
    }
])

[<Test>]
let testIfFactorialAssemblyParses () =
    Assert.AreEqual(factorialProgram, factorialAssembly |> parse_isa_stack)


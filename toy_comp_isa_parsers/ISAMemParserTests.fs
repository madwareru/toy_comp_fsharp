module toy_comp_isa_parsers.ISAMemParserTests

open NUnit.Framework
open toy_comp_core.ISAShared
open toy_comp_core.ISAMem
open toy_comp_isa_parsers.ISAMemParser

let factorialAssembly = @"
    LI [0], #5
    MOV [n], [0]
    LI [1], #1
    MOV [r], [1]
@4:
    LI [5], #1
    LT [4], [5], [n]
    JZ @14, [4]
    MUL [6], [r], [n]
    MOV [r], [6]
    LI [8], #1
    SUB [7], [n], [8]
    MOV [n], [7]
    JMP @4
@14:
    OUT [r]"
let factorialProgram = Program (Map [
    ProgramStartLabel, {
        Ops = [
            Li { Location = MemoryLocation "0"; Value = ImmValue 5 }
            Mov { Destination = MemoryLocation "n"; Source = MemoryLocation "0" }
            Li { Location = MemoryLocation "1"; Value = ImmValue 1 }
            Mov { Destination = MemoryLocation "r"; Source = MemoryLocation "1" }
        ]
        NextBlockLabel = Label "4"
    }
    Label "4", {
        Ops = [
            Li { Location = MemoryLocation "5"; Value = ImmValue 1 }
            Bin {
                Op = BinOp.Lt
                Destination = MemoryLocation "4"
                Lhs = MemoryLocation "5"
                Rhs = MemoryLocation "n"
            }
            Jz { DestinationLabel = Label "14"; CheckedLocation = MemoryLocation "4" }
            Bin {
                Op = BinOp.Mul
                Destination = MemoryLocation "6"
                Lhs = MemoryLocation "r"
                Rhs = MemoryLocation "n"
            }
            Mov { Destination = MemoryLocation "r"; Source = MemoryLocation "6" }
            Li { Location = MemoryLocation "8"; Value = ImmValue 1 }
            Bin {
                Op = BinOp.Sub
                Destination = MemoryLocation "7"
                Lhs = MemoryLocation "n"
                Rhs = MemoryLocation "8"
            }
            Mov { Destination = MemoryLocation "n"; Source = MemoryLocation "7" }
            Jmp { DestinationLabel = Label "4" }
        ]
        NextBlockLabel = Label "14"
    }
    Label "14", {
        Ops = [
            Out { Location = MemoryLocation "r" }
        ]
        NextBlockLabel = ProgramEndLabel
    }
])

[<Test>]
let testIfFactorialAssemblyParses () =
    Assert.AreEqual(factorialProgram, factorialAssembly |> parse_isa_mem)
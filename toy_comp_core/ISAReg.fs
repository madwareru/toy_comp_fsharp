module toy_comp_core.ISAReg

open toy_comp_core.ISAShared

type Register = Register of int

type LoadImm = { DestinationRegister: Register; Value: ImmValue }
type MemLoad = { DestinationRegister: Register; Source: MemoryLocation }
type MemStore = { Destination: MemoryLocation; SourceRegister: Register }
type Binary = { Op: BinOp; Destination: Register; Lhs: Register; Rhs: Register }
type JumpIfZero = { DestinationLabel: Label; CheckedRegister: Register }
type Jump = { DestinationLabel: Label }
type OutOp = { RegisterToOutput: Register }

type Operation =
    | Li of LoadImm
    | Ld of MemLoad
    | St of MemStore
    | Bin of Binary
    | Jz of JumpIfZero
    | Jmp of Jump
    | Out of OutOp
    
type Block = { Ops: Operation list; NextBlockLabel: Label }

type Program = Program of Map<Label, Block>
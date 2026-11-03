module toy_comp_core.ISAMem

open toy_comp_core.ISAShared

type LoadImm = { Location: MemoryLocation; Value: ImmValue }
type MoveMem = { Destination: MemoryLocation; Source: MemoryLocation }
type Binary = { Op: BinOp; Destination: MemoryLocation; Lhs: MemoryLocation; Rhs: MemoryLocation  }
type JumpIfZero = { DestinationLabel: Label; CheckedLocation: MemoryLocation }
type Jump = { DestinationLabel: Label }
type OutOp = { Location: MemoryLocation }

type Operation =
    | Li of LoadImm
    | Mov of MoveMem
    | Bin of Binary
    | Jz of JumpIfZero
    | Jmp of Jump
    | Out of OutOp
    
type Block = { Ops: Operation list; NextBlockLabel: Label }

type Program = Program of Map<Label, Block>
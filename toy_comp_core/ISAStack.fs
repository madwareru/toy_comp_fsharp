module toy_comp_core.ISAStack

open toy_comp_core.ISAShared

type PushImm = { Value: ImmValue }
type MemLoad = { Source: MemoryLocation }
type MemStore = { Destination: MemoryLocation }
type JumpIfZero = { DestinationLabel: Label }
type Jump = { DestinationLabel: Label }

type Operation =
    | Push of PushImm
    | Ld of MemLoad
    | St of MemStore
    | Bin of BinOp
    | Jz of JumpIfZero
    | Jmp of Jump
    | Out
    
type Block = { Ops: Operation list; NextBlockLabel: Label }

type Program = Program of Map<Label, Block>
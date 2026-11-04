module toy_comp_core.ISAStack

open toy_comp_core.ISAShared

type Operation =
    | Push of ImmValue
    | Ld of MemoryLocation
    | St of MemoryLocation
    | Bin of BinOp
    | Jz of Label
    | Jmp of Label
    | OutC
    | OutN

type Block = { Ops: Operation list; NextBlockLabel: Label }

type Program = Program of Map<Label, Block>
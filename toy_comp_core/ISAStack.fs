module toy_comp_core.ISAStack

open toy_comp_core.ISAShared

type Program = Program of Map<Label, Block>

and Block =
    { Ops: Operation list
      NextBlockLabel: Label }

and Operation =
    | Push of ImmValue
    | Ld of MemoryLocation
    | St of MemoryLocation
    | Bin of BinOp
    | Jz of Label
    | Jmp of Label
    | OutC
    | OutN

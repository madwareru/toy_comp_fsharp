module toy_comp_core.ISAMem

open toy_comp_core.ISAShared

type Program = Program of Map<Label, Block>

and Block =
    { Ops: Operation list
      NextBlockLabel: Label }

and Operation =
    | Li of MemoryLocation * ImmValue
    | Mov of MemoryLocation * MemoryLocation
    | Bin of BinOp * MemoryLocation * MemoryLocation * MemoryLocation
    | Jz of Label * MemoryLocation
    | Jmp of Label
    | OutС of MemoryLocation
    | OutN of MemoryLocation

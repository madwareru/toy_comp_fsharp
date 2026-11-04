module toy_comp_core.ISAReg

open toy_comp_core.ISAShared

type Register = Register of byte

type Operation =
    | Li of Register * ImmValue
    | Ld of Register * MemoryLocation
    | St of MemoryLocation * Register
    | Bin of BinOp * Register * Register * Register
    | Jz of Label * Register
    | Jmp of Label
    | OutC of Register
    | OutN of Register
    
type Block = { Ops: Operation list; NextBlockLabel: Label }

type Program = Program of Map<Label, Block>
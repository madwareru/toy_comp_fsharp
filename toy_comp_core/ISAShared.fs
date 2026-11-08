module toy_comp_core.ISAShared

type ImmValue = ImmValue of int64
type MemoryLocation = MemoryLocation of string

type Label =
    | ProgramStartLabel
    | Label of string
    | ProgramEndLabel

type BinOp =
    | Add
    | Sub
    | Mul
    | Eq
    | Lt
    | Leq

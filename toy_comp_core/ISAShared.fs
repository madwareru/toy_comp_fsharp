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
    
let calc_binop lhs rhs bin_op =
    match bin_op with
    | Add -> lhs + rhs
    | Sub -> lhs - rhs
    | Mul -> lhs * rhs
    | Eq -> if lhs = rhs then 1L else 0L
    | Leq -> if lhs <= rhs then 1L else 0L
    | Lt -> if lhs < rhs then 1L else 0L

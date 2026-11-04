module toy_comp_core.AST

open toy_comp_core.ISAShared
    
type Program = Program of Block
and Block = Block of Statement list
and Statement =
    | Assignment of MemoryLocation * Expr
    | PutChar of Expr
    | PrintNumber of Expr
    | Branch of Expr * Block * Block
    | WhileLoop of Expr * Block
and Expr =
    | Constant of ImmValue
    | Variable of MemoryLocation
    | Binary of BinOp * Expr * Expr
module toy_comp_compiler.ConstantFolding

open toy_comp_core.ISAShared
open toy_comp_core.AST

let fold_consts_in_program = function
    | Program top_level_block ->
        let rec fold_expr = function
            | Variable(location) -> Variable(location)
            | Constant(ImmValue value) -> Constant(ImmValue value)
            | Binary(bin_op, lhs, rhs) ->
                match bin_op, fold_expr lhs, fold_expr rhs with
                | Add, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) -> Constant(ImmValue(lhs + rhs))
                | Add, Constant(ImmValue(0L)), rhs -> rhs
                | Add, lhs, Constant(ImmValue(0L)) -> lhs
                
                | Sub, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) -> Constant(ImmValue(lhs - rhs))
                | Sub, lhs, Constant(ImmValue(0L)) -> lhs
                
                | Mul, Constant(ImmValue(0L)), _ -> Constant(ImmValue(0L))
                | Mul, _, Constant(ImmValue(0L)) -> Constant(ImmValue(0L))
                | Mul, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) -> Constant(ImmValue(lhs * rhs))
                | Mul, Constant(ImmValue(1L)), rhs -> rhs
                | Mul, lhs, Constant(ImmValue(1L)) -> lhs
                
                | Eq, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) ->
                    Constant(ImmValue(if lhs = rhs then 1 else 0))
                | Leq, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) ->
                    Constant(ImmValue(if lhs <= rhs then 1 else 0))
                | Lt, Constant(ImmValue(lhs)), Constant(ImmValue(rhs)) ->
                    Constant(ImmValue(if lhs < rhs then 1 else 0))
                | bin_op, lhs, rhs -> Binary(bin_op, lhs, rhs)
                
        let rec fold_block = function
            | Block ops ->
                ops |> List.map (function
                    | Assignment(loc, expr) -> Assignment(loc, fold_expr expr)
                    | PutChar(expr) -> PutChar(fold_expr expr)
                    | PrintNumber(expr) -> PrintNumber(fold_expr expr)
                    | Branch(cond_expr, then_block, else_block) ->
                        Branch(fold_expr cond_expr, fold_block then_block, fold_block else_block)
                    | WhileLoop(cond_expr, body) -> WhileLoop(fold_expr cond_expr, fold_block body)
                ) |> Block
                
        top_level_block |> fold_block |> Program
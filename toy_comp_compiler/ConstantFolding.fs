module toy_comp_compiler.ConstantFolding

open toy_comp_core.ISAShared
open toy_comp_core.AST

let fold_consts_in_program (Program top_level_block) =
    let rec fold_block (Block statements) = statements |> List.map fold_statement |> Block
    and fold_statement = function
        | Assignment(loc, expr) -> Assignment(loc, fold_expr expr)
        | PutChar(expr) -> PutChar(fold_expr expr)
        | PrintNumber(expr) -> PrintNumber(fold_expr expr)
        | Branch(cond_expr, then_block, else_block) ->
            let cond_expr' = cond_expr |> fold_expr
            let then_block' = fold_block then_block
            let else_block' = fold_block else_block
            Branch(cond_expr', then_block', else_block')
        | WhileLoop(cond_expr, body) ->
            let cond_expr' = cond_expr |> fold_expr
            let body' = body |> fold_block
            WhileLoop(cond_expr', body')
    and fold_expr = function
        | Variable(location) -> Variable(location)
        | Constant(ImmValue value) -> Constant(ImmValue value)
        | Binary(bin_op, lhs, rhs) ->
            let lhs' = lhs |> fold_expr
            let rhs' = rhs |> fold_expr
            match bin_op, lhs', rhs' with
            | Add, Constant(ImmValue(0L)), e -> e
            | Add, e, Constant(ImmValue(0L)) -> e
            | Sub, e, Constant(ImmValue(0L)) -> e
            | Mul, Constant(ImmValue(1L)), e -> e
            | Mul, e, Constant(ImmValue(1L)) -> e
            | Mul, Constant(ImmValue(0L)), _ -> Constant(ImmValue(0L))
            | Mul, _, Constant(ImmValue(0L)) -> Constant(ImmValue(0L))
            | _, Constant(ImmValue(l)), Constant(ImmValue(r)) -> bin_op |> calc_binop l r |> ImmValue |> Constant
            | _, l, r -> Binary(bin_op, l, r)

    top_level_block |> fold_block |> Program

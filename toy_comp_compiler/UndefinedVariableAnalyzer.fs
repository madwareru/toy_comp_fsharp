module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST

let analyze_for_unassigned_variables (Program root_block) : Program =
    let rec check_expr defined_set expr =
        match expr with
        | Constant _ -> ()
        | Variable location ->
            let (MemoryLocation name) = location
            if not(defined_set |> Set.contains location) then
                failwith $"Undefined variable: {name}"
        | Binary(_, lhs, rhs) ->
            check_expr defined_set lhs
            check_expr defined_set rhs

    let rec check_block defined_set (Block statements) =
        let mutable defined_set = defined_set
        for statement in statements do
            defined_set <-
                match statement with
                | Assignment(dst, expr) ->
                    check_expr defined_set expr
                    Set.add dst defined_set
                | PutChar expr
                | PrintNumber expr ->
                    check_expr defined_set expr
                    defined_set
                | Branch(Constant (ImmValue 0L), _, else_block) ->
                    // Since we know for sure that only else branch executes,
                    // we don't need to get intersection of two sets
                    check_block defined_set else_block
                | Branch(Constant (ImmValue _), then_block, _) ->
                    // The same as previous case, but with then branch instead
                    check_block defined_set then_block
                | Branch(cond, then_block, else_block) ->
                    check_expr defined_set cond
                    let then_defined = check_block defined_set then_block
                    let else_defined = check_block defined_set else_block
                    Set.intersect then_defined else_defined
                | WhileLoop(Constant (ImmValue 0L), _) ->
                    // This loop never executes, no new variables will be set
                    defined_set
                | WhileLoop(Constant (ImmValue _), body) ->
                    // Infinite loop, which actually will never terminate...
                    check_block defined_set body
                | WhileLoop(cond, body) ->
                    check_expr defined_set cond
                    check_block defined_set body |> ignore
                    // In the case of a loop we can't guarantee that a loop executed (for example, when
                    // condition contained a variable, and we don't know its value at a compile time).
                    // We can't guarantee that any variable from a loop body will be set.
                    defined_set
        defined_set
    
    check_block Set.empty root_block |> ignore
    Program root_block
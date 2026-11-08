module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST

let analyze_for_unassigned_variables (Program root_block) : Program =
    let rec check_expr defined expr =
        match expr with
        | Constant _ -> ()
        | Variable location ->
            if not (Set.contains location defined) then
                let (MemoryLocation name) = location
                failwith $"Undefined variable: {name}"
        | Binary(_, lhs, rhs) ->
            check_expr defined lhs
            check_expr defined rhs

    let rec check_block defined (Block statements) =
        match statements with
        | [] -> defined
        | statement :: rest ->
            let next_defined =
                match statement with
                | Assignment(dst, expr) ->
                    check_expr defined expr
                    Set.add dst defined
                | PutChar expr
                | PrintNumber expr ->
                    check_expr defined expr
                    defined
                | Branch(cond, then_block, else_block) ->
                    check_expr defined cond
                    let then_defined = check_block defined then_block
                    let else_defined = check_block defined else_block
                    Set.intersect then_defined else_defined
                | WhileLoop(cond, body) ->
                    check_expr defined cond
                    check_block defined body |> ignore
                    defined

            check_block next_defined (Block rest)
    
    check_block Set.empty root_block |> ignore
    Program root_block
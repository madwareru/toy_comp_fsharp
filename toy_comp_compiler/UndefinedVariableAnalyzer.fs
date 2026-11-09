module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_compiler.CompilerShared

type private TrackedValue =
    | Known of int64
    | Unknown

let analyze_for_unassigned_variables (Program root_block) : Program =
    let rec eval_expr expr assignments =
        match expr with
        | Constant(ImmValue value) -> Known value
        | Variable location ->
            let (MemoryLocation name) = location
            match assignments |> Map.tryFind location with
            | Some value -> value
            | None -> failwith $"Undefined variable: {name}"
        | Binary(bin_op, lhs, rhs) ->
            let lhs_value = assignments |> eval_expr lhs
            let rhs_value = assignments |> eval_expr rhs
            match lhs_value, rhs_value with
            | Known lhs, Known rhs -> bin_op |> calc_binop lhs rhs |> Known
            | _ -> Unknown

    let rec assigned_locations (Block statements) =
        let stmt_locations = function
            | Assignment(dst, _) -> Set.singleton dst
            | Branch(_, then_block, else_block) ->
                let then_locations = assigned_locations then_block
                let else_locations = assigned_locations else_block 
                Set.union then_locations else_locations
            | WhileLoop(_, body) -> assigned_locations body
            | PutChar _
            | PrintNumber _ -> Set.empty
        
        statements |> List.map stmt_locations |> Set.unionMany

    let invalidate assigned state =
        let invalidate_pair location value = if Set.contains location assigned then Unknown else value
        state |> Map.map invalidate_pair
    
    let merge_branch_assignments then_assignments else_assignments =
        let mapping = function
            | Known lhs, Known rhs when lhs = rhs -> Known lhs
            | _ -> Unknown
        map_zip mapping then_assignments else_assignments

    let rec check_block_and_gather_assignments parent_assignments (Block statements) =
        let mutable block_assignments = parent_assignments
        
        let check_statement = function
            | Assignment(dst, expr) ->
                let value = block_assignments |> eval_expr expr
                block_assignments <- Map.add dst value block_assignments
            | PutChar expr
            | PrintNumber expr ->
                block_assignments |> eval_expr expr |> ignore
            | Branch(cond, then_block, else_block) ->
                match block_assignments |> eval_expr cond with
                | Known 0L -> block_assignments <- check_block_and_gather_assignments block_assignments else_block
                | Known _ -> block_assignments <- check_block_and_gather_assignments block_assignments then_block
                | Unknown ->
                    let then_assignments = check_block_and_gather_assignments block_assignments then_block
                    let else_assignments = check_block_and_gather_assignments block_assignments else_block
                    block_assignments <- merge_branch_assignments then_assignments else_assignments
            | WhileLoop(cond, body) ->
                match block_assignments |> eval_expr cond with
                | Known 0L -> ()
                | Known _ ->
                    let body_assignments = body |> check_block_and_gather_assignments block_assignments
                    block_assignments <- body_assignments |> invalidate (assigned_locations body)
                | Unknown -> 
                    check_block_and_gather_assignments block_assignments body |> ignore
                    block_assignments <- invalidate (assigned_locations body) block_assignments
        
        for statement in statements do
            check_statement statement
                
        block_assignments

    root_block |> check_block_and_gather_assignments Map.empty |> ignore
    Program root_block
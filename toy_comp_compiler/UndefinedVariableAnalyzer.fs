module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST

type private TrackedValue =
    | Known of int64
    | Unknown

let analyze_for_unassigned_variables (Program root_block) : Program =
    let rec eval_expr state expr =
        match expr with
        | Constant(ImmValue value) -> Known value
        | Variable location ->
            let (MemoryLocation name) = location
            match Map.tryFind location state with
            | Some value -> value
            | None -> failwith $"Undefined variable: {name}"
        | Binary(op, lhs, rhs) ->
            let lhs_value = eval_expr state lhs
            let rhs_value = eval_expr state rhs
            match lhs_value, rhs_value with
            | Known lhs, Known rhs ->
                match op with
                | Add -> Known(lhs + rhs)
                | Sub -> Known(lhs - rhs)
                | Mul -> Known(lhs * rhs)
                | Eq -> Known(if lhs = rhs then 1L else 0L)
                | Lt -> Known(if lhs < rhs then 1L else 0L)
                | Leq -> Known(if lhs <= rhs then 1L else 0L)
            | _ -> Unknown

    let rec assigned_locations (Block statements) =
        statements
        |> List.map (function
            | Assignment(dst, _) -> Set.singleton dst
            | Branch(_, then_block, else_block) ->
                Set.union (assigned_locations then_block) (assigned_locations else_block)
            | WhileLoop(_, body) -> assigned_locations body
            | PutChar _
            | PrintNumber _ -> Set.empty)
        |> Set.unionMany

    let invalidate assigned state =
        state
        |> Map.map (fun location value ->
            if Set.contains location assigned then Unknown else value)

    let merge_branches then_state else_state =
        then_state
        |> Map.fold (fun merged location then_value ->
            match Map.tryFind location else_state with
            | Some else_value ->
                let value =
                    match then_value, else_value with
                    | Known lhs, Known rhs when lhs = rhs -> Known lhs
                    | _ -> Unknown
                Map.add location value merged
            | None -> merged) Map.empty

    let rec check_block state (Block statements) =
        let mutable state = state
        for statement in statements do
            state <-
                match statement with
                | Assignment(dst, expr) ->
                    let value = eval_expr state expr
                    Map.add dst value state
                | PutChar expr
                | PrintNumber expr ->
                    eval_expr state expr |> ignore
                    state
                | Branch(cond, then_block, else_block) ->
                    match eval_expr state cond with
                    | Known 0L -> check_block state else_block
                    | Known _ -> check_block state then_block
                    | Unknown ->
                        let then_state = check_block state then_block
                        let else_state = check_block state else_block
                        merge_branches then_state else_state
                | WhileLoop(Constant(ImmValue 0L), _) -> state
                | WhileLoop(Constant(ImmValue _), body) ->
                    check_block state body
                    |> invalidate (assigned_locations body)
                | WhileLoop(cond, body) ->
                    eval_expr state cond |> ignore
                    check_block state body |> ignore
                    invalidate (assigned_locations body) state
        state

    check_block Map.empty root_block |> ignore
    Program root_block
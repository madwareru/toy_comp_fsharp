module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST

let rec private check_expr defined expr =
    match expr with
    | Constant _ -> Ok()
    | Variable location when Set.contains location defined -> Ok()
    | Variable location -> Error location
    | Binary(_, lhs, rhs) ->
        check_expr defined lhs |> Result.bind (fun () -> check_expr defined rhs)

let rec private check_block defined (Block statements) =
    match statements with
    | [] -> Ok defined
    | statement :: rest ->
        let result =
            match statement with
            | Assignment(dst, expr) ->
                check_expr defined expr |> Result.map (fun () -> Set.add dst defined)
            | PutChar expr
            | PrintNumber expr -> check_expr defined expr |> Result.map (fun () -> defined)
            | Branch _
            | WhileLoop _ -> failwith "Task 2"

        result |> Result.bind (fun next_defined -> check_block next_defined (Block rest))

let analyze_program (Program rootBlock) : Result<unit, MemoryLocation> =
    check_block Set.empty rootBlock |> Result.map ignore

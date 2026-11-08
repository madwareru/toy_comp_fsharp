module toy_comp_compiler.UndefinedVariableAnalyzer

open toy_comp_core.ISAShared
open toy_comp_core.AST

let rec private check_expr defined expr =
    match expr with
    | Constant _ -> ()
    | Variable location ->
        if not (Set.contains location defined) then
            let (MemoryLocation name) = location
            failwith $"Undefined variable: {name}"
    | Binary(_, lhs, rhs) ->
        check_expr defined lhs
        check_expr defined rhs

let rec private check_block defined (Block statements) =
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
            | Branch(cond, thenBlock, elseBlock) ->
                check_expr defined cond
                let thenDefined = check_block defined thenBlock
                let elseDefined = check_block defined elseBlock
                Set.intersect thenDefined elseDefined
            | WhileLoop(cond, body) ->
                check_expr defined cond
                check_block defined body |> ignore
                defined

        check_block next_defined (Block rest)

let analyze_program (Program rootBlock) : unit =
    check_block Set.empty rootBlock |> ignore

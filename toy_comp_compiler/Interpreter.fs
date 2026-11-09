module toy_comp_compiler.Interpreter

open System.Text
open toy_comp_compiler.CompilerShared
open toy_comp_core.ISAShared
open toy_comp_core.AST

let interp_program (program: Program) =
    match program with
    | Program top_level_block ->
        let mutable memory = Map.empty
        let mem_set key value = memory <- memory |> Map.add key value
        let mem_get key = memory[key]

        let mutable text_buffer = StringBuilder()

        let rec interp_expr =
            function
            | Variable(location) -> mem_get location
            | Constant(ImmValue value) -> value
            | Binary(bin_op, lhs, rhs) ->
                let lhs = interp_expr lhs
                let rhs = interp_expr rhs
                bin_op |> calc_binop lhs rhs

        let rec interp_block =
            function
            | Block ops ->
                for op in ops do
                    match op with
                    | Assignment(location, expr) -> interp_expr expr |> mem_set location
                    | PutChar(expr) ->
                        // We support printing of an ASCII part of a symbol.
                        // We are doing so by just % 128 and printing of it.
                        // Anything above 127 will just wrap
                        text_buffer <- text_buffer.Append((interp_expr expr) % 128L |> char)
                    | PrintNumber(expr) -> text_buffer <- text_buffer.AppendLine((interp_expr expr).ToString())
                    | Branch(cond, then_block, else_block) ->
                        if interp_expr cond <> 0 then
                            interp_block then_block
                        else
                            interp_block else_block
                    | WhileLoop(cond, body_block) ->
                        while interp_expr cond <> 0 do
                            interp_block body_block

        interp_block top_level_block
        text_buffer.ToString()

module toy_comp_compiler.ISAMemCompiler

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_core.ISAMem
open toy_comp_compiler.CompilerShared

type private Instruction =
    | Op of Operation
    | LabelDef of Label

type private SourceProgram = toy_comp_core.AST.Program
type private TargetProgram = toy_comp_core.ISAMem.Program

let compile_program_isa_mem (mode: CompilationMode) (program: SourceProgram) : TargetProgram CompilationResult =

    match program with
    | toy_comp_core.AST.Program(root_block) ->
        let mutable next_label_id = 0

        let allocate_label () =
            let next_label = Label(next_label_id.ToString())
            next_label_id <- next_label_id + 1
            next_label

        let mutable reg_top = 0uy

        let inc_reg_top () =
            if reg_top = 255uy then
                failwith "Compilation failed: maximum register top reached and there was overflow attempt"
            else
                reg_top <- reg_top + 1uy

        let dec_reg_top () =
            if reg_top = 0uy then
                failwith "Compilation failed: minimum register top reached and there wad underflow attempt"
            else
                reg_top <- reg_top - 1uy

        let get_tmp offset =
            MemoryLocation($"tmp_{reg_top - offset}")

        let rec compile_expr =
            function
            | Constant(value) ->
                seq {
                    yield Op(Li(get_tmp 0uy, value))
                    inc_reg_top ()
                }
            | Variable(src) ->
                seq {
                    yield Op(Mov(get_tmp 0uy, src))
                    inc_reg_top ()
                }
            | Binary(bin_op, Variable(lhs), Variable(rhs)) ->
                seq {
                    yield Op(Bin(bin_op, get_tmp 0uy, lhs, rhs))
                    inc_reg_top ()
                }
            | Binary(bin_op, Constant(lhs), Variable(rhs)) ->
                seq {
                    yield Op(Li(get_tmp 0uy, lhs))
                    yield Op(Bin(bin_op, get_tmp 0uy, get_tmp 0uy, rhs))
                    inc_reg_top ()
                }
            | Binary(bin_op, Variable(lhs), Constant(rhs)) ->
                seq {
                    yield Op(Li(get_tmp 0uy, rhs))
                    yield Op(Bin(bin_op, get_tmp 0uy, lhs, get_tmp 0uy))
                    inc_reg_top ()
                }
            | Binary(bin_op, lhs, rhs) ->
                seq {
                    yield! compile_expr lhs
                    yield! compile_expr rhs
                    dec_reg_top ()
                    yield Op(Bin(bin_op, get_tmp 1uy, get_tmp 1uy, get_tmp 0uy))
                }

        let rec compile_expr_direct dst =
            function
            | Constant(value) -> seq { yield Op(Li(dst, value)) }
            | Variable(src) -> seq { yield Op(Mov(dst, src)) }
            | Binary(bin_op, Variable(lhs), Variable(rhs)) -> seq { yield Op(Bin(bin_op, dst, lhs, rhs)) }
            | Binary(bin_op, Constant(lhs), Variable(rhs)) ->
                seq {
                    yield Op(Li(get_tmp 0uy, lhs))
                    yield Op(Bin(bin_op, dst, get_tmp 0uy, rhs))
                }
            | Binary(bin_op, Variable(lhs), Constant(rhs)) ->
                seq {
                    yield Op(Li(get_tmp 0uy, rhs))
                    yield Op(Bin(bin_op, dst, lhs, get_tmp 0uy))
                }
            | Binary(bin_op, lhs, rhs) ->
                seq {
                    yield! compile_expr lhs
                    yield! compile_expr rhs
                    dec_reg_top ()
                    yield Op(Bin(bin_op, dst, get_tmp 1uy, get_tmp 0uy))
                    dec_reg_top ()
                }

        let rec compile_block =
            function
            | Block(statements) ->
                seq {
                    for statement in statements do
                        match statement with
                        | Assignment(dst, Constant(src)) -> yield Op(Li(dst, src))
                        | Assignment(dst, Variable(src)) -> yield Op(Mov(dst, src))
                        | Assignment(dst, Binary(bin_op, Variable(lhs), Variable(rhs))) ->
                            yield Op(Bin(bin_op, dst, lhs, rhs))
                        | Assignment(dst, src) -> yield! compile_expr_direct dst src
                        | PutChar(Variable(src)) -> yield Op(OutС(src))
                        | PutChar(src) ->
                            yield! compile_expr src
                            dec_reg_top ()
                            yield Op(OutС(get_tmp 0uy))
                        | PrintNumber(Variable(src)) -> yield Op(OutN(src))
                        | PrintNumber(src) ->
                            yield! compile_expr src
                            dec_reg_top ()
                            yield Op(OutN(get_tmp 0uy))
                        | Branch(_, Block [], Block [])
                        | Branch(Constant(ImmValue(0L)), _, Block [])
                        | Branch(Constant(ImmValue(1L)), Block [], _)
                        | WhileLoop(Constant(ImmValue(0L)), _) ->
                            // Dead code, legally do nothing here
                            ()
                        | Branch(Constant(ImmValue(0L)), _, else_block) -> yield! compile_block else_block
                        | Branch(Constant(ImmValue _), then_block, _) -> yield! compile_block then_block
                        | Branch(Variable(cond), then_block, Block([])) ->
                            let label_after = allocate_label ()
                            yield Op(Jz(label_after, cond))
                            yield! compile_block then_block
                            yield LabelDef(label_after)
                        | Branch(cond, then_block, Block([])) ->
                            let label_after = allocate_label ()
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(label_after, get_tmp 0uy))
                            yield! compile_block then_block
                            yield LabelDef(label_after)
                        | Branch(Variable(cond), then_block, else_block) ->
                            let else_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield Op(Jz(else_label, cond))
                            yield! compile_block then_block
                            yield Op(Jmp(label_after))
                            yield LabelDef(else_label)
                            yield! compile_block else_block
                            yield LabelDef(label_after)
                        | Branch(cond, then_block, else_block) ->
                            let else_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(else_label, get_tmp 0uy))
                            yield! compile_block then_block
                            yield Op(Jmp(label_after))
                            yield LabelDef(else_label)
                            yield! compile_block else_block
                            yield LabelDef(label_after)
                        | WhileLoop(Constant(ImmValue _), body) ->
                            // Infinite loop
                            let loop_label = allocate_label ()
                            yield LabelDef(loop_label)
                            yield! compile_block body
                            yield Op(Jmp(loop_label))
                        | WhileLoop(Variable(cond), body) ->
                            let loop_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield LabelDef(loop_label)
                            yield Op(Jz(label_after, cond))
                            yield! compile_block body
                            yield Op(Jmp(loop_label))
                            yield LabelDef(label_after)
                        | WhileLoop(cond, body) ->
                            let loop_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield LabelDef(loop_label)
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(label_after, get_tmp 0uy))
                            yield! compile_block body
                            yield Op(Jmp(loop_label))
                            yield LabelDef(label_after)
                }

        let mutable program_map = Map.empty
        let mutable label = ProgramStartLabel
        let mutable ops = []

        for instruction in compile_block root_block do
            match instruction with
            | Op op -> ops <- op :: ops
            | LabelDef next_label ->
                program_map <-
                    program_map.Add(
                        label,
                        { Ops = List.rev ops
                          NextBlockLabel = next_label }
                    )

                label <- next_label
                ops <- []

        program_map <-
            program_map.Add(
                label,
                { Ops = List.rev ops
                  NextBlockLabel = ProgramEndLabel }
            )

        match mode with
        | Text ->
            let mutable text_buffer = StringBuilder()
            let mutable current_label = ProgramStartLabel

            while current_label <> ProgramEndLabel do
                match current_label with
                | Label(label_name) -> text_buffer <- text_buffer.AppendLine($"@{label_name}:")
                | _ -> ()

                let op_str op dst lhs rhs = $"    {op} [{dst}], [{lhs}], [{rhs}]"

                let current_block = program_map[current_label]

                for operation in current_block.Ops do
                    let instruction_str =
                        match operation with
                        | Li(MemoryLocation(dst), ImmValue(value)) -> $"    LI [{dst}], #{value}"
                        | Mov(MemoryLocation(dst), MemoryLocation(src)) -> $"    MOV [{dst}], [{src}]"
                        | Jz(Label(label_name), MemoryLocation(src)) -> $"    JZ @{label_name}, [{src}]"
                        | Jmp(Label(label_name)) -> $"    JMP @{label_name}"
                        | Bin(Add, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "ADD"
                        | Bin(Sub, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "SUB"
                        | Bin(Mul, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "MUL"
                        | Bin(Eq, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "EQ"
                        | Bin(Leq, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "LE"
                        | Bin(Lt, MemoryLocation d, MemoryLocation l, MemoryLocation r) -> (d, l, r) |||> op_str "LT"
                        | OutС(MemoryLocation(src)) -> $"    OUT [{src}]"
                        | OutN(MemoryLocation(src)) -> $"    OUTN [{src}]"
                        | Jmp _ -> failwith "unrecheable"
                        | Jz _ -> failwith "unrecheable"

                    text_buffer <- text_buffer.AppendLine(instruction_str)

                current_label <- current_block.NextBlockLabel

            CompiledText(text_buffer.ToString())
        | ISA -> CompiledISA(Program program_map)

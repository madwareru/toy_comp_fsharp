module toy_comp_compiler.ISARegCompiler

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_core.ISAReg
open toy_comp_compiler.CompilerShared
open toy_comp_compiler.UndefinedVariableAnalyzer

type private Instruction =
    | Op of Operation
    | LabelDef of Label

type private SourceProgram = toy_comp_core.AST.Program
type private TargetProgram = toy_comp_core.ISAReg.Program

let compile_program_isa_reg (mode: CompilationMode) (program: SourceProgram) : TargetProgram CompilationResult =
    analyze_program program

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

        let rec compile_expr =
            function
            | Constant(value) ->
                seq {
                    yield Op(Li(Register(reg_top), value))
                    inc_reg_top ()
                }
            | Variable(src) ->
                seq {
                    yield Op(Ld(Register(reg_top), src))
                    inc_reg_top ()
                }
            | Binary(bin_op, lhs, rhs) ->
                seq {
                    yield! compile_expr lhs
                    yield! compile_expr rhs
                    dec_reg_top ()
                    yield Op(Bin(bin_op, Register(reg_top - 1uy), Register(reg_top - 1uy), Register(reg_top)))
                }

        let rec compile_block =
            function
            | Block(statements) ->
                seq {
                    for statement in statements do
                        match statement with
                        | Assignment(dst, src) ->
                            yield! compile_expr src
                            dec_reg_top ()
                            yield Op(St(dst, Register(reg_top)))
                        | PutChar(src) ->
                            yield! compile_expr src
                            dec_reg_top ()
                            yield Op(OutC(Register(reg_top)))
                        | PrintNumber(src) ->
                            yield! compile_expr src
                            dec_reg_top ()
                            yield Op(OutN(Register(reg_top)))
                        | Branch(_, Block [], Block [])
                        | Branch(Constant(ImmValue(0L)), _, Block [])
                        | Branch(Constant(ImmValue _), Block [], _)
                        | WhileLoop(Constant(ImmValue(0L)), _) ->
                            // Dead code, legally do nothing here
                            ()
                        | Branch(Constant(ImmValue(0L)), _, else_block) -> yield! compile_block else_block
                        | Branch(Constant(ImmValue _), then_block, _) -> yield! compile_block then_block
                        | Branch(cond, then_block, Block([])) ->
                            let label_after = allocate_label ()
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(label_after, Register(reg_top)))
                            yield! compile_block then_block
                            yield LabelDef(label_after)
                        | Branch(cond, then_block, else_block) ->
                            let else_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(else_label, Register(reg_top)))
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
                        | WhileLoop(cond, body) ->
                            let loop_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield LabelDef(loop_label)
                            yield! compile_expr cond
                            dec_reg_top ()
                            yield Op(Jz(label_after, Register(reg_top)))
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

                let current_block = program_map[current_label]

                for operation in current_block.Ops do
                    let instruction_str =
                        match operation with
                        | Li(Register(dst), ImmValue(value)) -> $"    LI r{dst}, #{value}"
                        | Ld(Register(dst), MemoryLocation(src)) -> $"    LD r{dst}, [{src}]"
                        | St(MemoryLocation(dst), Register(src)) -> $"    ST [{dst}], r{src}"
                        | Jz(Label(label_name), Register(src)) -> $"    JZ @{label_name}, r{src}"
                        | Jmp(Label(label_name)) -> $"    JMP @{label_name}"
                        | Bin(Add, Register(dst), Register(lhs), Register(rhs)) -> $"    ADD r{dst}, r{lhs}, r{rhs}"
                        | Bin(Sub, Register(dst), Register(lhs), Register(rhs)) -> $"    SUB r{dst}, r{lhs}, r{rhs}"
                        | Bin(Mul, Register(dst), Register(lhs), Register(rhs)) -> $"    MUL r{dst}, r{lhs}, r{rhs}"
                        | Bin(Eq, Register(dst), Register(lhs), Register(rhs)) -> $"    EQ r{dst}, r{lhs}, r{rhs}"
                        | Bin(Leq, Register(dst), Register(lhs), Register(rhs)) -> $"    LE r{dst}, r{lhs}, r{rhs}"
                        | Bin(Lt, Register(dst), Register(lhs), Register(rhs)) -> $"    LT r{dst}, r{lhs}, r{rhs}"
                        | OutC(Register(src)) -> $"    OUT r{src}"
                        | OutN(Register(src)) -> $"    OUTN r{src}"
                        | Jmp _ -> failwith "unrecheable"
                        | Jz _ -> failwith "unrecheable"

                    text_buffer <- text_buffer.AppendLine(instruction_str)

                current_label <- current_block.NextBlockLabel

            CompiledText(text_buffer.ToString())
        | ISA -> CompiledISA(Program program_map)

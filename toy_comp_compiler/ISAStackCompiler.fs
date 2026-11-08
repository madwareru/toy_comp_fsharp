module toy_comp_compiler.ISAStackCompiler

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_core.ISAStack
open toy_comp_compiler.CompilerShared
open toy_comp_compiler.UndefinedVariableAnalyzer

type private Instruction =
    | Op of Operation
    | LabelDef of Label

type private SourceProgram = toy_comp_core.AST.Program
type private TargetProgram = toy_comp_core.ISAStack.Program

let compile_program_isa_stack (mode: CompilationMode) (program: SourceProgram) : TargetProgram CompilationResult =
    analyze_program program

    match program with
    | toy_comp_core.AST.Program(root_block) ->
        let mutable next_label_id = 0

        let allocate_label () =
            let next_label = Label(next_label_id.ToString())
            next_label_id <- next_label_id + 1
            next_label

        let rec compile_expr =
            function
            | Constant(value) -> Op(Push(value)) |> Seq.singleton
            | Variable(src) -> Op(Ld(src)) |> Seq.singleton
            | Binary(bin_op, lhs, rhs) ->
                seq {
                    yield! compile_expr lhs
                    yield! compile_expr rhs
                    yield Op(Bin bin_op)
                }

        let rec compile_block =
            function
            | Block(statements) ->
                seq {
                    for statement in statements do
                        match statement with
                        | Assignment(dst, src) ->
                            yield! compile_expr src
                            yield Op(St(dst))
                        | PutChar(src) ->
                            yield! compile_expr src
                            yield Op(OutC)
                        | PrintNumber(src) ->
                            yield! compile_expr src
                            yield Op(OutN)
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
                            yield Op(Jz(label_after))
                            yield! compile_block then_block
                            yield LabelDef(label_after)
                        | Branch(cond, then_block, else_block) ->
                            let else_label = allocate_label ()
                            let label_after = allocate_label ()
                            yield! compile_expr cond
                            yield Op(Jz(else_label))
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
                            yield Op(Jz(label_after))
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
                        | Push(ImmValue(value)) -> $"    PUSH #{value}"
                        | Ld(MemoryLocation(src)) -> $"    LD [{src}]"
                        | St(MemoryLocation(dst)) -> $"    ST [{dst}]"
                        | Jz(Label(label_name)) -> $"    JZ @{label_name}"
                        | Jmp(Label(label_name)) -> $"    JMP @{label_name}"
                        | Bin(Add) -> "    ADD"
                        | Bin(Sub) -> "    SUB"
                        | Bin(Mul) -> "    MUL"
                        | Bin(Eq) -> "    EQ"
                        | Bin(Leq) -> "    LE"
                        | Bin(Lt) -> "    LT"
                        | OutC -> "    OUT"
                        | OutN -> "    OUTN"
                        | Jmp _ -> failwith "unrecheable"
                        | Jz _ -> failwith "unrecheable"

                    text_buffer <- text_buffer.AppendLine(instruction_str)

                current_label <- current_block.NextBlockLabel

            CompiledText(text_buffer.ToString())
        | ISA -> CompiledISA(Program program_map)

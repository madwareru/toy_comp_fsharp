module toy_comp_compiler.ISAStackCompiler

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.AST
open toy_comp_core.ISAStack
open toy_comp_compiler.CompilerShared
    
type private Instruction =
    | Op of Operation
    | LabelDef of Label
    
type SourceProgram = toy_comp_core.AST.Program
type TargetProgram = toy_comp_core.ISAStack.Program

let compile_program_isa_stack (mode : CompilationMode) (program : SourceProgram) : TargetProgram CompilationResult =
    match program with
    | toy_comp_core.AST.Program(root_block) ->
        let mutable next_label_id = 0
        let allocate_label () =
            let next_label = Label(next_label_id.ToString())
            next_label_id <- next_label_id + 1
            next_label
        
        let rec compile_expr = function
            | Constant(value) -> seq { Op(Push(value)) }
            | Variable(src) -> seq { Op(Ld(src)) }
            | Binary(bin_op, lhs, rhs) ->
                seq {
                    for instruction in compile_expr lhs do yield instruction
                    for instruction in compile_expr rhs do yield instruction
                    yield Op(Bin bin_op)
                }
                
        let rec compile_block = function
            | Block(statements) -> seq {
                for statement in statements do
                    match statement with
                    | Assignment(dst, src) ->
                        for instruction in compile_expr src do
                            yield instruction
                        yield Op(St(dst))
                    | PutChar(src) ->
                        for instruction in compile_expr src do
                            yield instruction
                        yield Op(OutC)
                    | PrintNumber(src) ->
                        for instruction in compile_expr src do
                            yield instruction
                        yield Op(OutN)
                    | WhileLoop(cond, body) ->
                        let loop_label = allocate_label ()
                        let label_after = allocate_label ()
                        yield LabelDef(loop_label)
                        for cond_instruction in compile_expr cond do
                            yield cond_instruction
                        yield Op(Jz(label_after))
                        for body_instruction in compile_block body do
                            yield body_instruction
                        yield Op(Jmp(loop_label))
                        yield LabelDef(label_after)
                    | Branch(cond, then_block, Block([])) ->
                        let label_after = allocate_label ()
                        for cond_instruction in compile_expr cond do
                            yield cond_instruction
                        yield Op(Jz(label_after))
                        for body_instruction in compile_block then_block do
                            yield body_instruction
                        yield LabelDef(label_after)
                    | Branch(cond, then_block, else_block) ->
                        let else_label = allocate_label ()
                        let label_after = allocate_label ()
                        for cond_instruction in compile_expr cond do
                            yield cond_instruction
                        yield Op(Jz(else_label))
                        for then_instruction in compile_block then_block do
                            yield then_instruction
                        yield Op(Jmp(label_after))
                        yield LabelDef(else_label)
                        for else_instruction in compile_block else_block do
                            yield else_instruction
                        yield LabelDef(label_after)
            }
            
        match mode with
        | Text ->
            let mutable text_buffer = StringBuilder()
            for instruction in compile_block(root_block) do
                match instruction with
                | LabelDef(Label(label_name)) -> text_buffer <- text_buffer.AppendLine($"@{label_name}:")
                | LabelDef _ -> failwith "unreacheable"
                | Op(operation) ->
                    match operation with
                    | Push(ImmValue(value)) -> text_buffer <- text_buffer.AppendLine($"    PUSH #{value}")
                    | Ld(MemoryLocation(src)) -> text_buffer <- text_buffer.AppendLine($"    LD [{src}]")
                    | St(MemoryLocation(dst)) -> text_buffer <- text_buffer.AppendLine($"    ST [{dst}]")
                    | Jz(Label(label_name)) -> text_buffer <- text_buffer.AppendLine($"    JZ @{label_name}")
                    | Jz _ -> failwith "unrecheable"
                    | Jmp(Label(label_name)) -> text_buffer <- text_buffer.AppendLine($"    JMP @{label_name}")
                    | Jmp _ -> failwith "unrecheable"
                    | Bin(Add) -> text_buffer <- text_buffer.AppendLine("    ADD")
                    | Bin(Sub) -> text_buffer <- text_buffer.AppendLine("    SUB")
                    | Bin(Mul) -> text_buffer <- text_buffer.AppendLine("    MUL")
                    | Bin(Eq) -> text_buffer <- text_buffer.AppendLine("    EQ")
                    | Bin(Leq) -> text_buffer <- text_buffer.AppendLine("    LEQ")
                    | Bin(Lt) -> text_buffer <- text_buffer.AppendLine("    LT")
                    | OutC -> text_buffer <- text_buffer.AppendLine("    OUTC")
                    | OutN -> text_buffer <- text_buffer.AppendLine("    OUTN")
            CompiledText(text_buffer.ToString())
        | ISA ->
            let mutable program_map = Map.empty
            let mutable label = ProgramStartLabel
            let mutable ops = []
            for instruction in compile_block(root_block) do
                match instruction with
                | Op op -> ops <- op :: ops
                | LabelDef next_label ->
                    program_map <- program_map.Add(label, { Ops = List.rev ops; NextBlockLabel = next_label })
                    label <- next_label
                    ops <- []
            program_map <- program_map.Add(label, { Ops = List.rev ops; NextBlockLabel = ProgramEndLabel })
            CompiledISA(Program program_map)
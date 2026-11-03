module toy_comp_isa_parsers.ISAStackParser

open FParsec
open toy_comp_core.ISAShared
open toy_comp_core.ISAStack
open toy_comp_isa_parsers.FParsecShared

type private Instruction =
    | Op of Operation
    | LabelDef of string

let private parse_instruction: Parser<Instruction, unit> =
    let parse_label_def = parse_label_ref .>> ws_no_new_line .>> pchar ':' |>> LabelDef

    let parse_push =
        parse_keyword "PUSH" >>. ws_no_new_line >>. parse_imm
        |>> fun value -> Operation.Push { Value = ImmValue value }

    let parse_ld =
        parse_keyword "LD" >>. ws_no_new_line >>. parse_addr
        |>> fun source -> Operation.Ld { Source = MemoryLocation source }

    let parse_st =
        parse_keyword "ST" >>. ws_no_new_line >>. parse_addr
        |>> fun destination -> Operation.St { Destination = MemoryLocation destination }

    let parse_jz =
        parse_keyword "JZ" >>. ws_no_new_line >>. parse_label_ref
        |>> fun label -> Operation.Jz { DestinationLabel = Label label }

    let parse_jmp =
        parse_keyword "JMP" >>. ws_no_new_line >>. parse_label_ref
        |>> fun label -> Operation.Jmp { DestinationLabel = Label label }

    let parse_out = parse_keyword "OUT" >>% Operation.Out

    let parse_binary = parse_bin_op |>> Operation.Bin

    let parse_operation =
        choice [
            parse_push
            parse_ld
            parse_st
            parse_jz
            parse_jmp
            parse_out
            parse_binary
        ] |>> Op

    (parse_label_def <|> parse_operation) .>> ws_no_new_line .>> parse_line_end

let private parse_program: Parser<Program, unit> =
    ws_all >>. many (parse_instruction .>> ws_all) .>> eof
    |>> fun parsed_instructions ->
        let mutable program_map = Map.empty
        let mutable label = ProgramStartLabel
        let mutable ops = []
        for instruction in parsed_instructions do
            match instruction with
            | Op op -> ops <- op :: ops
            | LabelDef next_label ->
                program_map <- program_map.Add(label, { Ops = List.rev ops; NextBlockLabel = Label next_label })
                label <- Label next_label
                ops <- []
        program_map <- program_map.Add(label, { Ops = List.rev ops; NextBlockLabel = ProgramEndLabel })
        Program program_map

let parse_isa_stack text =
    match run parse_program text with
    | Success (program, _, _) -> program
    | Failure (errorMessage, _, _) -> failwith errorMessage

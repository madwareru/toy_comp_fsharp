module toy_comp_isa_parsers.ISAMemParser

open FParsec
open toy_comp_core.ISAShared
open toy_comp_core.ISAMem
open toy_comp_isa_parsers.FParsecShared

type private Instruction =
    | Op of Operation
    | LabelDef of string

let private parse_instruction: Parser<Instruction, unit> =
    let parse_label_def =
        parse_label_ref .>> ws_no_new_line .>> pchar ':'
        |>> LabelDef

    let parse_li =
        parse_keyword "LI" >>. ws_no_new_line >>. parse_addr .>> parse_comma .>>. parse_imm
        |>> fun (location, value) -> Operation.Li { Location = MemoryLocation location; Value = ImmValue value }

    let parse_mov =
        parse_keyword "MOV" >>. ws_no_new_line >>. parse_addr .>> parse_comma .>>. parse_addr
        |>> fun (dst, src) -> Operation.Mov { Destination = MemoryLocation dst; Source = MemoryLocation src }

    let parse_jz =
        parse_keyword "JZ" >>. ws_no_new_line >>. parse_label_ref .>> parse_comma .>>. parse_addr
        |>> fun (label, checkedLocation) -> Operation.Jz {
            DestinationLabel = Label label
            CheckedLocation = MemoryLocation checkedLocation
        }

    let parse_jmp =
        parse_keyword "JMP" >>. ws_no_new_line >>. parse_label_ref
        |>> fun label -> Operation.Jmp { DestinationLabel = Label label }

    let parse_out =
        parse_keyword "OUT" >>. ws_no_new_line >>. parse_addr
        |>> fun location -> Operation.Out { Location = MemoryLocation location }
        
    let parse_binary =
        parse_bin_op .>> ws_no_new_line .>>. parse_addr .>> parse_comma .>>. parse_addr .>> parse_comma .>>. parse_addr
        |>> fun (((op, destination), lhs), rhs) -> Operation.Bin {
            Op = op
            Destination = MemoryLocation destination
            Lhs = MemoryLocation lhs
            Rhs = MemoryLocation rhs
        }

    let parse_operation = choice [parse_li; parse_mov; parse_jz; parse_jmp; parse_out; parse_binary] |>> Op

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

let parse_isa_mem (text: string) =
    match run parse_program text with
    | Success (program, _, _) -> program
    | Failure (errorMessage, _, _) -> failwith errorMessage

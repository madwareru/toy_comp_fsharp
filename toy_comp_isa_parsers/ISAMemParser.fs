module toy_comp_isa_parsers.ISAMemParser

open FParsec
open toy_comp_core.ISAShared
open toy_comp_core.ISAMem
open toy_comp_isa_parsers.FParsecShared

type private Instruction =
    | Op of Operation
    | LabelDef of Label

let private parse_instruction: Parser<Instruction, unit> =
    let parse_label_def = parse_label_ref .>> ws_no_new_line .>> pchar ':' |>> LabelDef

    let parse_li =
        parse_keyword "LI" >>. ws_no_new_line >>. parse_addr .>> parse_comma
        .>>. parse_imm
        |>> Li

    let parse_mov =
        parse_keyword "MOV" >>. ws_no_new_line >>. parse_addr .>> parse_comma
        .>>. parse_addr
        |>> Mov

    let parse_jz =
        parse_keyword "JZ" >>. ws_no_new_line >>. parse_label_ref .>> parse_comma
        .>>. parse_addr
        |>> Jz

    let parse_jmp = parse_keyword "JMP" >>. ws_no_new_line >>. parse_label_ref |>> Jmp
    let parse_out = parse_keyword "OUT" >>. ws_no_new_line >>. parse_addr |>> OutС
    let parse_outn = parse_keyword "OUTN" >>. ws_no_new_line >>. parse_addr |>> OutN

    let parse_binary =
        parse_bin_op .>> ws_no_new_line .>>. parse_addr .>> parse_comma .>>. parse_addr
        .>> parse_comma
        .>>. parse_addr
        |>> flatten4
        |>> Bin

    let parse_operation =
        choice
            [ parse_li
              parse_mov
              parse_jz
              parse_jmp
              parse_outn
              parse_out
              parse_binary ]
        |>> Op

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

        Program program_map

let parse_isa_mem (text: string) =
    match run parse_program text with
    | Success(program, _, _) -> program
    | Failure(errorMessage, _, _) -> failwith errorMessage

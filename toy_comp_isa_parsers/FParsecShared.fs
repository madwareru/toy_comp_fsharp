module toy_comp_isa_parsers.FParsecShared

open System
open FParsec
open toy_comp_core.ISAShared

let private is_ident_char c = Char.IsAsciiLetterOrDigit c || c = '_'
let private is_ws_no_new_line_char c = c <> '\n' && Char.IsWhiteSpace c

let ws_no_new_line: Parser<unit, unit> = manySatisfy is_ws_no_new_line_char |>> ignore
let ws_all: Parser<unit, unit> = manySatisfy Char.IsWhiteSpace |>> ignore
let parse_line_end: Parser<unit, unit> = (pchar '\n' >>% ()) <|> followedBy eof
let parse_comma = ws_no_new_line >>. pchar ',' .>> ws_no_new_line

let parse_location_id: Parser<string, unit> =
    many1Satisfy2 Char.IsAsciiLetter is_ident_char
    <|> many1Satisfy Char.IsAsciiDigit

let parse_addr = pchar '[' >>. parse_location_id .>> pchar ']'
let parse_reg: Parser<string, unit> = pchar 'r' >>. many1Satisfy Char.IsAsciiDigit
let parse_label_ref = pchar '@' >>. parse_location_id
let parse_imm: Parser<int64, unit> = pchar '#' >>. pint64

let parse_keyword keyword : Parser<string, unit> = pstring keyword .>> notFollowedBy (satisfy is_ident_char)
let parse_bin_op =
    choice [
        parse_keyword "ADD" >>% BinOp.Add
        parse_keyword "SUB" >>% BinOp.Sub
        parse_keyword "MUL" >>% BinOp.Mul
        parse_keyword "EQ" >>% BinOp.Eq
        parse_keyword "LT" >>% BinOp.Lt
        parse_keyword "LE" >>% BinOp.Leq
    ]

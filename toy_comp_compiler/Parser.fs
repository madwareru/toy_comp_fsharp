module toy_comp_compiler.Parser

open System
open FParsec

open toy_comp_core.AST
open toy_comp_core.ISAShared

let private is_ident_char c = Char.IsAsciiLetterOrDigit c || c = '_'

let ws: Parser<unit, unit> = spaces
let private token p = ws >>. p .>> ws
let private parse_token op_s: Parser<string, unit> = token (pstring op_s)
let private parse_semicolon = parse_token ";" >>% ()
let private parse_colon = parse_token ":" >>% ()
let private parse_assignment_op = parse_token "=" >>% ()
let private parse_lparen = parse_token "("
let private parse_rparen = parse_token ")"

let private flatten3 ((a, b), c) = a, b, c
let private flatten4 (((a, b), c), d) = a, b, c, d

let parse_var_name: Parser<MemoryLocation, unit> =
    many1Satisfy2 Char.IsAsciiLetter is_ident_char
    |>> MemoryLocation
    
let parse_expr, parse_expr_ref = createParserForwardedToRef()
    
let parse_var_name_expr: Parser<Expr, unit> = parse_var_name |>> Variable

let parse_constant_expr: Parser<Expr, unit> =
    pint64 |>> ImmValue |>> Constant

let parse_term_expr: Parser<Expr, unit> =
    choice [
        parse_var_name_expr
        parse_constant_expr
        parse_lparen >>. parse_expr .>> parse_rparen
    ]
    
let opp = OperatorPrecedenceParser<Expr, unit, unit>()
opp.TermParser <- parse_term_expr

opp.AddOperator(InfixOperator("==", ws, 1, Associativity.Left, fun x y -> Binary(Eq, x, y)))
opp.AddOperator(InfixOperator("<=", ws, 2, Associativity.Left, fun x y -> Binary(Leq, x, y)))
opp.AddOperator(InfixOperator("<", ws, 3, Associativity.Left, fun x y -> Binary(Lt, x, y)))
opp.AddOperator(InfixOperator("+", ws, 4, Associativity.Left, fun x y -> Binary(Add, x, y)))
opp.AddOperator(InfixOperator("-", ws, 4, Associativity.Left, fun x y -> Binary(Sub, x, y)))
opp.AddOperator(InfixOperator("*", ws, 5, Associativity.Left, fun x y -> Binary(Mul, x, y)))

do parse_expr_ref.Value <- opp.ExpressionParser

let parse_stmt, parse_stmt_ref = createParserForwardedToRef()

let parse_block: Parser<Block, unit> = many parse_stmt |>> Block

let parse_assignment: Parser<Statement, unit> =
    parse_var_name .>> parse_assignment_op .>>. parse_expr
    |>> Assignment

let parse_if_else: Parser<Statement, unit> =
    (parse_token "if") >>. parse_expr .>> parse_colon .>>. parse_block
    .>> (parse_token "else") .>> parse_colon .>>. parse_block .>> (parse_token "end")
    |>> flatten3 |>> Branch

let parse_if: Parser<Statement, unit> = 
    (parse_token "if") >>. parse_expr .>> parse_colon .>>. parse_block .>> (parse_token "end")
    |>> fun (cond, then_expr) -> Branch(cond, then_expr, Block [])
    
let parse_while: Parser<Statement, unit> = 
    (parse_token "while") >>. parse_expr .>> parse_colon .>>. parse_block .>> (parse_token "end")
    |>> WhileLoop
    
let parse_put_char: Parser<Statement, unit> =
    (parse_token "out") >>. parse_lparen >>. parse_expr .>> parse_rparen
    |>> PutChar
    
let parse_print_number: Parser<Statement, unit> =
    (parse_token "outn") >>. parse_lparen >>. parse_expr .>> parse_rparen
    |>> PrintNumber
    
do parse_stmt_ref.Value <-
    choice [
        parse_print_number
        parse_put_char
        parse_if_else
        parse_if
        parse_while
        parse_assignment
    ] .>> parse_semicolon

let parse_program (text: string) =
    match run (parse_block .>> eof |>> Program) text with
    | Success (program, _, _) -> program
    | Failure (errorMessage, _, _) -> failwith errorMessage

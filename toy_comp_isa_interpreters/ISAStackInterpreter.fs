module toy_comp_isa_interpreters.ISAStackInterpreter

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.ISAStack

let interp_isa_stack_program = function
    | Program blocks ->
        let start_block = blocks[ProgramStartLabel]
        
        let mutable current_label = ProgramStartLabel
        let mutable pc = start_block.Ops
        let mutable next_label = start_block.NextBlockLabel
        let mutable memory = Map.empty
        let mutable stack = []
        let mutable text_buffer = StringBuilder()
        
        let mem_set key value = memory <- memory |> Map.add key value
        let mem_get key =
            match memory |> Map.tryFind key with
            | Some v -> v
            | _ -> failwith $"value for {key} is undefined"
        
        let stack_push value = stack <- value :: stack
        let stack_pop () =
            match stack with
            | [] -> failwith "stack underflow"
            | v :: rest ->
                stack <- rest
                v
        
        let goto = function
            | ProgramEndLabel as lbl -> current_label <- lbl
            | lbl ->
                let next_block = blocks[lbl]
                pc <- next_block.Ops
                next_label <- next_block.NextBlockLabel
                current_label <- lbl
        
        while current_label <> ProgramEndLabel do
            match pc with
                | [] -> goto next_label
                | Jmp(lbl_to) :: _ -> goto lbl_to
                | Jz(lbl_to) :: next_pc ->
                    match stack_pop () with
                    | 0L -> goto lbl_to
                    | _ -> pc <- next_pc
                | Ld(src) :: next_pc ->
                    mem_get src |> stack_push
                    pc <- next_pc
                | St(dst) :: next_pc ->
                    stack_pop () |> mem_set dst
                    pc <- next_pc
                | Push(ImmValue value) :: next_pc ->
                    value |> stack_push
                    pc <- next_pc
                | OutC :: next_pc ->
                    // We support printing of an ASCII part of a symbol.
                    // We are doing so by just % 128 and printing of it.
                    // Anything above 127 will just wrap
                    text_buffer <- text_buffer.Append(stack_pop () % 128L |> char)
                    pc <- next_pc
                | OutN :: next_pc ->
                    text_buffer <- text_buffer.AppendLine((stack_pop ()).ToString())
                    pc <- next_pc
                | Bin(bin_op) :: next_pc ->
                    let rhs = stack_pop ()
                    let lhs = stack_pop ()
                
                    match bin_op with
                    | Add -> lhs + rhs
                    | Sub -> lhs - rhs
                    | Mul -> lhs * rhs
                    | Eq -> if lhs = rhs then 1 else 0
                    | Leq -> if lhs <= rhs then 1 else 0
                    | Lt -> if lhs < rhs then 1 else 0
                    |> stack_push
                    
                    pc <- next_pc
        
        text_buffer.ToString()
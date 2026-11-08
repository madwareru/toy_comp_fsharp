module toy_comp_isa_interpreters.ISARegInterpreter

open System.Text
open toy_comp_core.ISAShared
open toy_comp_core.ISAReg

let interp_isa_reg_program =
    function
    | Program blocks ->
        let start_block = blocks[ProgramStartLabel]

        let mutable current_label = ProgramStartLabel
        let mutable pc = start_block.Ops
        let mutable next_label = start_block.NextBlockLabel
        let mutable memory = Map.empty
        let mutable registers = Array.zeroCreate 256
        let mutable text_buffer = StringBuilder()

        let mem_set key value = memory <- memory |> Map.add key value

        let mem_get key =
            memory |> Map.tryFind key |> Option.defaultWith (fun () -> 0L)

        let reg_set (id: byte) value = registers[id |> int] <- value
        let reg_get (id: byte) = registers[id |> int]

        let goto =
            function
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
            | Jz(lbl_to, Register(src)) :: next_pc ->
                match reg_get src with
                | 0L -> goto lbl_to
                | _ -> pc <- next_pc
            | Li(Register(dst), ImmValue(value)) :: next_pc ->
                reg_set dst value
                pc <- next_pc
            | Ld(Register(dst), src) :: next_pc ->
                mem_get src |> reg_set dst
                pc <- next_pc
            | St(dst, Register(src)) :: next_pc ->
                reg_get src |> mem_set dst
                pc <- next_pc
            | OutC(Register(src)) :: next_pc ->
                // We support printing of an ASCII part of a symbol.
                // We are doing so by just % 128 and printing of it.
                // Anything above 127 will just wrap
                text_buffer <- text_buffer.Append(reg_get src % 128L |> char)
                pc <- next_pc
            | OutN(Register(src)) :: next_pc ->
                text_buffer <- text_buffer.AppendLine((reg_get src).ToString())
                pc <- next_pc
            | Bin(bin_op, Register(dst), Register(lhs), Register(rhs)) :: next_pc ->
                let lhs = reg_get lhs
                let rhs = reg_get rhs

                match bin_op with
                | Add -> lhs + rhs
                | Sub -> lhs - rhs
                | Mul -> lhs * rhs
                | Eq -> if lhs = rhs then 1 else 0
                | Leq -> if lhs <= rhs then 1 else 0
                | Lt -> if lhs < rhs then 1 else 0
                |> reg_set dst

                pc <- next_pc

        text_buffer.ToString()

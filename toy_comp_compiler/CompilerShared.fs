module toy_comp_compiler.CompilerShared

type CompilationMode =
    | Text
    | ISA

type 't CompilationResult =
    | CompiledText of string
    | CompiledISA of 't
    
let map_zip (mapping : 'b * 'c -> 'd) (lhs : Map<'a, 'b>) (rhs : Map<'a, 'c>) : Map<'a, 'd> =
    let mutable zipped = Map.empty
    for pair in lhs do
        let k, v_l = pair.Key, pair.Value
        match rhs |> Map.tryFind k with
        | None -> ()
        | Some v_r -> zipped <- zipped |> Map.add k (mapping (v_l, v_r))
    zipped
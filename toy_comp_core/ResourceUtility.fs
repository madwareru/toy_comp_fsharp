module toy_comp_core.ResourceUtility

open System.IO
open System.Reflection

let private read_resource_as_text res_name =
    let res_name = $"toy_comp_core.Resources.{res_name}"
    let assembly = Assembly.GetExecutingAssembly()
    use stream = assembly.GetManifestResourceStream(res_name)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

type SampleAsm =
    | Fact
    | Fib
    | Sierpinski
    | Carpet
    | Mandelbrot
    | Primes
    | Expressions
    | ControlFlow

let private get_file_name =
    function
    | Fact -> "fact"
    | Fib -> "fib"
    | Sierpinski -> "sierpinski"
    | Carpet -> "carpet"
    | Mandelbrot -> "mandelbrot"
    | Primes -> "primes"
    | Expressions -> "expressions"
    | ControlFlow -> "control_flow"

type ISAKind =
    | Mem
    | Stack
    | Reg

let read_sample_asm isa_kind sample =
    let name_str = sample |> get_file_name

    let res_name =
        match isa_kind with
        | Mem -> $"Asm.{name_str}.asmm"
        | Stack -> $"Asm.{name_str}.asms"
        | Reg -> $"Asm.{name_str}.asmr"

    res_name |> read_resource_as_text

let read_sample_program sample =
    let name_str = sample |> get_file_name
    let res_name = $"Programs.{name_str}.mrb"
    res_name |> read_resource_as_text

let read_expected_out sample =
    let name_str = sample |> get_file_name
    let res_name = $"ExpectedOuts.{name_str}.txt"
    res_name |> read_resource_as_text
    
let sample_collection () = [
    SampleAsm.Fib
    SampleAsm.Fact
    SampleAsm.Primes
    SampleAsm.Sierpinski
    SampleAsm.Carpet
    SampleAsm.Mandelbrot
    SampleAsm.Expressions
    SampleAsm.ControlFlow
]

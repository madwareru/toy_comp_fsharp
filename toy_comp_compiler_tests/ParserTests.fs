module toy_comp_compiler_tests.ParserTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser

[<Test>]
let testIfAllSampleProgramsParses () =
    for sample in [
        //SampleAsm.Fact
        //SampleAsm.Fib
        //SampleAsm.Sierpinski
        //SampleAsm.Carpet
        //SampleAsm.Mandelbrot
        SampleAsm.Primes
    ] do
        let program = sample |> read_sample_program |> parse_program
        ()
    Assert.Pass()
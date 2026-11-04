module toy_comp_compiler_tests.InterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.Interpreter

[<Test>]
let testIfAllSampleProgramsParsesAndInterpretsAsExpected () =
    for sample_caption, sample in [
        "=== fib ===", SampleAsm.Fib
        "=== fact ===", SampleAsm.Fact
        "=== primes ===", SampleAsm.Primes
        "=== sierpinski ===", SampleAsm.Sierpinski
        "=== carpet ===", SampleAsm.Carpet
        "=== mandelbrot ===", SampleAsm.Mandelbrot
    ] do
        System.Console.WriteLine(sample_caption)
        let expected_out = sample |> read_expected_out
        let result = sample |> read_sample_program |> parse_program |> interp_program
        System.Console.WriteLine(result)
        Assert.AreEqual(expected_out, result)
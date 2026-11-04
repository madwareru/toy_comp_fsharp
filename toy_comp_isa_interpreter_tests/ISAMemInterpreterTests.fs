module toy_comp_isa_interpreter_tests.ISAMemInterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_isa_parsers.ISAMemParser
open toy_comp_isa_interpreters.ISAMemInterpreter

[<Test>]
let testIfAllSampleAssemblyParsesAndInterpretsAsExpected () =
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
        let result = sample |> read_sample_asm ISAKind.Mem |> parse_isa_mem |> interp_isa_mem_program
        System.Console.WriteLine(result)
        Assert.AreEqual(expected_out, result)
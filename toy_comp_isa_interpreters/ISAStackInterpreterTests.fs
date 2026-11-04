module toy_comp_isa_interpreters.ISAStackInterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_isa_parsers.ISAStackParser
open toy_comp_isa_interpreters.ISAStackInterpreter

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
        let result = sample |> read_sample_asm ISAKind.Stack |> parse_isa_stack |> interp_isa_stack_program
        System.Console.WriteLine(result)
        Assert.AreEqual(expected_out, result)
module toy_comp_isa_interpreter_tests.ISAStackInterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_isa_parsers.ISAStackParser
open toy_comp_isa_interpreters.ISAStackInterpreter

[<Test>]
let testIfAllSampleAssemblyParsesAndInterpretsAsExpected () =
    for sample in [
        SampleAsm.Fib
        SampleAsm.Fact
        SampleAsm.Primes
        SampleAsm.Sierpinski
        SampleAsm.Carpet
        SampleAsm.Mandelbrot
    ] do
        let expected_out = sample |> read_expected_out
        let result = sample |> read_sample_asm ISAKind.Stack |> parse_isa_stack |> interp_isa_stack_program
        Assert.AreEqual(expected_out, result)
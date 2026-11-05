module toy_comp_compiler_tests.ISAStackCompilerTests

open NUnit.Framework
open toy_comp_compiler.CompilerShared
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.ISAStackCompiler
open toy_comp_isa_interpreters.ISAStackInterpreter

[<Test>]
let testIfAllSampleProgramsCompilesAndInterpretsAsExpected () =
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
        let isa = sample |> read_sample_program |> parse_program |> compile_program_isa_stack CompilationMode.ISA
        match isa with
        | CompiledISA isa ->
            let result = isa |> interp_isa_stack_program
            System.Console.WriteLine(result)
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"
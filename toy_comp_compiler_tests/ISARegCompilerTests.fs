module toy_comp_compiler_tests.ISARegCompilerTests

open NUnit.Framework
open toy_comp_compiler.CompilerShared
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.ISARegCompiler
open toy_comp_isa_parsers.ISARegParser
open toy_comp_isa_interpreters.ISARegInterpreter

[<Test>]
let testIfAllSampleProgramsCompilesAndInterpretsAsExpected () =
    for sample in [
        SampleAsm.Fib
        SampleAsm.Fact
        SampleAsm.Primes
        SampleAsm.Sierpinski
        SampleAsm.Carpet
        SampleAsm.Mandelbrot
    ] do
        let expected_out = sample |> read_expected_out
        let isa = sample |> read_sample_program |> parse_program |> compile_program_isa_reg CompilationMode.ISA
        match isa with
        | CompiledISA isa ->
            let result = isa |> interp_isa_reg_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"
        
[<Test>]
let testIfAllSampleProgramsCompilesToTextCorrectly () =
    for sample in [
        SampleAsm.Fib
        SampleAsm.Fact
        SampleAsm.Primes
        SampleAsm.Sierpinski
        SampleAsm.Carpet
        SampleAsm.Mandelbrot
    ] do
        let expected_out = sample |> read_expected_out
        let isa = sample |> read_sample_program |> parse_program |> compile_program_isa_reg CompilationMode.Text
        match isa with
        | CompiledText text ->
            let result = text |> parse_isa_reg |> interp_isa_reg_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"

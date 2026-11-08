module toy_comp_compiler_tests.ISAStackCompilerTests

open NUnit.Framework
open toy_comp_compiler.CompilerShared
open toy_comp_compiler.UndefinedVariableAnalyzer
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.ConstantFolding
open toy_comp_compiler.ISAStackCompiler
open toy_comp_isa_parsers.ISAStackParser
open toy_comp_isa_interpreters.ISAStackInterpreter

[<Test>]
let testIfAllSampleProgramsCompilesAndInterpretsAsExpected () =
    for sample in
        [ SampleAsm.Fib
          SampleAsm.Fact
          SampleAsm.Primes
          SampleAsm.Sierpinski
          SampleAsm.Carpet
          SampleAsm.Mandelbrot ] do
        let expected_out = sample |> read_expected_out

        let isa =
            sample
            |> read_sample_program
            |> parse_program
            |> analyze_for_unassigned_variables
            |> fold_consts_in_program
            |> compile_program_isa_stack CompilationMode.ISA

        match isa with
        | CompiledISA isa ->
            let result = isa |> interp_isa_stack_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"

[<Test>]
let testIfAllSampleProgramsCompilesToTextCorrectly () =
    for sample in
        [ SampleAsm.Fib
          SampleAsm.Fact
          SampleAsm.Primes
          SampleAsm.Sierpinski
          SampleAsm.Carpet
          SampleAsm.Mandelbrot ] do
        let expected_out = sample |> read_expected_out

        let isa =
            sample
            |> read_sample_program
            |> parse_program
            |> fold_consts_in_program
            |> compile_program_isa_stack CompilationMode.Text

        match isa with
        | CompiledText text ->
            let result = text |> parse_isa_stack |> interp_isa_stack_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"

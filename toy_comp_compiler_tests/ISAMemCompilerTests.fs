module toy_comp_compiler_tests.ISAMemCompilerTests

open NUnit.Framework
open toy_comp_compiler.CompilerShared
open toy_comp_compiler.UndefinedVariableAnalyzer
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.ConstantFolding
open toy_comp_compiler.ISAMemCompiler
open toy_comp_isa_parsers.ISAMemParser
open toy_comp_isa_interpreters.ISAMemInterpreter

[<Test>]
let testIfAllSampleProgramsCompilesAndInterpretsAsExpected () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out

        let isa =
            sample
            |> read_sample_program
            |> parse_program
            |> fold_consts_in_program
            |> compile_program_isa_mem CompilationMode.ISA

        match isa with
        | CompiledISA isa ->
            let result = isa |> interp_isa_mem_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"

[<Test>]
let testIfAllSampleProgramsCompilesToTextCorrectly () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out

        let isa =
            sample
            |> read_sample_program
            |> parse_program
            |> analyze_for_unassigned_variables
            |> fold_consts_in_program
            |> compile_program_isa_mem CompilationMode.Text

        match isa with
        | CompiledText text ->
            let result = text |> parse_isa_mem |> interp_isa_mem_program
            Assert.AreEqual(expected_out, result)
        | _ -> failwith "unreacheable"

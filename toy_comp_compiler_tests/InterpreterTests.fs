module toy_comp_compiler_tests.InterpreterTests

open NUnit.Framework
open toy_comp_compiler.UndefinedVariableAnalyzer
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.Interpreter

[<Test>]
let testIfAllSampleProgramsParsesAndInterpretsAsExpected () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out

        let result =
            sample
            |> read_sample_program
            |> parse_program
            |> analyze_for_unassigned_variables
            |> interp_program

        Assert.AreEqual(expected_out, result)

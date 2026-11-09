module toy_comp_compiler_tests.ConstantFoldingTests

open NUnit.Framework
open toy_comp_compiler.UndefinedVariableAnalyzer
open toy_comp_core.ResourceUtility
open toy_comp_compiler.Parser
open toy_comp_compiler.ConstantFolding
open toy_comp_compiler.Interpreter

[<Test>]
let testIfAllResultsAreTheSameAfterConstandFoldedProgramInterpretation () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out
        let program =
            sample
            |> read_sample_program
            |> parse_program
            |> analyze_for_unassigned_variables
        let constant_folded_program = program |> fold_consts_in_program
        Assert.AreEqual(expected_out, program |> interp_program)
        Assert.AreEqual(expected_out, constant_folded_program |> interp_program)

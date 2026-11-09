module toy_comp_isa_interpreter_tests.ISARegInterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_isa_parsers.ISARegParser
open toy_comp_isa_interpreters.ISARegInterpreter

[<Test>]
let testIfAllSampleAssemblyParsesAndInterpretsAsExpected () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out

        let result =
            sample |> read_sample_asm ISAKind.Reg |> parse_isa_reg |> interp_isa_reg_program

        Assert.AreEqual(expected_out, result)

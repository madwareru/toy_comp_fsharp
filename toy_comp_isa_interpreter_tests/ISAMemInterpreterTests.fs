module toy_comp_isa_interpreter_tests.ISAMemInterpreterTests

open NUnit.Framework
open toy_comp_core.ResourceUtility
open toy_comp_isa_parsers.ISAMemParser
open toy_comp_isa_interpreters.ISAMemInterpreter

[<Test>]
let testIfAllSampleAssemblyParsesAndInterpretsAsExpected () =
    for sample in sample_collection () do
        let expected_out = sample |> read_expected_out

        let result =
            sample
            |> read_sample_asm ISAKind.Mem
            |> parse_isa_mem
            |> interp_isa_mem_program

        Assert.AreEqual(expected_out, result)

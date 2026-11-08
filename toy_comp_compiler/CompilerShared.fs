module toy_comp_compiler.CompilerShared

type CompilationMode =
    | Text
    | ISA

type 't CompilationResult =
    | CompiledText of string
    | CompiledISA of 't

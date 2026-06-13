# Test coverage expansion

- Status: Approved
- Plan file: `.alta/plans/2026-06-13-test-coverage-expansion.md`
- Created: 2026-06-13
- Task: Add a substantial, deterministic NUnit test suite that exercises the main CppAst.CodeGen C# model, writer, converter, options, mapping, and generated-output behaviors.
- Git: not ignored; commit this plan with the related implementation work

## Objective
- Significantly increase test coverage for the library by adding executable assertions around generated C# output and core C# AST/model behavior.
- Use `Zio.FileSystems.MemoryFileSystem` for generated output by default; do not add Verify/snapshot dependencies in this first pass unless an implementation blocker makes inline assertions impractical.
- Keep production changes limited to narrow fixes that new tests expose; avoid broad refactors and unrelated behavior changes.

## Context and evidence
- Existing tests are sparse: `src/CppAst.CodeGen.Tests/ConverterTests.cs` is marked `TODO: Write proper tests`, and several converter tests only dump generated text to `Console.WriteLine` without assertions.
- The test project is NUnit-based despite the repo guidance mentioning MSTest: `src/CppAst.CodeGen.Tests/CppAst.CodeGen.Tests.csproj` references `nunit`, `NUnit3TestAdapter`, and aliases legacy NUnit asserts.
- The library already references Zio: `src/CppAst.CodeGen/CppAst.CodeGen.csproj` has `PackageReference Include="Zio"`, and current tests already use `MemoryFileSystem`.
- Generated output can be captured deterministically: `CSharpCompilation.DumpTo(CodeWriter)` writes `CSharpGeneratedFile` members through `CodeWriter.PushFileOutput`, and `CodeWriterOptions` accepts a custom `IFileSystem` and newline.
- Converter behavior is plugin-driven via defaults registered in `CSharpConverterOptions`: comments, naming, containers, typedefs, enums, functions, parameters, structs, fields, function pointers, type conversion, mapping rules, DllImport/LibraryImport, and child visiting.
- Important uncovered production surfaces include `CSharpContainerList`, comments/XML docs, attribute text generation, `CSharpElementComparer`, type/member dumping, `DefaultTypeConverter`, structs/unions/bitfields, typedef wrapping, mapping rules/macros, DllImport/LibraryImport options, and per-include output dispatch.
- Current git state was clean at planning time (`main...origin/main` with no short-status entries).

## Assumptions and open decisions
- The user explicitly approved proceeding without plan validation, so this plan is marked Approved and should be executed immediately by Default mode.
- Prefer NUnit and existing project patterns; do not migrate test framework.
- Prefer direct, normalized string assertions and helper methods over checked-in snapshots/gold files to minimize dependency and file churn.
- If new tests reveal real defects, fix them in the smallest production-code change in the same logical slice; likely areas to watch are parent/validation handling in `CSharpContainerList`, name coverage in `CSharpElementComparer`, and macro-reference recovery in `DefaultMappingRulesConverter`.
- Objective-C-specific converter tests are lower priority and potentially parser/platform-sensitive on Windows; add only stable smoke coverage if a simple local parser scenario is reliable.

## Design notes
- Add a small shared test helper for conversion and generated-output capture:
  - Convert C/C++ text with `CSharpConverter.Convert` and an optional `CSharpConverterOptions` mutator.
  - Assert non-null compilation and no diagnostics before dumping.
  - Dump to `MemoryFileSystem` with `CodeWriterOptions.NewLine = "\n"` and return normalized generated text from `options.DefaultOutputFilePath`.
  - Provide helpers for multi-file output, line-ending normalization, and `Contains`/`DoesNotContain` groups.
- Keep most converter tests as integration tests over generated C# strings, because the library’s primary contract is generated code.
- Add focused model tests for `DumpTo`, attributes, comments, comparer, and containers where direct object assertions are clearer than full converter input.
- Use physical temporary input files only for `Convert(List<string> files, ...)` and `DispatchOutputPerInclude` tests; continue using `MemoryFileSystem` for generated C# output.
- Commit in coherent slices when green: test infrastructure, model/writer tests, converter integration tests, mapping/macro tests, and any production fixes.

## Risks and challenges
- Some converter scenarios depend on Clang/CppAst parse details and platform defaults; assertions should target stable generated fragments rather than every byte of large files.
- Adding tests around currently unasserted behavior may expose existing defects; fix only those required for the new intended behavior.
- Macro mapping has generated prefixes and preprocessing; tests must assert recovered public names and generated C# rather than transient prefix values.
- Per-include dispatch requires physical input files for the parser and careful cleanup.
- The repo has generated `bin/`/`obj/` folders in the workspace, but they are ignored; do not commit or churn them.

## Implementation checklist
- [x] Read approved plan and confirm workspace state before implementation.
- [x] Add test helper(s) under `src/CppAst.CodeGen.Tests/` (for example `TestHelpers/GeneratedCodeTestHelper.cs`) for convert-and-dump, multi-output capture, line-ending normalization, and grouped string assertions.
- [ ] Refactor `ConverterTests.cs` to use the helper and turn console-only tests into assertions covering anonymous/nested structs/unions/function pointers, macro-to-const/enum output, canonical enum typedef base types, and the larger function/typedef/string scenario.
- [ ] Add C# AST/model dumping tests covering `CSharpGeneratedFile`, namespace forms, classes/structs/interfaces/enums, generic parameters/where clauses, methods/properties/fields, fixed arrays, contextual return/parameter attributes, and `CSharpMethod.Wrap`/`Clone`/`ToFunctionPointer`.
- [ ] Add comment and XML-doc tests covering escaping, inline/self-closing XML comments, `CSharpFullComment`/`CSharpSimpleComment` prefixes, `CSharpParamComment`, `CSharpReturnComment`, `CSharpSinceComment`, and conversion of representative C++ Doxygen comments to generated XML docs.
- [ ] Add container/comparer/helper tests covering `CSharpContainerList` parent updates and validation, `CSharpElementComparer` equality/inequality for names, methods, fields, enum items, generics, nullable/pointer/ref/array/function-pointer types, `CSharpHelper` casing/name escaping, and calling-convention mapping.
- [ ] Add converter integration tests for function exports and interop attributes: default `LibraryImport`, `UseLibraryImport = false` `DllImport`, default calling convention overrides, `GenerateAsInternal`, custom namespace/class/default output path, and required using insertion/deduplication.
- [ ] Add converter integration tests for type conversion and options: primitive/bool/char behavior, `DefaultMarshalForBool`, `DisableRuntimeMarshalling`, `const char*` parameter/return/const field strings, `AllowMarshalForString`, `ManagedToUnmanagedStringTypeForParameter`, `MapVoidPtrToIntPtr`, auto-by-ref, manual `ByRef`/`NoByRef`, arrays/fixed buffers, and function pointer typedefs/parameters.
- [ ] Add converter integration tests for structs/unions/fields: sequential vs explicit layout, `FieldOffset(0)` for unions, bitfield backing fields and properties, opaque pointer structs, fixed-size arrays, global const vs ignored non-const globals, extern properties, nested anonymous type naming, and struct marshalling usage where observable.
- [ ] Add typedef/enum tests covering standard C typedefs, wrap/no-wrap/force options, `DisableTypedefToStructWrap`, pointer typedef wrapper operators, enum flags detection from shift expressions, `GenerateEnumItemAsFields`, and anonymous enum behavior.
- [ ] Add mapping-rule tests covering `Name`, `Type`, `InitValue`, `MarshalAs`, `Visibility`, `Discard`, `MapType`, regex captures, unnamed parameter matching (`argN`), macro-to-const, macro-to-enum, explicit casts, override values, and macro references to other mapped macros.
- [ ] Add file-input/per-include dispatch tests using temporary physical headers plus `MemoryFileSystem` output; assert output files/classes are created at expected `UPath`s and contain only the declarations from their source include.
- [ ] If any of the above tests fail because of confirmed library defects, apply the smallest production fix with a regression test and keep the commit focused.
- [ ] Remove or reduce `Console.WriteLine`-only test output once assertions cover the scenario.

## Verification checklist
- [ ] Run focused tests after each slice, e.g. `cd src; dotnet test -c Release --filter <new-test-class-or-category>`.
- [ ] Run the full suite: `cd src; dotnet test -c Release`.
- [ ] Run the release build: `cd src; dotnet build -c Release`.
- [ ] Self-review `git diff` to ensure only test files, narrow necessary source fixes, and this plan changed; verify ignored `bin/obj` artifacts are not included.
- [ ] Commit each green logical slice using the repo’s dotnet-releaser prefix rules (for example `Add test coverage helpers`, `Add converter mapping tests`, `Fix macro mapping references`).

## Handoff notes
- Execute this approved plan in Default mode now; no further user approval is needed.
- Keep tests deterministic with `NewLine = "\n"` and normalized generated strings.
- Prefer MemoryFileSystem output assertions over Verify snapshots to avoid adding dependencies and snapshot churn.
- Use temporary physical input files only where CppAst requires file paths; clean them up in test teardown/helpers.
- If the implementation gets large, use sequential read-only/implementation sub-sessions by area (model tests, converter types, mapping/macros) and commit after each passing slice.

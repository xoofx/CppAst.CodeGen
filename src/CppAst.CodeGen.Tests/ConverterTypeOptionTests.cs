// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class ConverterTypeOptionTests
{
    [Test]
    public void PrimitiveBoolCharVoidPointerAndStringOptionsAffectGeneratedTypes()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
const char* const_name = ""abc"";
EXPORT_API bool takes(bool flag, const char* text);
EXPORT_API const char* returns_text(void);
EXPORT_API void accept_void(void* data);
EXPORT_API char echo_char(char value);
"), options =>
        {
            options.DefaultMarshalForBool = new CSharpMarshalAsAttribute(UnmanagedType.I1);
            options.ManagedToUnmanagedStringTypeForParameter = "global::System.ReadOnlySpan<byte>";
            options.MapVoidPtrToIntPtr = true;
            options.CharAsByte = false;
        });

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public const string const_name = \"abc\";",
            "[return:global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.I1)]\n        public static partial bool takes([global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.I1)] bool flag, [global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPUTF8Str)] global::System.ReadOnlySpan<byte> text);",
            "[return:global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPUTF8Str)]\n        public static partial string returns_text();",
            "public static partial void accept_void(nint data);",
            "public static partial sbyte echo_char(sbyte value);");
    }

    [Test]
    public void AllowMarshalForStringFalseKeepsConstCharPointerAsPointerForParametersAndReturns()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
EXPORT_API void raw_parameter(const char* text);
EXPORT_API const char* raw_return(void);
"), options => options.AllowMarshalForString = false);

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public static partial void raw_parameter(byte* text);",
            "public static partial byte* raw_return();");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(text, "LPUTF8Str");
    }

    [Test]
    public void DisableRuntimeMarshallingRemovesStructBoolFieldMarshalAndDefaultCharset()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
struct NativeBool
{
    bool value;
};
", options => options.DisableRuntimeMarshalling = true);

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public partial struct NativeBool",
            "public bool value;");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(text,
            "MarshalAs(UnmanagedType.U1)]\n            public bool value;",
            "CharSet = CharSet.Ansi");
    }

    [Test]
    public void AutoByRefAndManualByRefMappingControlPointerParameters()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
struct Data { int value; };
EXPORT_API void update(int* value, const struct Data* input, struct Data* output);
"), options =>
        {
            options.MappingRules.Add(e => e.Map<CppParameter>("update::value").NoByRef());
            options.MappingRules.Add(e => e.Map<CppParameter>("update::output").ByRef(CSharpRefKind.Out));
        });

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public int value;",
            "public static partial void update(int* value, in libnative.Data input, out libnative.Data output);");
    }

    [Test]
    public void FixedArraysAndFunctionPointerParametersUseDeterministicShapes()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
typedef void (*callback_t)(int value);
struct Arrays
{
    int values[4];
    const char* names[2];
};
EXPORT_API void register_callback(callback_t callback);
"));

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public unsafe partial struct Arrays",
            "public fixed int values[4];",
            "public FixedArray2<nint> names;",
            "public readonly partial struct callback_t : IEquatable<callback_t>",
            "public delegate*unmanaged[Cdecl]<int, void> Value { get; }",
            "public static implicit operator delegate*unmanaged[Cdecl]<int, void> (callback_t from) => from.Value;",
            "public static partial void register_callback(libnative.callback_t callback);");
    }

    private static string ExportHeader(string declarations)
    {
        return @$"
#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility(""default"")))
#endif

{declarations}
";
    }
}

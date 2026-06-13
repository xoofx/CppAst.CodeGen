// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class ConverterStructEnumTypedefTests
{
    [Test]
    public void StructUnionBitfieldOpaqueExternAndGlobalFieldScenariosAreGeneratedDeterministically()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
int ignored_global;
const int CONST_VALUE = 42;
extern int external_counter;
struct Opaque;
struct Bits
{
    unsigned int low : 3;
    unsigned int high : 5;
};
union Overlap
{
    int i;
    float f;
};
EXPORT_API void take_opaque(struct Opaque* opaque);
"));

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public const int CONST_VALUE = 42;",
            "public static int external_counter => throw new NotImplementedException();",
            "public readonly partial record struct Opaque(nint Handle)",
            "public partial struct Bits",
            "private uint __bitfield__0;",
            "public uint low",
            "return unchecked((uint)((__bitfield__0 >> 0) & 0b111));",
            "public uint high",
            "return unchecked((uint)((__bitfield__0 >> 3) & 0b11111));",
            "[global::System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]",
            "[FieldOffset(0)]\n            public int i;",
            "[FieldOffset(0)]\n            public float f;",
            "public static partial void take_opaque(libnative.Opaque opaque);");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(text, "ignored_global");
    }

    [Test]
    public void TypedefWrapNoWrapForceAndDisableStructWrapOptionsControlGeneratedShape()
    {
        var source = ExportHeader(@"
typedef int wrapped_int;
typedef int plain_int;
typedef int forced_int;
struct Data { int value; };
typedef struct Data DataAlias;
EXPORT_API wrapped_int use_wrapped(wrapped_int value);
EXPORT_API plain_int use_plain(plain_int value);
EXPORT_API forced_int use_forced(forced_int value);
EXPORT_API void consume_alias(DataAlias value);
");

        var defaultText = GeneratedCodeTestHelper.GenerateSingleFile(source);
        GeneratedCodeTestHelper.AssertContainsAll(defaultText,
            "public readonly partial record struct wrapped_int(int Value)",
            "public readonly partial record struct plain_int(int Value)",
            "public readonly partial record struct forced_int(int Value)",
            "public readonly partial record struct DataAlias(libnative.Data Value)",
            "public static partial libnative.wrapped_int use_wrapped(libnative.wrapped_int value);",
            "public static partial void consume_alias(libnative.DataAlias value)");

        var noWrapText = GeneratedCodeTestHelper.GenerateSingleFile(source, options =>
        {
            options.TypedefCodeGenKind = CppTypedefCodeGenKind.NoWrap;
            options.TypedefWrapForceList.Add("forced_int");
            options.DisableTypedefToStructWrap = true;
        });
        GeneratedCodeTestHelper.AssertContainsAll(noWrapText,
            "public readonly partial record struct forced_int(int Value)",
            "public static partial int use_wrapped(int value);",
            "public static partial int use_plain(int value);",
            "public static partial libnative.forced_int use_forced(libnative.forced_int value);",
            "public static partial void consume_alias(libnative.Data value);");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(noWrapText,
            "public readonly partial record struct wrapped_int",
            "public readonly partial record struct plain_int",
            "public readonly partial record struct DataAlias");
    }

    [Test]
    public void EnumFlagsGeneratedFieldsAndAnonymousEnumsFollowOptions()
    {
        var source = @"
enum Flags
{
    FLAG_NONE = 0,
    FLAG_READ = 1 << 0,
    FLAG_WRITE = 1 << 1,
};

enum
{
    ANON_A = 10,
    ANON_B = 11,
};
";

        var defaultText = GeneratedCodeTestHelper.GenerateSingleFile(source);
        GeneratedCodeTestHelper.AssertContainsAll(defaultText,
            "[Flags]\n        public enum Flags : int",
            "FLAG_NONE = unchecked((int)0)",
            "FLAG_READ = unchecked((int)1)",
            "FLAG_WRITE = unchecked((int)2)",
            "public const libnative.Flags FLAG_READ = Flags.FLAG_READ;");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(defaultText,
            "ANON_A",
            "ANON_B");

        var noFieldText = GeneratedCodeTestHelper.GenerateSingleFile(source, options => options.GenerateEnumItemAsFields = false);
        GeneratedCodeTestHelper.AssertContainsAll(noFieldText,
            "[Flags]\n        public enum Flags : int",
            "FLAG_READ = unchecked((int)1)");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(noFieldText,
            "public const libnative.Flags FLAG_READ = Flags.FLAG_READ;",
            "ANON_A");
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

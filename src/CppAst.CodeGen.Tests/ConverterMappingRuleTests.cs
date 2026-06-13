// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class ConverterMappingRuleTests
{
    [Test]
    public void ElementMappingRulesRenameRetypeDefaultMarshalHideDiscardAndMapTypes()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader(@"
typedef int Remapped;
EXPORT_API int rename_me(int, int count, bool flag);
EXPORT_API int hidden(int value);
EXPORT_API int discarded(int value);
EXPORT_API void use_remapped(Remapped value);
"), options =>
        {
            options.MappingRules.Add(e => e.Map<CppFunction>("rename_(.*)").Name("renamed_$1").Type("unsigned int"));
            options.MappingRules.Add(e => e.Map<CppParameter>("renamed_me::arg0").Type("char*"));
            options.MappingRules.Add(e => e.Map<CppParameter>("renamed_me::count").InitValue("42"));
            options.MappingRules.Add(e => e.Map<CppParameter>("renamed_me::flag").MarshalAs(UnmanagedType.I1));
            options.MappingRules.Add(e => e.Map<CppFunction>("hidden").Private());
            options.MappingRules.Add(e => e.Map<CppFunction>("discarded").Discard());
            options.MappingRules.Add(e => e.MapType("Remapped", "nint"));
        });

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public static partial uint renamed_me(byte* arg0, int count = 42, [global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.I1)] bool flag);",
            "private static partial int hidden(int value);",
            "public static partial void use_remapped(nint value);");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(text,
            "discarded",
            "MarshalAs(UnmanagedType.U1)] bool flag");
    }

    [Test]
    public void MacroMappingRulesSupportConstEnumCastsOverridesAndReferences()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
#define BASE_VALUE 4
#define ALIAS_VALUE BASE_VALUE
#define OVERRIDE_VALUE 123
#define MODE_READ 1
#define MODE_WRITE 2
", options =>
        {
            options.MappingRules.Add(e => e.MapMacroToConst("BASE_(.*)", "int", explicitCast: true, enumItemName: "BASE_$1_FIELD"));
            options.MappingRules.Add(e => e.MapMacroToConst("ALIAS_(.*)", "int"));
            options.MappingRules.Add(e => e.MapMacroToConst("OVERRIDE_VALUE", "int", overrideValue: "999"));
            options.MappingRules.Add(e => e.MapMacroToEnum("MODE_(.*)", "Mode", "Mode_$1", integerType: "unsigned int", explicitCast: true));
        });

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "public enum Mode : uint",
            "Mode_READ = unchecked((uint)1)",
            "Mode_WRITE = unchecked((uint)2)",
            "public const int BASE_VALUE_FIELD = 4;",
            "public const int ALIAS_VALUE = 4;",
            "public const int OVERRIDE_VALUE = 999;");
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

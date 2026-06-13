// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using CppAst.CodeGen.CSharp;
using Zio;

namespace CppAst.CodeGen.Tests;

public class ConverterInteropTests
{
    [Test]
    public void DefaultLibraryImportUsesConfiguredNamespaceClassOutputPathVisibilityAndCallingConvention()
    {
        var result = GeneratedCodeTestHelper.Generate(ExportHeader("bool do_work(const char* name);"), options =>
        {
            options.DefaultNamespace = "Custom.Native";
            options.DefaultClassLib = "NativeMethods";
            options.DefaultOutputFilePath = (UPath)"/custom/native.cs";
            options.DefaultDllImportNameAndArguments = "\"custom\"";
            options.GenerateAsInternal = true;
            options.DefaultCallingConvention = CallingConvention.StdCall;
        });
        var text = result.ReadAllText((UPath)"/custom/native.cs");

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "namespace Custom.Native",
            "internal static unsafe partial class NativeMethods",
            "[global::System.Runtime.InteropServices.LibraryImport(\"custom\", EntryPoint = \"do_work\")]",
            "[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]",
            "[return:global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)]",
            "public static partial bool do_work([global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPUTF8Str)] string name);");
    }

    [Test]
    public void DllImportModeEmitsExternMethodAndDllImportAttribute()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader("int do_work(int value);"), options =>
        {
            options.UseLibraryImport = false;
            options.DefaultDllImportNameAndArguments = "\"legacy\"";
            options.DefaultCallingConvention = CallingConvention.FastCall;
        });

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "[global::System.Runtime.InteropServices.DllImport(\"legacy\", CallingConvention = CallingConvention.FastCall)]",
            "public static extern int do_work(int value);");
        GeneratedCodeTestHelper.AssertDoesNotContainAny(text, "LibraryImport(\"legacy\"");
    }

    [Test]
    public void RequiredUsingsAreInsertedAndDeduplicatedForInteropAttributes()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(ExportHeader("bool first(bool value);\nEXPORT_API bool second(bool value);"));

        Assert.AreEqual(2, CountOccurrences(text, "using System.Runtime.InteropServices;")); // top-level plus class-local using
        Assert.AreEqual(1, CountOccurrences(text, "using System.Runtime.CompilerServices;"));
        Assert.AreEqual(2, CountOccurrences(text, "[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]"));
    }

    private static string ExportHeader(string declarations)
    {
        return @$"
#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility(""default"")))
#endif

EXPORT_API {declarations}
";
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}

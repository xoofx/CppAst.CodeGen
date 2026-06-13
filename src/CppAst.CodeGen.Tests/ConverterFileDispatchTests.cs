// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.IO;
using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class ConverterFileDispatchTests
{
    [Test]
    public void DispatchOutputPerIncludeWritesDeclarationsToTheirIncludeOutputFiles()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "CppAstCodeGenTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var firstHeader = Path.Combine(tempDirectory, "first.h");
            var secondHeader = Path.Combine(tempDirectory, "nested", "second.h");
            Directory.CreateDirectory(Path.GetDirectoryName(secondHeader)!);
            File.WriteAllText(firstHeader, ExportHeader(@"
struct FirstStruct { int value; };
EXPORT_API int first_function(int value);
"));
            File.WriteAllText(secondHeader, ExportHeader(@"
struct SecondStruct { int value; };
EXPORT_API int second_function(int value);
"));

            var result = GeneratedCodeTestHelper.Generate(new[] { firstHeader, secondHeader }, options =>
            {
                options.DispatchOutputPerInclude = true;
                options.IncludeFolders.Add(tempDirectory);
                options.DefaultOutputFilePath = "/generated/All.cs";
            });

            Assert.True(result.Outputs.ContainsKey("/first.generated.cs"), string.Join(", ", result.Outputs.Keys));
            Assert.True(result.Outputs.ContainsKey("/nested/second.generated.cs"), string.Join(", ", result.Outputs.Keys));

            var firstText = result.Outputs["/first.generated.cs"];
            var secondText = result.Outputs["/nested/second.generated.cs"];

            GeneratedCodeTestHelper.AssertContainsAll(firstText,
                "public partial struct FirstStruct",
                "public static partial int first_function(int value);");
            GeneratedCodeTestHelper.AssertDoesNotContainAny(firstText,
                "SecondStruct",
                "second_function");

            GeneratedCodeTestHelper.AssertContainsAll(secondText,
                "public partial struct SecondStruct",
                "public static partial int second_function(int value);");
            GeneratedCodeTestHelper.AssertDoesNotContainAny(secondText,
                "FirstStruct",
                "first_function");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
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

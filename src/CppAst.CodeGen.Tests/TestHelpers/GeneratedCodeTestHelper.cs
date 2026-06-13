// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using Zio;
using Zio.FileSystems;

namespace CppAst.CodeGen.Tests;

internal static class GeneratedCodeTestHelper
{
    public static GeneratedCodeResult Generate(string source, Action<CSharpConverterOptions>? configure = null)
    {
        var options = CreateOptions(configure);
        var compilation = CSharpConverter.Convert(source, options);
        return Dump(compilation, options);
    }

    public static GeneratedCodeResult Generate(IReadOnlyList<string> files, Action<CSharpConverterOptions>? configure = null)
    {
        var options = CreateOptions(configure);
        var compilation = CSharpConverter.Convert(files.ToList(), options);
        return Dump(compilation, options);
    }

    public static string GenerateSingleFile(string source, Action<CSharpConverterOptions>? configure = null)
    {
        var result = Generate(source, configure);
        return result.ReadAllText(result.Options.DefaultOutputFilePath);
    }

    public static string DumpToString(CSharpElement element, CodeWriterMode mode = CodeWriterMode.Full)
    {
        var writer = CreateWriter(mode: mode);
        element.DumpTo(writer);
        return NormalizeLineEndings(writer.ToString());
    }

    public static CodeWriter CreateWriter(MemoryFileSystem? fileSystem = null, CodeWriterMode mode = CodeWriterMode.Full)
    {
        return new CodeWriter(new CodeWriterOptions(fileSystem ?? new MemoryFileSystem(), mode) { NewLine = "\n" });
    }

    public static string NormalizeLineEndings(string text)
    {
        if (text.Length > 0 && text[0] == '\ufeff')
        {
            text = text.Substring(1);
        }

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static void AssertContainsAll(string text, params string[] expectedFragments)
    {
        foreach (var expected in expectedFragments)
        {
            StringAssert.Contains(expected, text, $"Expected generated text to contain:{Environment.NewLine}{expected}{Environment.NewLine}Actual text:{Environment.NewLine}{text}");
        }
    }

    public static void AssertDoesNotContainAny(string text, params string[] unexpectedFragments)
    {
        foreach (var unexpected in unexpectedFragments)
        {
            StringAssert.DoesNotContain(unexpected, text, $"Expected generated text not to contain:{Environment.NewLine}{unexpected}{Environment.NewLine}Actual text:{Environment.NewLine}{text}");
        }
    }

    private static CSharpConverterOptions CreateOptions(Action<CSharpConverterOptions>? configure)
    {
        var options = new CSharpConverterOptions();
        configure?.Invoke(options);
        return options;
    }

    private static GeneratedCodeResult Dump(CSharpCompilation? compilation, CSharpConverterOptions options)
    {
        Assert.NotNull(compilation, "The converter returned a null compilation.");
        Assert.False(compilation!.HasErrors, compilation.Diagnostics.ToString());

        var fileSystem = new MemoryFileSystem();
        var writer = CreateWriter(fileSystem);
        compilation.DumpTo(writer);
        return new GeneratedCodeResult(compilation, options, fileSystem);
    }
}

internal sealed class GeneratedCodeResult
{
    public GeneratedCodeResult(CSharpCompilation compilation, CSharpConverterOptions options, MemoryFileSystem fileSystem)
    {
        Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public CSharpCompilation Compilation { get; }

    public CSharpConverterOptions Options { get; }

    public MemoryFileSystem FileSystem { get; }

    public string ReadAllText(UPath path)
    {
        Assert.True(FileSystem.FileExists(path), $"Expected generated file `{path}` to exist. Actual files: {string.Join(", ", Outputs.Keys)}");
        return GeneratedCodeTestHelper.NormalizeLineEndings(FileSystem.ReadAllText(path));
    }

    public IReadOnlyDictionary<string, string> Outputs => FileSystem
        .EnumerateFiles(UPath.Root, "*", SearchOption.AllDirectories)
        .OrderBy(path => path.FullName, StringComparer.Ordinal)
        .ToDictionary(path => path.FullName, path => GeneratedCodeTestHelper.NormalizeLineEndings(FileSystem.ReadAllText(path)), StringComparer.Ordinal);
}

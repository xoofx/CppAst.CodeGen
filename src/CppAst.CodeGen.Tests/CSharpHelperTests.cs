// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class CSharpHelperTests
{
    [TestCase("lower", CSharpCasingKind.Lower)]
    [TestCase("camelCase", CSharpCasingKind.Camel)]
    [TestCase("PascalCase", CSharpCasingKind.Pascal)]
    [TestCase("SCREAMING", CSharpCasingKind.Screaming)]
    [TestCase("camel_Snake", CSharpCasingKind.CamelSnake)]
    [TestCase("lower_snake", CSharpCasingKind.LowerSnake)]
    [TestCase("Pascal_Snake", CSharpCasingKind.PascalSnake)]
    [TestCase("SCREAMING_SNAKE", CSharpCasingKind.ScreamingSnake)]
    [TestCase("not-identifier", CSharpCasingKind.Undefined)]
    public void DetectsCommonCSharpCasingKinds(string name, CSharpCasingKind expected)
    {
        Assert.AreEqual(expected, CSharpHelper.GetCSharpCasingKind(name));
    }

    [Test]
    public void ConvertsPascalCaseAndEscapesKeywordsOnlyWhenNeeded()
    {
        Assert.AreEqual("Value", CSharpHelper.ToPascal("value"));
        Assert.AreEqual("Value", CSharpHelper.ToPascal("Value"));
        Assert.AreEqual(string.Empty, CSharpHelper.ToPascal(string.Empty));
        Assert.AreEqual("@class", CSharpHelper.EscapeName("class"));
        Assert.AreEqual("native_value", CSharpHelper.EscapeName("native_value"));
    }

    [Test]
    public void MapsCppCallingConventionsToInteropCallingConventions()
    {
        Assert.AreEqual(CallingConvention.Cdecl, CppCallingConvention.C.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.Cdecl, CppCallingConvention.Default.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.StdCall, CppCallingConvention.X86StdCall.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.FastCall, CppCallingConvention.X86FastCall.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.ThisCall, CppCallingConvention.X86ThisCall.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.Winapi, CppCallingConvention.Win64.GetCSharpCallingConvention());
        Assert.AreEqual(CallingConvention.Cdecl, CppCallingConvention.Invalid.GetCSharpCallingConvention());
    }

    [Test]
    public void MapsInteropCallingConventionsToUnmanagedCallConvTypeNames()
    {
        Assert.AreEqual("Cdecl", CallingConvention.Cdecl.GetUnmanagedCallConvType());
        Assert.AreEqual("Stdcall", CallingConvention.StdCall.GetUnmanagedCallConvType());
        Assert.AreEqual("Thiscall", CallingConvention.ThisCall.GetUnmanagedCallConvType());
        Assert.AreEqual("Fastcall", CallingConvention.FastCall.GetUnmanagedCallConvType());
        Assert.AreEqual("Winapi", CallingConvention.Winapi.GetUnmanagedCallConvType());
    }
}

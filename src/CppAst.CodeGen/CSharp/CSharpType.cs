// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;
using Zio.FileSystems;

namespace CppAst.CodeGen.CSharp;

public abstract class CSharpType : CSharpElement
{
    public abstract void DumpReferenceTo(CodeWriter writer);

    public string GetName()
    {
        var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Simple));
        DumpReferenceTo(writer);
        return writer.CurrentWriter!.ToString()!;
    }
}

/// <summary>
/// A member type is a TargetType.MemberType representation (a class within another class)
/// </summary>
public class CSharpTargetAndMemberType : CSharpType
{
    public CSharpTargetAndMemberType(CSharpType targetType, CSharpType memberType)
    {
        TargetType = targetType;
        MemberType = memberType;
    }

    public CSharpType TargetType { get; set; }

    public CSharpType MemberType { get; set; }


    public override void DumpTo(CodeWriter writer)
    {
        TargetType.DumpReferenceTo(writer);
        writer.Write(".");
        MemberType.DumpReferenceTo(writer);
    }

    public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
}
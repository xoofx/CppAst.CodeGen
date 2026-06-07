// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp;

/// <summary>
/// Used to only output the name of a type without its full path
/// </summary>
public class CSharpSimpleNameReferenceType : CSharpType
{
    public CSharpSimpleNameReferenceType(CSharpNamedType baseType)
    {
        BaseType = baseType;
    }

    public CSharpNamedType BaseType { get; set; }
    
    public override void DumpTo(CodeWriter writer) => writer.Write(BaseType.Name);

    public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
}
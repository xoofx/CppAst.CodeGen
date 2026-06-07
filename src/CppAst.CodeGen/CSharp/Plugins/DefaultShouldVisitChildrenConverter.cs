// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp;

public class DefaultShouldVisitChildrenConverter : ICSharpConverterPlugin
{
    public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
    {
        pipeline.ShouldVisitChildren.Add(ShouldVisitChildren);
    }

    protected virtual bool ShouldVisitChildren(CSharpConverter converter, CppElement element, CSharpElement? context)
    {
        return true;
    }
}
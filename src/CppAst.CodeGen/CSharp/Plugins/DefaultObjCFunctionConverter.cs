// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;

namespace CppAst.CodeGen.CSharp;

public class DefaultObjCFunctionConverter : ICSharpConverterPlugin
{
    /// <inheritdoc />
    public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
    {
        pipeline.FunctionConverters.Add(ConvertFunction);
    }

    protected virtual CSharpElement? ConvertFunction(CSharpConverter converter, CppFunction cppFunction, CSharpElement context)
    {
        if (!CppHelper.IsObjCFunction(cppFunction))
        {
            return null;
        }

        var cppParent = cppFunction.Parent as CppClass;

        // Register the struct as soon as possible
        var csFunction = new CSharpMethod(string.Empty)
        { 
            CppElement = cppFunction
        };
        
        var container = (ICSharpContainer)context;

        if (container is CSharpTypeWithMembers csTypeWithMembers)
        {
            container = (CSharpExtension)csTypeWithMembers.LinkedExtension!;
        }
        container.Members.Add(csFunction);

        if ((cppFunction.Flags & (CppFunctionFlags.Virtual | CppFunctionFlags.Method | CppFunctionFlags.ClassMethod)) == 0)
        {
            csFunction.Modifiers |= CSharpModifiers.Static | CSharpModifiers.Extern;
        }
        else if ((cppFunction.Flags & (CppFunctionFlags.ClassMethod)) != 0)
        {
            csFunction.Modifiers |= CSharpModifiers.Static;
        }
        csFunction.Visibility = CSharpVisibility.Public;

        // TODO: hack to allow GetCSharpName to rename the function
        CSharpElement parentCsFunction = csFunction;
        parentCsFunction = (CSharpElement)container;

        csFunction.Name = converter.GetCSharpName(cppFunction, parentCsFunction);
        csFunction.Comment = converter.GetCSharpComment(cppFunction, parentCsFunction);
        csFunction.ReturnType = converter.GetCSharpType(cppFunction.ReturnType, csFunction);

        csFunction.BodyInline = (writer, element) => writer.Write("throw new NotImplementedException()");

        return csFunction;
    }
}
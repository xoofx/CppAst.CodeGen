// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CSharpClass : CSharpTypeWithMembers
    {
        public CSharpClass(string name) : base(name)
        {
            Modifiers = CSharpModifiers.Partial;
        }
        protected override string DeclarationKind => "class";
    }
}
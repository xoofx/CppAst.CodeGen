// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CSharpStruct : CSharpTypeWithMembers
    {
        public CSharpStruct(string name) : base(name)
        {
            Modifiers = CSharpModifiers.Partial;
        }

        /// <inheritdoc />
        protected override string DeclarationKind => "struct";

        public bool IsOpaque => CppElement is CppClass cppClass && !cppClass.IsDefinition;
    }
}
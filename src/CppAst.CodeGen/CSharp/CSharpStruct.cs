﻿// Copyright (c) Alexandre Mutel. All rights reserved.
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

        public CSharpStructMarshallingUsage MarshallingUsage { get; set; }

        public bool IsOpaque
        {
            get
            {
                var cppElement = CppElement;

                while (cppElement is CppTypedef cppTypedef)
                {
                    cppElement = cppTypedef.ElementType;
                }

                return cppElement is CppClass cppClass && !cppClass.IsDefinition;
            }
        }
    }
}

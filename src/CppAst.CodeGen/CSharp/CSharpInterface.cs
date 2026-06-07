// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CSharpInterface : CSharpTypeWithMembers
    {
        public CSharpInterface(string name) : base(name)
        {
        }

        /// <summary>
        /// Gets or sets an associated object (used by ObjC to link interface/struct)
        /// </summary>
        public CSharpStruct? ObjCProtocolDefaultImpl { get; set; }


        protected override string DeclarationKind => "interface";
    }
}
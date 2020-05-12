// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpEnum : CSharpTypeWithMembers
    {
        public CSharpEnum(string name) : base(name)
        {
        }

        public CSharpType IntegerBaseType => BaseTypes.Count > 0 ? BaseTypes[0] : null;

        public bool IsFlags { get; set; }

        /// <inheritdoc />
        protected override string DeclarationKind => "enum";

        /// <inheritdoc />
        public override IEnumerable<CSharpAttribute> GetAttributes()
        {
            foreach (var attr in base.GetAttributes()) { yield return attr; }

            if (IsFlags)
            {
                yield return new CSharpFreeAttribute("Flags");
            }
        }
    }
}
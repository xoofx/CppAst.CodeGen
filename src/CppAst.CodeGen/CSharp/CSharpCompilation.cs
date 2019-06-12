// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Globalization;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpCompilation : CSharpElement, ICSharpContainer
    {
        public CSharpCompilation()
        {
            Diagnostics = new CppDiagnosticBag();
            Members = new CSharpContainerList<CSharpElement>(this);
        }

        ICSharpContainer ICSharpContainer.Parent => Parent as ICSharpContainer;

        public CppDiagnosticBag Diagnostics { get; }

        public bool HasErrors => Diagnostics.HasErrors;

        public CSharpContainerList<CSharpElement> Members { get; }

        void ICSharpContainer.ValidateMember(CSharpElement element)
        {
            if (!(element is CSharpGeneratedFile))
            {
                throw new ArgumentException("Only instance of CSharpGeneratedFile can be added to members of a CSharpCompilation");
            }
        }

        public override void DumpTo(CodeWriter writer)
        {
            if (writer.Mode == CodeWriterMode.Simple)
            {
                writer.Write("Count = ").Write(Members.Count.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                this.DumpMembersTo(writer);
            }
        }
    }
}
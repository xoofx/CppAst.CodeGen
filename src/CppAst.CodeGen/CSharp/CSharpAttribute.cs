// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public enum CSharpAttributeScope
    {
        None,
        Return,
        Assembly
    }

    public interface ICSharpAttributesProvider
    {
        IEnumerable<CSharpAttribute> GetAttributes();
    }

    public interface ICSharpContextualAttributesProvider
    {
        IEnumerable<CSharpAttribute> GetContextualAttributes();
    }

    public abstract class CSharpAttribute : CSharpElement
    {
        public CSharpAttributeScope Scope { get; set; }

        public abstract string ToText();

        public CSharpAttribute Clone()
        {
            return (CSharpAttribute)MemberwiseClone();
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            DumpTo(writer, Scope);
        }

        public void DumpTo(CodeWriter writer, CSharpAttributeScope scopeOverride)
        {
            if (scopeOverride != CSharpAttributeScope.None)
            {
                writer.Write(scopeOverride == CSharpAttributeScope.Return ? "return:" : "assembly:");
            }
            writer.Write(ToText());
        }
    }

    public class CSharpFreeAttribute : CSharpAttribute
    {
        public CSharpFreeAttribute(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public CSharpFreeAttribute(CSharpAttributeScope scope, string text)
        {
            Scope = scope;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public string Text { get; set; }

        /// <inheritdoc />
        public override string ToText() => Text;
    }
}
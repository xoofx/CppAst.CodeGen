// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpParamComment : CSharpXmlComment
    {
        private readonly CSharpXmlAttribute _nameAttribute;
        
        public CSharpParamComment(string name) : base("param")
        {
            _nameAttribute = new CSharpXmlAttribute("name", name ?? throw new ArgumentNullException(nameof(name)));
            Attributes.Add(_nameAttribute);
            IsInline = true;
        }

        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name
        {
            get => _nameAttribute.Value;
            set => _nameAttribute.Value = value;
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            base.DumpTo(writer);
            writer.WriteLine();
        }
    }
}

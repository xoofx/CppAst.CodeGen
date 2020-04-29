// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpField : CSharpElement, ICSharpWithComment, ICSharpAttributesProvider, ICSharpElementWithVisibility, ICSharpMember
    {
        public CSharpField(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Attributes = new List<CSharpAttribute>();
            Visibility = CSharpVisibility.Public;
        }

        /// <inheritdoc />
        public CSharpComment Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public CSharpType FieldType { get; set; }

        public string InitValue { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes() => Attributes;

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            if (writer.Mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            FieldType?.DumpContextualAttributesTo(writer);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);

            if (FieldType is CSharpFixedArrayType fixedArrayType)
            {
                writer.Write("fixed ");
                fixedArrayType.ElementType.DumpReferenceTo(writer);
                writer.Write(" ");
                writer.Write(Name);
                writer.Write("[").Write(fixedArrayType.Size.ToString(CultureInfo.InvariantCulture)).Write("]");
            }
            else
            {
                FieldType?.DumpReferenceTo(writer);
                writer.Write(" ");
                writer.Write(Name);
            }

            if (InitValue != null)
            {
                writer.Write(" = ");
                writer.Write(InitValue);
            }
            writer.Write(";");
            writer.WriteLine();
        }
    }

    public class CSharpBitField : CSharpField
    {
        public CSharpBitField(string name) : base(name)
        {
        }

        internal int CurrentBitWidth { get; set; }

        internal int MaxBitWidth { get; set; }
    }
}
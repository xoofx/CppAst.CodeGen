// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpParameter : CSharpElement, ICSharpAttributesProvider, ICSharpMember
    {
        public CSharpParameter()
        {
            Attributes = new List<CSharpAttribute>();
        }

        public CSharpParameter(string name) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public List<CSharpAttribute> Attributes { get; private set; }

        public int Index { get; set; }

        public CSharpType ParameterType { get; set; }

        public bool IsThis { get; set; }

        public bool IsParams { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        public string DefaultValue { get; set; }

        public CSharpParameter Clone()
        {
            var newParam = (CSharpParameter)MemberwiseClone();
            newParam.Attributes = new List<CSharpAttribute>();
            foreach (var attr in Attributes)
            {
                newParam.Attributes.Add(attr.Clone());
            }
            return newParam;
        }

        public void DumpArgTo(CodeWriter writer)
        {
            if (ParameterType is CSharpRefType refType)
            {
                switch (refType.Kind)
                {
                    case CSharpRefKind.Out:
                        writer.Write("out ");
                        break;
                    case CSharpRefKind.Ref:
                        writer.Write("ref ");
                        break;
                    case CSharpRefKind.RefReadOnly:
                        writer.Write("ref readonly ");
                        break;
                }
            }

            writer.Write(Name);
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            this.DumpAttributesTo(writer);
            ParameterType?.DumpContextualAttributesTo(writer, true);
            if (IsThis)
            {
                writer.Write("this ");
            }
            if (IsParams)
            {
                writer.Write("params ");
            }
            ParameterType?.DumpReferenceTo(writer);
            writer.Write(" ");
            writer.Write(Name);
            if (DefaultValue != null)
            {
                writer.Write(" = ");
                writer.Write(DefaultValue);
            }
        }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes() => Attributes;
    }
}
// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public static class CSharpElementExtensions
    {
        public static void DumpMembersTo(this ICSharpContainer element, CodeWriter writer)
        {
            var members = element.Members;
            for (var i = 0; i < members.Count; i++)
            {
                var elementMember = members[i];
                if (i > 0) { writer.WriteLine(); }
                elementMember.DumpTo(writer);
            }
        }

        public static void DumpTo(this CSharpRefKind refKind, CodeWriter writer)
        {
            switch (refKind)
            {
                case CSharpRefKind.None:
                    break;
                case CSharpRefKind.In:
                    writer.Write("in ");
                    break;
                case CSharpRefKind.Out:
                    writer.Write("out ");
                    break;
                case CSharpRefKind.Ref:
                    writer.Write("ref ");
                    break;
                case CSharpRefKind.RefReadOnly:
                    writer.Write("ref readonly ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null);
            }
        }

        public static void DumpTo(this CSharpVisibility visibility, CodeWriter writer)
        {
            switch (visibility)
            {
                case CSharpVisibility.None:
                    break;
                case CSharpVisibility.Public:
                    writer.Write("public ");
                    break;
                case CSharpVisibility.Private:
                    writer.Write("private ");
                    break;
                case CSharpVisibility.Protected:
                    writer.Write("protected ");
                    break;
                case CSharpVisibility.Internal:
                    writer.Write("internal ");
                    break;
                case CSharpVisibility.ProtectedInternal:
                    writer.Write("protected internal ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
            }
        }

        public static void DumpTo(this CSharpModifiers modifiers, CodeWriter writer)
        {
            if ((modifiers & CSharpModifiers.Const) != 0)
            {
                writer.Write("const ");
                return;
            }

            if ((modifiers & CSharpModifiers.Static) != 0)
            {
                writer.Write("static ");
            }

            if ((modifiers & CSharpModifiers.Abstract) != 0)
            {
                writer.Write("abstract ");
            }

            if ((modifiers & CSharpModifiers.Unsafe) != 0)
            {
                writer.Write("unsafe ");
            }

            if ((modifiers & CSharpModifiers.ReadOnly) != 0)
            {
                writer.Write("readonly ");
            }

            if ((modifiers & CSharpModifiers.Extern) != 0)
            {
                writer.Write("extern ");
            }

            if ((modifiers & CSharpModifiers.Partial) != 0)
            {
                writer.Write("partial ");
            }
        }

        public static void DumpAttributesTo(this ICSharpAttributesProvider element, CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Simple) return;

            foreach (var attr in element.GetAttributes())
            {
                writer.Write("[");
                attr.DumpTo(writer);
                writer.Write("]");
                writer.WriteLine();
            }
        }

        public static void DumpContextualAttributesTo(this CSharpElement element, CodeWriter writer, bool inline = false, CSharpAttributeScope? scopeOverride = null)
        {
            if (!(element is ICSharpContextualAttributesProvider provider))
            {
                return;
            }

            var mode = writer.Mode;

            if (mode == CodeWriterMode.Simple) { return; }

            if (inline)
            {
                bool isFirst = true;

                foreach (var attr in provider.GetContextualAttributes())
                {
                    if (isFirst)
                    {
                        writer.Write("[");
                        isFirst = false;
                    }
                    else
                    {
                        writer.Write("] [");
                    }

                    if (scopeOverride.HasValue)
                    {
                        attr.DumpTo(writer, scopeOverride.Value);
                    }
                    else
                    {
                        attr.DumpTo(writer);
                    }

                    writer.Write("] ");
                }
            }
            else
            {
                foreach (var attr in provider.GetContextualAttributes())
                {
                    writer.Write("[");
                    if (scopeOverride.HasValue)
                    {
                        attr.DumpTo(writer, scopeOverride.Value);
                    }
                    else
                    {
                        attr.DumpTo(writer);
                    }
                    writer.Write("]");
                    writer.WriteLine();
                }
            }
        }

        public static void DumpTo(this List<CSharpParameter> parameters, CodeWriter writer)
        {
            writer.Write("(");
            for (var i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                if (i > 0) { writer.Write(", "); }
                param.DumpTo(writer);
            }

            writer.Write(")");
        }
    }
}
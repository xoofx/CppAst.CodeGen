// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpMethod : CSharpElement, ICSharpAttributesProvider, ICSharpElementWithVisibility
    {
        public CSharpMethod()
        {
            Attributes = new List<CSharpAttribute>();
            Parameters = new List<CSharpParameter>();
            Visibility = CSharpVisibility.Public;
        }

        public CSharpComment Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public CSharpType ReturnType { get; set; }

        public bool IsConstructor { get; set; }

        public string Name { get; set; }

        public List<CSharpParameter> Parameters { get; }

        public Action<CodeWriter, CSharpElement> Body { get; set; }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes()
        {
            return Attributes;
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            ReturnType?.DumpContextualAttributesTo(writer, false, CSharpAttributeScope.Return);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);
            if (IsConstructor)
            {
                writer.Write((Parent as CSharpNamedType)?.Name);
            }
            else
            {
                ReturnType?.DumpReferenceTo(writer);
                writer.Write(" ");
                writer.Write(Name);
            }
            Parameters.DumpTo(writer);

            if (Body != null)
            {
                if (mode == CodeWriterMode.Full)
                {
                    writer.WriteLine();
                    writer.OpenBraceBlock();
                    Body?.Invoke(writer, this);
                    writer.CloseBraceBlock();
                }
                else
                {
                    writer.WriteLine(" { ... }");
                }
            }
            else
            {
                writer.WriteLine(";");
            }
        }
    }

    public static class CSharpMethodExtensions
    {
        public static CSharpMethod Wrap(this CSharpMethod csMethod)
        {
            var dllImport = csMethod.Attributes.OfType<CSharpDllImportAttribute>().FirstOrDefault();

            // Create a new method
            var clonedMethod = new CSharpMethod
            {
                Name = csMethod.Name,
                ReturnType = csMethod.ReturnType,
                Modifiers = csMethod.Modifiers,
                Comment = csMethod.Comment,
            };

            // Remove the comment from the private method now
            for (int i = 0; i < csMethod.Parameters.Count; i++)
            {
                var fromParam = csMethod.Parameters[i];
                var clonedParam = fromParam.Clone();
                clonedParam.Parent = clonedMethod;
                clonedMethod.Parameters.Add(clonedParam);
            }

            // If original function has a DllImport, update its EntryPoint
            // as we are going to change its name after
            if (dllImport != null)
            {
                // Remove extern
                clonedMethod.Modifiers ^= CSharpModifiers.Extern;
                dllImport.EntryPoint = $"\"{clonedMethod.Name}\"";
            }

            // Remove the comment from the original method
            csMethod.Comment = null;
            // Rename it to avoid naming clash
            csMethod.Name += "__";
            // Make it private
            csMethod.Visibility = CSharpVisibility.Private;

            // Insert the new function right before
            var members = ((ICSharpContainer)csMethod.Parent).Members;
            int index = members.IndexOf(csMethod);
            members.Insert(index, clonedMethod);


            return clonedMethod;
        }
    }
}
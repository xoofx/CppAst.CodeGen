﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public enum CSharpMethodKind
    {
        Method,
        Constructor,
        Destructor,
        Operator,
        Conversion,
    }


    public class CSharpMethod : CSharpElement, ICSharpWithComment, ICSharpAttributesProvider, ICSharpElementWithVisibility, ICSharpMember
    {
        public CSharpMethod(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Attributes = new List<CSharpAttribute>();
            Parameters = new List<CSharpParameter>();
            Visibility = CSharpVisibility.Public;
        }

        public CSharpComment? Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public CSharpType? ReturnType { get; set; }

        public CSharpMethodKind Kind { get; set; }

        public string Name { get; set; }

        public List<CSharpParameter> Parameters { get; }

        public bool IsManaged { get; set; }

        public Action<CodeWriter, CSharpElement>? Body { get; set; }

        public Action<CodeWriter, CSharpElement>? BodyInline { get; set; }
        
        public CSharpMethod Clone()
        {
            var newMethod = new CSharpMethod(Name)
            {
                CppElement = CppElement,
                Comment = Comment,
                Visibility = Visibility,
                Modifiers = Modifiers,
                ReturnType = ReturnType,
                Kind = Kind,
            };

            foreach (var attribute in Attributes)
            {
                newMethod.Attributes.Add(attribute.Clone());
            }

            foreach (var parameter in Parameters)
            {
                newMethod.Parameters.Add(parameter.Clone());
            }

            return newMethod;
        }
        
        /// <summary>
        /// Creates a function pointer that is matching the signature of the method.
        /// </summary>
        /// <returns></returns>
        public CSharpFunctionPointer? ToFunctionPointer()
        {
            if (ReturnType == null)
            {
                return null;
            }
            var functionPointer = new CSharpFunctionPointer(ReturnType);
            foreach (var parameter in Parameters)
            {
                if (parameter.ParameterType != null)
                    functionPointer.Parameters.Add(parameter.Clone());
            }
            return functionPointer;
        }

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
            if (Kind == CSharpMethodKind.Constructor)
            {
                writer.Write(((CSharpNamedType)Parent!).Name);
            }
            else
            {
                if (Kind == CSharpMethodKind.Operator)
                {
                    writer.Write("operator ");
                }
                ReturnType?.DumpReferenceTo(writer);
                writer.Write(" ");
                writer.Write(Name ?? string.Empty);
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
                if (BodyInline != null)
                {
                    writer.Write(" => ");
                    BodyInline?.Invoke(writer, this);
                }

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
            var clonedMethod = new CSharpMethod(csMethod.Name)
            {
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
            var members = ((ICSharpContainer)csMethod.Parent!).Members;
            int index = members.IndexOf(csMethod);
            members.Insert(index, clonedMethod);


            return clonedMethod;
        }
    }
}

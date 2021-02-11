// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;
using Zio.FileSystems;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpComment : CSharpElement
    {
        protected CSharpComment()
        {
            Children = new List<CSharpComment>();
        }

        public List<CSharpComment> Children { get; private set; }

        public virtual CSharpComment Clone()
        {
            var copy = (CSharpComment)MemberwiseClone();
            copy.Children = new List<CSharpComment>();
            foreach (var child in Children)
            {
                copy.Children.Add(child.Clone());
            }
            return copy;
        }

        public string ChildrenToFullString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Full));
            DumpChildrenTo(writer);
            return writer.CurrentWriter.ToString();
        }

        protected internal void DumpChildrenTo(CodeWriter writer)
        {
            foreach (var children in Children)
            {
                children.DumpTo(writer);
            }
        }
    }
}
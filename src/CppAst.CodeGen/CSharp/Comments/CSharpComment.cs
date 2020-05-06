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

        public List<CSharpComment> Children { get; }

        public string ChildrenToFullString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem()));
            DumpChildrenTo(writer);
            return writer.CurrentWriter.ToString();
        }

        protected internal void DumpChildrenTo(CodeWriter writer, bool inline = true)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                Children[i].DumpTo(writer);

                if (!inline && i != Children.Count - 1)
                {
                    writer.WriteLine();
                }
            }
        }
    }
}
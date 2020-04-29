// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using Zio;
using Zio.FileSystems;

namespace CppAst.CodeGen.Common
{
    public class CodeWriterOptions
    {
        public CodeWriterOptions() : this(GetDefaultFileSystem())
        {
        }

        public CodeWriterOptions(IFileSystem fileSystem, CodeWriterMode mode = CodeWriterMode.Full)
        {
            FileSystem = fileSystem;
            Mode = mode;
            IndentSize = 4;
            NewLine = Environment.NewLine;
            Tags = new Dictionary<string, object>();
        }

        public CodeWriterMode Mode { get; set; }

        public int IndentSize { get; set; }

        public string NewLine { get; set; }

        public IFileSystem FileSystem { get; }

        public Dictionary<string, object> Tags { get; }

        public object this[string tagName]
        {
            get
            {
                Tags.TryGetValue(tagName, out var obj);
                return obj;
            }
            set
            {
                Tags[tagName] = value;
            }
        }

        private static IFileSystem GetDefaultFileSystem()
        {
            var fs = new PhysicalFileSystem();
            var subfs = new SubFileSystem(fs, fs.ConvertPathFromInternal(Environment.CurrentDirectory));
            return subfs;
        }
    }
}
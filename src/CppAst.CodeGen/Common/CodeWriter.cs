// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Zio;

namespace CppAst.CodeGen.Common
{
    public class CodeWriter
    {
        private readonly Stack<WriterOutput> _backendWriters;
        private int _indentLevel;
        private bool _hasNewLine;
        private readonly List<string> _prefixes;

        public CodeWriter(CodeWriterOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _backendWriters = new Stack<WriterOutput>();
            _prefixes = new List<string>();
            _hasNewLine = true;
            _backendWriters.Push(new WriterOutput(new StringWriter(), false));
        }

        public CodeWriterOptions Options { get; }

        public CodeWriterMode Mode => Options.Mode;

        public TextWriter CurrentWriter => _backendWriters.Count > 0 ? _backendWriters.Peek().Writer : null;

        public virtual void PushOutput(TextWriter writer, bool autoDispose = false)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            _backendWriters.Push(new WriterOutput(writer, autoDispose));
        }

        public virtual void PushFileOutput(UPath path, bool autoDispose = true)
        {
            if (path.IsNull || path.IsEmpty) throw new ArgumentNullException(nameof(path));

            var fileSystem = Options.FileSystem;
            if (fileSystem == null)
            {
                throw new InvalidOperationException($"The `{nameof(CodeWriterOptions)}.{nameof(CodeWriterOptions.FileSystem)}` is null but must be setup in order to use `{nameof(PushFileOutput)}`");
            }

            var filePath = UPath.Combine(UPath.Root, path);
            var directory = filePath.GetDirectory();
            if (!directory.IsEmpty && !fileSystem.DirectoryExists(directory))
            {
                fileSystem.CreateDirectory(directory);
            }
            var finalWriter = new StreamWriter(new BufferedStream(fileSystem.CreateFile(filePath)));
            PushOutput(finalWriter, autoDispose);
        }

        public virtual TextWriter PopOutput()
        {
            var writerOutput = _backendWriters.Pop();
            if (writerOutput.AutoDispose)
            {
                writerOutput.Writer.Dispose();
            }

            return writerOutput.Writer;
        }

        public void Indent()
        {
            _indentLevel++;
        }

        public void PushPrefix(string prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            _prefixes.Add(prefix);
        }

        public void PopPrefix()
        {
            if (_prefixes.Count == 0) throw new InvalidOperationException("Cannot pop prefix more than push prefix");
            _prefixes.RemoveAt(_prefixes.Count - 1);
        }

        public void UnIndent()
        {
            if (_indentLevel == 0) throw new InvalidOperationException("Cannot un-indent more than indent");
            _indentLevel--;
        }

        public void OpenBraceBlock()
        {
            WriteLine("{");
            Indent();
        }

        public void CloseBraceBlock()
        {
            UnIndent();
            WriteLine("}");
        }

        public CodeWriter Write(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var currentWriter = CurrentWriter;
            if (currentWriter == null)
            {
                throw new InvalidOperationException($"The `{nameof(CurrentWriter)}` of this instance cannot be null. You must call `{nameof(PushOutput)}` before writing to this instance.");
            }

            var firstEndOfLine = text.IndexOf('\n');
            var lastEndOfLine = text.LastIndexOf('\n');
            var hasTrailingEndOfLine = text.EndsWith("\n");
            var hasMultiLine = firstEndOfLine != lastEndOfLine || (firstEndOfLine >= 0 && !hasTrailingEndOfLine);

            if (hasMultiLine)
            {
                var startIndex = 0;
                var isNextNewLine = true;
                var nextStartIndex = firstEndOfLine < 0 ? text.Length : firstEndOfLine + 1;

                while (true)
                {
                    if (_hasNewLine)
                    {
                        WriteIndentAndPrefix();
                        _hasNewLine = false;
                    }
                    _hasNewLine = isNextNewLine;

                    var subText = NormalizeLine(text.Substring(startIndex, nextStartIndex - startIndex));

                    currentWriter.Write(subText);
                    startIndex = nextStartIndex;

                    if (startIndex >= text.Length)
                    {
                        break;
                    }

                    nextStartIndex = text.IndexOf('\n', startIndex);
                    if (nextStartIndex < 0)
                    {
                        isNextNewLine = false;
                        nextStartIndex = text.Length;
                    }
                    else
                    {
                        isNextNewLine = true;
                        nextStartIndex += 1;
                    }
                }
            }
            else
            {
                var normalizedLineText = NormalizeLine(text);

                if (_hasNewLine)
                {
                    if (!string.IsNullOrWhiteSpace(normalizedLineText))
                    {
                        WriteIndent();
                    }

                    WritePrefix();
                    _hasNewLine = false;
                }

                currentWriter.Write(normalizedLineText);
            }

            _hasNewLine = hasTrailingEndOfLine;
            return this;
        }

        private string NormalizeLine(string text)
        {
            // Make sure that we are only using our NewLine and not the one provided
            if (text.EndsWith("\n"))
            {
                text = text.TrimEnd('\r', '\n');
                return $"{text}{(Options.NewLine ?? "\n")}";
            }

            return text;
        }

        private void WriteIndentAndPrefix()
        {
            WriteIndent();
            WritePrefix();
        }

        private void WriteIndent()
        {
            var currentWriter = CurrentWriter;
            if (currentWriter == null)
            {
                throw new InvalidOperationException($"The {nameof(CurrentWriter)} of this instance cannot be null");
            }

            var indentSize = Options.IndentSize;

            for (int i = 0; i < _indentLevel; i++)
            {
                for (int j = 0; j < indentSize; j++)
                {
                    currentWriter.Write(" ");
                }
            }
        }

        private void WritePrefix()
        {
            var currentWriter = CurrentWriter;

            if (currentWriter == null)
            {
                throw new InvalidOperationException($"The {nameof(CurrentWriter)} of this instance cannot be null");
            }

            // Print all prefixes after indent
            foreach (var prefix in _prefixes)
            {
                currentWriter.Write(prefix);
            }
        }

        public CodeWriter WriteLine(string text)
        {
            Write(text);
            WriteLine();
            return this;
        }

        public CodeWriter WriteLine()
        {
            Write(Options.NewLine ?? "\n");
            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return CurrentWriter != null ? CurrentWriter.ToString() : string.Empty;
        }

        private readonly struct WriterOutput
        {
            public WriterOutput(TextWriter writer, bool autoDispose)
            {
                Writer = writer;
                AutoDispose = autoDispose;
            }

            public readonly TextWriter Writer;

            public readonly bool AutoDispose;
        }
    }
}
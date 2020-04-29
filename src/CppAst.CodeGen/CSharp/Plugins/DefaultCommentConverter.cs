
// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultCommentConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.CommentConverters.Add(ConvertComment);
        }

        public static CSharpComment ConvertComment(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            if (!(element is ICppDeclaration cppDecl))
            {
                return null;
            }

            var comment = cppDecl.Comment;
            if (comment?.Children == null)
            {
                return null;
            }

            var csFullComment = new CSharpFullComment();

            CSharpXmlComment csRemarks = null;

            for (var i = 0; i < comment.Children.Count; i++)
            {
                var childComment = comment.Children[i];
                if (i == 0)
                {
                    var summary = new CSharpXmlComment("summary");
                    summary.Children.Add(GetAsCSharpComment(childComment));
                    csFullComment.Children.Add(summary);
                    continue;
                }

                switch (childComment.Kind)
                {
                    case CppCommentKind.ParamCommand:
                        var paramComment = (CppCommentParamCommand)childComment;

                        var csParamComment = new CSharpParamComment(paramComment.ParamName);
                        csParamComment.Children.Add(GetChildAsCSharpComment(paramComment));
                        csFullComment.Children.Add(csParamComment);
                        break;

                    case CppCommentKind.BlockCommand:
                        var blockCommand = (CppCommentBlockCommand)childComment;
                        if (blockCommand.CommandName == "return")
                        {
                            var returnComment = new CSharpReturnComment();
                            returnComment.Children.Add(GetChildAsCSharpComment(blockCommand));
                            csFullComment.Children.Add(returnComment);
                        }
                        else if (blockCommand.CommandName == "see")
                        {
                            var seealso = new CSharpXmlComment("seealso") { IsSelfClosing = true };
                            seealso.Attributes.Add(new CSharpXmlAttribute("cref", GetChildAsText(childComment)));
                            csFullComment.Children.Add(seealso);
                        }
                        else
                        {
                            if (csRemarks == null) csRemarks = new CSharpXmlComment("remarks");
                            AddComment(csRemarks, childComment);
                        }
                        break;
                    default:
                        if (csRemarks == null) csRemarks = new CSharpXmlComment("remarks");
                        AddComment(csRemarks, childComment);
                        break;
                }
            }

            if (csRemarks != null && csRemarks.Children.Count > 0)
            {
                csFullComment.Children.Add(csRemarks);
            }

            return csFullComment;
        }

        private static CSharpTextComment GetAsCSharpComment(CppComment comment, bool trimEnd = true)
        {
            if (comment == null) return null;
            return new CSharpTextComment(GetAsText(comment, trimEnd));
        }

        private static string GetAsText(CppComment comment, bool trimEnd = true)
        {
            if (comment == null) return null;
            var text = comment.ToString();
            return trimEnd ? text.TrimEnd() : text;
        }

        private static CSharpTextComment GetChildAsCSharpComment(CppComment comment, bool trimEnd = true)
        {
            if (comment?.Children == null) return null;
            return new CSharpTextComment(GetChildAsText(comment, trimEnd));
        }

        private static void AddComment(CSharpComment dest, CppComment comment)
        {
            var text = GetAsText(comment);
            if (!string.IsNullOrEmpty(text))
            {
                dest.Children.Add(new CSharpTextComment(text));
            }
        }

        private static string GetChildAsText(CppComment comment, bool trimEnd = true)
        {
            if (comment?.Children == null) return null;
            var builder = new StringBuilder();
            foreach (var child in comment.Children)
            {
                builder.Append(child);
            }

            var text = builder.ToString();
            if (trimEnd) text = text.TrimEnd();
            return text;
        }
    }
}
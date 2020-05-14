// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
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

            var summary = new CSharpXmlComment("summary");
            var remarks = new CSharpXmlComment("remarks");

            for (var i = 0; i < comment.Children.Count; i++)
            {
                var childComment = comment.Children[i];

                if (i == 0)
                {
                    if (TryGetAsCSharpComment(childComment, out var summaryComment))
                    {
                        summary.Children.Add(summaryComment);
                        continue;
                    }
                }

                switch (childComment.Kind)
                {
                    case CppCommentKind.ParamCommand:

                        var paramComment = (CppCommentParamCommand)childComment;

                        if (TryGetChildAsCSharpComment(paramComment, out var paramChildComment))
                        {
                            var csParamComment = new CSharpParamComment(paramComment.ParamName);
                            csParamComment.Children.Add(paramChildComment);
                            csFullComment.Children.Add(csParamComment);
                        }

                        break;

                    case CppCommentKind.BlockCommand:
                        var blockCommand = (CppCommentBlockCommand)childComment;

                        switch (blockCommand.CommandName)
                        {
                            case "return":
                                if (TryGetChildAsCSharpComment(blockCommand, out var blockCommandComment))
                                {
                                    var returnComment = new CSharpReturnComment();
                                    returnComment.Children.Add(blockCommandComment);
                                    csFullComment.Children.Add(returnComment);
                                }
                                break;
                            case "ref":
                            case "see":
                                if (TryGetChildAsText(childComment, out var attribute))
                                {
                                    var seeAlso = new CSharpXmlComment("seealso")
                                    {
                                        IsSelfClosing = true
                                    };

                                    seeAlso.Attributes.Add(new CSharpXmlAttribute("cref", attribute));
                                    csFullComment.Children.Add(seeAlso);
                                }

                                break;
                            case "note":
                            case "deprecated":
                                if (TryGetChildAsText(childComment, out var noteText))
                                {
                                    remarks.Children.Add(new CSharpTextComment(noteText));
                                }
                                break;
                            default:
                                if (TryGetChildAsCSharpComment(childComment, out var summaryComment))
                                {
                                    summary.Children.Add(summaryComment);
                                }

                                break;
                        }
                        break;
                    case CppCommentKind.Paragraph:
                        if (TryGetAsText(childComment, out var paragraphText))
                        {
                            if (paragraphText.Contains("@retval"))
                            {
                                paragraphText = paragraphText.Replace("@retval ", "@");

                                var entries = paragraphText.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                                var returnComment = new CSharpReturnComment();

                                foreach (var entry in entries)
                                {
                                    var returnTextComment = new CSharpTextComment($"{entry}");
                                    returnComment.Children.Add(returnTextComment);
                                }

                                csFullComment.Children.Add(returnComment);
                            }
                            else
                            {
                                remarks.Children.Add(new CSharpTextComment(paragraphText));
                            }
                        }

                        break;
                    case CppCommentKind.Full:
                    case CppCommentKind.HtmlStartTag:
                    case CppCommentKind.HtmlEndTag:
                    case CppCommentKind.InlineCommand:
                    case CppCommentKind.Text:
                    case CppCommentKind.Null:
                    case CppCommentKind.TemplateParamCommand:
                    case CppCommentKind.VerbatimBlockCommand:
                    case CppCommentKind.VerbatimBlockLine:
                    case CppCommentKind.VerbatimLine:
                        {
                            if (TryGetAsCSharpComment(childComment, out var summaryComment))
                            {
                                summary.Children.Add(summaryComment);
                            }

                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            csFullComment.Children.Insert(0, summary);

            if (remarks.Children.Count > 0)
            {
                csFullComment.Children.Add(remarks);
            }

            return csFullComment;
        }

        private static bool TryGetAsCSharpComment(CppComment comment, out CSharpTextComment textComment, bool trimEnd = true)
        {
            if (TryGetAsText(comment, out var text, trimEnd))
            {
                textComment = new CSharpTextComment(text);
                return true;
            }

            textComment = null;
            return false;
        }

        private static bool TryGetChildAsCSharpComment(CppComment comment, out CSharpTextComment textComment, bool trimEnd = true)
        {
            if (TryGetChildAsText(comment, out var text, trimEnd))
            {
                textComment = new CSharpTextComment(text);
                return true;
            }

            textComment = null;
            return false;
        }

        private static bool TryGetAsText(CppComment comment, out string text, bool trimEnd = true)
        {
            text = null;

            if (comment == null)
            {
                return false;
            }

            text = comment.ToString();

            if (trimEnd)
            {
                text = text.TrimEnd();
            }

            return !string.IsNullOrEmpty(text);
        }

        private static bool TryGetChildAsText(CppComment comment, out string text, bool trimEnd = true)
        {
            text = null;
            var builder = new StringBuilder();

            if (comment?.Children != null)
            {
                foreach (var child in comment.Children)
                {
                    if (TryGetAsText(child, out var childText, trimEnd))
                    {
                        builder.Append(childText);
                    }
                }
            }

            text = builder.ToString();

            if (trimEnd)
            {
                text = text.TrimEnd();
            }

            return !string.IsNullOrEmpty(text);
        }
    }
}
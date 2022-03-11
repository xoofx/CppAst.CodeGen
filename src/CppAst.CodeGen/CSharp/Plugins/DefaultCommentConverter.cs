// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
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

            CSharpXmlComment csSummary = null;
            CSharpXmlComment csRemarks = null;

            var uncategorized = new List<CppComment>();

            for (var i = 0; i < comment.Children.Count; i++)
            {
                var childComment = comment.Children[i];

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

                        switch (blockCommand.CommandName)
                        {
                            case "return":
                            case "returns":
                                var returnComment = new CSharpReturnComment();
                                returnComment.Children.Add(GetChildAsCSharpComment(childComment));
                                csFullComment.Children.Add(returnComment);
                                break;
                            case "see":
                            case "sa":
                                if (childComment.Children != null && childComment.Children.Count > 0)
                                {
                                    var seealso = new CSharpSeeAlsoComment(childComment);
                                    csFullComment.Children.Add(seealso);
                                }
                                break;

                            case "brief":
                                csSummary ??= new CSharpXmlComment("summary");
                                csSummary.Children.Add(GetChildAsCSharpComment(childComment));
                                break;
                            case "remark":
                            case "remarks":
                                csRemarks ??= new CSharpXmlComment("remarks");
                                csRemarks.Children.Add(GetChildAsCSharpComment(childComment));
                                break;
                            case "since":
                                var since = new CSharpSinceComment();
                                since.Children.Add(GetChildAsCSharpComment(childComment));
                                csFullComment.Children.Add(since);
                                break;
                            default:
                                var genericComment = new CSharpXmlComment(blockCommand.CommandName);
                                genericComment.Children.Add(GetChildAsCSharpComment(childComment));
                                csFullComment.Children.Add(genericComment);
                                break;
                        }

                        break;
                    default:
                        uncategorized.Add(childComment);
                        break;
                }
            }

            foreach (var uncategorizedComment in uncategorized)
            {
                if (csSummary == null)
                {
                    csSummary = new CSharpXmlComment("summary");
                    AddComment(csSummary, uncategorizedComment);
                }
                else
                {
                    csRemarks ??= new CSharpXmlComment("remarks");
                    AddComment(csRemarks, uncategorizedComment);
                }
            }

            if (csSummary != null && csSummary.Children.Count > 0)
            {
                csFullComment.Children.Add(csSummary);
            }

            if (csRemarks != null && csRemarks.Children.Count > 0)
            {
                csFullComment.Children.Add(csRemarks);
            }

            var tagOrdinals = new[]
            {
                "summary", "typeparam", "param",
                "returns", "exception", "remarks",
                "seealso", "example", "since",
            };

            csFullComment.Children.Sort((first, second) =>
            {
                var firstXml = first as CSharpXmlComment;
                var secondXml = second as CSharpXmlComment;

                if (firstXml == null && secondXml == null) return 0;
                if (firstXml == null) return -1;
                if (secondXml == null) return 1;

                var firstOrdinal = Array.IndexOf(tagOrdinals, firstXml.TagName);
                var secondOrdinal = Array.IndexOf(tagOrdinals, secondXml.TagName);

                firstOrdinal = firstOrdinal != -1 ? firstOrdinal : tagOrdinals.Length;
                secondOrdinal = secondOrdinal != -1 ? secondOrdinal : tagOrdinals.Length;

                return firstOrdinal - secondOrdinal;
            });


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

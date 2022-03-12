using System.Collections.Generic;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpSeeAlsoComment : CSharpXmlComment
    {
        public CSharpSeeAlsoComment(CppComment cppComment) : base("seealso")
        {
            var builder = new StringBuilder();
            var cppCommentChildren = new Stack<CppComment>(cppComment.Children);

            while (cppCommentChildren.Count > 0)
            {
                var child = cppCommentChildren.Pop();

                if (child.Children != null && child.Children.Count > 0)
                {
                    foreach (var c in child.Children)
                    {
                        cppCommentChildren.Push(c);
                    }
                }
                else
                {
                    switch (child)
                    {
                        case CppCommentCommand command:
                        {
                            switch (command.CommandName)
                            {
                                case "ref":
                                    builder.Append(string.Join(" ", command.Arguments));
                                    break;
                                default:
                                    builder.Append(child);
                                    break;
                            }

                            break;
                        }
                        default:
                            builder.Append(child);
                            break;
                    }
                }
            }

            IsSelfClosing = true;
            Attributes.Add(new CSharpXmlAttribute("cref", builder.ToString()));
        }
    }
}

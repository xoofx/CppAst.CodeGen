using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpSinceComment : CSharpXmlComment
    {
        public CSharpSinceComment() : base("since")
        {
            IsInline = true;
        }

        public override void DumpTo(CodeWriter writer)
        {
            base.DumpTo(writer);
            writer.WriteLine();
        }
    }
}

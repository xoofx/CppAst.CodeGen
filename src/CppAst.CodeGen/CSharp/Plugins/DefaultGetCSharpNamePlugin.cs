using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultGetCSharpNamePlugin: ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpNameResolvers.Add(DefaultGetCSharpName);
        }

        public static string DefaultGetCSharpName(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            if (element is CppFunction cppFunction && context is ICSharpMember csMember)
            {
                var name = csMember.Name + "Delegate";
                return name;
            }
            return null;
        }
   }
}
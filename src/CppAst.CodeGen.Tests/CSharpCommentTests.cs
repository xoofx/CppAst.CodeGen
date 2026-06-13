// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests;

public class CSharpCommentTests
{
    [Test]
    public void FullAndSimpleCommentsApplyPrefixesAndEscapeXmlText()
    {
        var fullComment = new CSharpFullComment();
        var summary = new CSharpXmlComment("summary");
        summary.Children.Add(new CSharpTextComment("A < B & C > D"));
        fullComment.Children.Add(summary);

        Assert.AreEqual("/// <summary>\n/// A &lt; B &amp; C &gt; D\n/// </summary>\n", GeneratedCodeTestHelper.DumpToString(fullComment));

        var simpleComment = new CSharpSimpleComment();
        simpleComment.Children.Add(new CSharpTextComment("line one\nline two"));

        Assert.AreEqual("// line one\n// line two", GeneratedCodeTestHelper.DumpToString(simpleComment));
    }

    [Test]
    public void XmlCommentsSupportInlineSelfClosingParamReturnAndSinceForms()
    {
        var param = new CSharpParamComment("value");
        param.Children.Add(new CSharpTextComment("input"));
        Assert.AreEqual("<param name=\"value\">input</param>\n", GeneratedCodeTestHelper.DumpToString(param));

        var returns = new CSharpReturnComment();
        returns.Children.Add(new CSharpTextComment("result"));
        Assert.AreEqual("<returns>result</returns>\n", GeneratedCodeTestHelper.DumpToString(returns));

        var since = new CSharpSinceComment();
        since.Children.Add(new CSharpTextComment("1.2.3"));
        Assert.AreEqual("<since>1.2.3</since>\n", GeneratedCodeTestHelper.DumpToString(since));

        var seeAlso = new CSharpXmlComment("seealso") { IsSelfClosing = true };
        seeAlso.Attributes.Add(new CSharpXmlAttribute("cref", "Demo.Native"));
        Assert.AreEqual("<seealso cref=\"Demo.Native\"/>\n", GeneratedCodeTestHelper.DumpToString(seeAlso));
    }

    [Test]
    public void CloneDeepCopiesChildrenAndXmlAttributes()
    {
        var original = new CSharpXmlComment("summary");
        original.Attributes.Add(new CSharpXmlAttribute("langword", "true"));
        original.Children.Add(new CSharpTextComment("original"));

        var clone = (CSharpXmlComment)original.Clone();
        clone.Attributes[0].Value = "false";
        ((CSharpTextComment)clone.Children[0]).Text = "clone";

        Assert.AreEqual("true", original.Attributes[0].Value);
        Assert.AreEqual("original", ((CSharpTextComment)original.Children[0]).Text);
        Assert.AreEqual("false", clone.Attributes[0].Value);
        Assert.AreEqual("clone", ((CSharpTextComment)clone.Children[0]).Text);
    }

    [Test]
    public void ConverterTransformsDoxygenCommentsToEscapedXmlDocumentation()
    {
        var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility(""default"")))
#endif

// Adds <left> & right.
// @param value first <value>
// @return result > zero
EXPORT_API int documented(int value);
");

        GeneratedCodeTestHelper.AssertContainsAll(text,
            "/// Adds ",
            "/// &lt;left",
            "/// &gt; ",
            "/// &amp;",
            "/// right.",
            "/// <param name=\"value\">first ",
            "/// &lt;value",
            "/// &gt;</param>",
            "/// <returns>result &gt; zero</returns>");
    }
}

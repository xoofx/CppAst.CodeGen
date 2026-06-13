// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.CSharp;
using Zio;

namespace CppAst.CodeGen.Tests;

public class CSharpContainerListTests
{
    [Test]
    public void AddInsertRemoveAndClearMaintainParentLinks()
    {
        var parent = new CSharpClass("Parent");
        var first = new CSharpField("first") { FieldType = CSharpPrimitiveType.Int() };
        var second = new CSharpField("second") { FieldType = CSharpPrimitiveType.Int() };

        parent.Members.Add(first);
        parent.Members.Insert(0, second);

        Assert.AreSame(parent, first.Parent);
        Assert.AreSame(parent, second.Parent);
        Assert.AreEqual(0, parent.Members.IndexOf(second));
        Assert.AreEqual(1, parent.Members.IndexOf(first));

        Assert.True(parent.Members.Remove(second));
        Assert.Null(second.Parent);
        Assert.AreSame(parent, first.Parent);

        parent.Members.Clear();

        Assert.Null(first.Parent);
        Assert.AreEqual(0, parent.Members.Count);
    }

    [Test]
    public void ReplacingByIndexerMaintainsParentLinksAndValidation()
    {
        var parent = new CSharpClass("Parent");
        var oldField = new CSharpField("oldField") { FieldType = CSharpPrimitiveType.Int() };
        var newField = new CSharpField("newField") { FieldType = CSharpPrimitiveType.Int() };
        parent.Members.Add(oldField);

        parent.Members[0] = newField;

        Assert.Null(oldField.Parent);
        Assert.AreSame(parent, newField.Parent);
        Assert.AreSame(newField, parent.Members[0]);

        Assert.Throws<ArgumentException>(() => parent.Members[0] = new CSharpNamespace("Invalid"));
    }

    [Test]
    public void RejectsNullSelfAlreadyParentedAndInvalidMembers()
    {
        var parent = new CSharpClass("Parent");
        var otherParent = new CSharpClass("OtherParent");
        var field = new CSharpField("field") { FieldType = CSharpPrimitiveType.Int() };
        parent.Members.Add(field);

        Assert.Throws<ArgumentNullException>(() => parent.Members.Add(null!));
        Assert.Throws<ArgumentException>(() => parent.Members.Add(parent));
        Assert.Throws<ArgumentException>(() => otherParent.Members.Add(field));
        Assert.Throws<ArgumentException>(() => parent.Members.Add(new CSharpGeneratedFile((UPath)"/invalid.cs")));
        Assert.Throws<ArgumentException>(() => new CSharpCompilation().Members.Add(new CSharpNamespace("Invalid")));
    }
}

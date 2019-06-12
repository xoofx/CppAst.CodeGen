// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CppElementMatcher
    {
        public abstract bool Match(CppElement cppElement, List<ICppElementMatch> outMatches);
    }
}
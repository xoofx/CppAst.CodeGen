// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public class CppElementTypeMatcher : CppElementMatcher
    {
        public CppElementTypeMatcher(Type matchType)
        {
            MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
        }

        public Type MatchType { get; }

        /// <inheritdoc />
        public override bool Match(CppElement cppElement, List<ICppElementMatch> outMatches)
        {
            return MatchType.IsInstanceOfType(cppElement);
        }
    }

    public class CppElementTypeMatcher<T> : CppElementTypeMatcher where T : CppElement
    {
        public CppElementTypeMatcher() : base(typeof(T))
        {
        }

        /// <inheritdoc />
        public override bool Match(CppElement cppElement, List<ICppElementMatch> outMatches)
        {
            return cppElement is T;
        }
    }
}
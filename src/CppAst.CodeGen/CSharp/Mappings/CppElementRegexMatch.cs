// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    public class CppElementRegexMatch : ICppElementMatch
    {
        internal CppElementRegexMatch(List<Match> matches)
        {
            Matches = matches;

            var regexInputBuilder = new StringBuilder();
            var regexPatternBuilder = new StringBuilder();
            for (int i = 0; i < Matches.Count; i++)
            {
                var match = Matches[i];
                var groups = match.Groups;
                var full = groups[0].Value;

                if (i > 0)
                {
                    regexInputBuilder.Append("::");
                    regexPatternBuilder.Append("::");
                }
                regexInputBuilder.Append(full);

                int startIndex = 0;
                for (int j = 1; j < groups.Count; j++)
                {
                    var group = groups[j];

                    if ((group.Index - 1) > startIndex)
                    {
                        regexPatternBuilder.Append(full.Substring(startIndex, group.Index - startIndex));
                    }
                    regexPatternBuilder.Append("(");
                    regexPatternBuilder.Append(group.Value);
                    regexPatternBuilder.Append(")");
                    startIndex = group.Index + group.Length;
                }

                if (startIndex != full.Length)
                {
                    regexPatternBuilder.Append(full.Substring(startIndex, full.Length - startIndex));
                }
            }

            RegexInput = regexInputBuilder.ToString();
            RegexPattern = regexPatternBuilder.ToString();
        }

        public string RegexInput { get; }

        public string RegexPattern { get; }

        public List<Match> Matches { get; }
    }
}
// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    public class CppElementRegexMatcher : CppElementMatcher
    {
        private readonly string _regexString;
        private readonly List<Regex> _regexParts;

        public CppElementRegexMatcher(string regexString)
        {
            _regexString = regexString ?? throw new ArgumentNullException(nameof(regexString));

            _regexParts = new List<Regex>();
            var regexTxtParts = _regexString.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var regexTxtPart in regexTxtParts)
            {
                try
                {
                    var regex = new Regex($"^{regexTxtPart}$");
                    _regexParts.Add(regex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"The regex part `{regexTxtPart}` from `{_regexString}` is invalid. Reason: {ex.Message}");
                }
            }
            _regexParts.Reverse();
        }

        public override bool Match(CppElement cppElement, List<ICppElementMatch> outMatches)
        {
            var iterator = cppElement;
            int matchIndex = 0;
            List<Match> matches = null;
            while (iterator != null)
            {
                if (iterator is ICppMember member)
                {
                    if (matchIndex >= _regexParts.Count)
                    {
                        return false;
                    }

                    var regexPart = _regexParts[matchIndex];
                    var match = regexPart.Match(member.Name);
                    if (match.Success)
                    {
                        if (matches == null)
                        {
                            // TODO: we could pull List<Match> in case of failure
                            matches = new List<Match>();
                        }
                        matches.Add(match);
                        matchIndex++;
                        if (matchIndex == _regexParts.Count)
                        {
                            break;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                iterator = iterator.Parent as CppElement;
            }

            // We expect to match all the parts
            if (matchIndex != _regexParts.Count)
            {
                return false;
            }

            outMatches.Add(new CppElementRegexMatch(matches));

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"MapRegexName: `{_regexString}`";
        }
    }
}
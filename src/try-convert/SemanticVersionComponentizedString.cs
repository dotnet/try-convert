using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MSBuild.Conversion
{
    internal class SemanticVersionComponentizedString
    {
        public SemanticVersionComponentizedString(string rawString)
        {
            _rawString = rawString ?? string.Empty;
        }

        private readonly string _rawString;
        private IReadOnlyList<MixedTypeComparisonToken> _parts;

        public IReadOnlyList<MixedTypeComparisonToken> Parts
        {
            get
            {
                EnsureParsed();
                return _parts;
            }
        }

        private static Regex _partSplitterRegex = new Regex(@"
            (\.)            # literal dot - parenthesised so the dot tokens are included in the split results.
            |               # The dot MUST be above the zero-width assertions, otherwise it will not necessarily be split / captured.
            (?<=\d)(?=\D)   # digit / non-digit boundary
            |
            (?<=\D)(?=\d)   # non-digit / digit boundary
            ", RegexOptions.IgnorePatternWhitespace);

        private void EnsureParsed()
        {
            if (_parts == null)
            {
                _parts = _partSplitterRegex.Split(_rawString)
                                                    .Where(x => !string.IsNullOrEmpty(x))
                                                    .Select(x => new MixedTypeComparisonToken(x))
                                                    .ToList();
            }
        }

        public int CompareTo(SemanticVersionComponentizedString otherVersion)
        {
            return Compare(this, otherVersion);
        }

        public static int Compare(SemanticVersionComponentizedString components1, SemanticVersionComponentizedString components2)
        {
            if (components1.Parts.Count == 0 && components2.Parts.Count == 0)
            {
                return 0;
            }

            if (components1.Parts.Count == 0 || components2.Parts.Count == 0)
            {   // the one with parts is higher
                return components1.Parts.Count != 0 ? 1 : -1;
            }

            int checkLength = Math.Min(components1.Parts.Count, components2.Parts.Count);
            for (int i = 0; i < checkLength; i++)
            {
                int partResult = components1.Parts[i].CompareTo(components2.Parts[i]);
                if (partResult != 0)
                {
                    return partResult;
                }
            }

            return components1.Parts.Count.CompareTo(components2.Parts.Count);
        }
    }
}

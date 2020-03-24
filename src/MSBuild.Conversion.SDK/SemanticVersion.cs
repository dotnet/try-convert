using System;
using System.Text.RegularExpressions;

namespace MSBuild.Conversion
{
    public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private SemanticVersion(string originalText)
        {
            _hashCode = originalText?.GetHashCode() ?? 0;
            OriginalText = originalText;
        }
        private static readonly char[] LeadingVersionChars = { 'v', 'V' };
        private static readonly Regex SemanticVersionRegex = new Regex(@"
                [vV]?
                ([0-9]+)
                (\.[0-9]+)?
                (\.[0-9]+)?
                (
                    -
                    (
                        [0-9A-Za-z\-]+
                        (
                            \.
                            [0-9A-Za-z\-]+
                        )*
                    )
                )?",
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace
        );

        private readonly int _hashCode;

        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }
        public string PrereleaseVersion { get; private set; }
        public string BuildMetadata { get; private set; }
        public SemanticVersionComponentizedString BuildMetadataComponents { get; private set; }
        public SemanticVersionComponentizedString PrereleaseVersionComponents { get; private set; }
        public string OriginalText { get; }

        public static SemanticVersion Min => new SemanticVersion(null);

        public static SemanticVersion Parse(string value)
        {
            if (value == null)
            {
                return new SemanticVersion(value);
            }

            value = value.Trim();
            var match = SemanticVersionRegex.Match(value);
            if (!match.Success)
            {
                return new SemanticVersion(string.Empty);
            }

            SemanticVersion ver = new SemanticVersion(value);

            int prereleaseStart = value.IndexOf('-');
            int buildMetadataStart = value.IndexOf('+');

            //If the index of the build metadata marker (+) is greater than the index of the prerelease marker (-)
            //  then it is necessarily found in the string because if both were not found they'd be equal
            if (buildMetadataStart > prereleaseStart)
            {
                //If the build metadata marker is not the last character in the string, take off everything after it
                //  and use it for the build metadata field
                if (buildMetadataStart < value.Length - 1)
                {
                    ver.BuildMetadata = value.Substring(buildMetadataStart + 1);
                }

                value = value.Substring(0, buildMetadataStart);

                //If the prerelease section is found, extract it
                if (prereleaseStart > -1)
                {
                    //If the prerelease section marker is not the last character in the string, take off everything after it
                    //  and use it for the prerelease field
                    if (prereleaseStart < value.Length - 1)
                    {
                        ver.PrereleaseVersion = value.Substring(prereleaseStart + 1);
                    }

                    value = value.Substring(0, prereleaseStart);
                }
            }
            //If the build metadata wasn't the last metadata section found, check to see if a prerelease section exists.
            //  If it doesn't, then neither section exists
            else if (prereleaseStart > -1)
            {
                //If the prerelease version marker is not the last character in the string, take off everything after it
                //  and use it for the prerelease version field
                if (prereleaseStart < value.Length - 1)
                {
                    ver.PrereleaseVersion = value.Substring(prereleaseStart + 1);
                }

                value = value.Substring(0, prereleaseStart);

                //If the build metadata section is found, extract it
                if (buildMetadataStart > -1)
                {
                    //If the build metadata marker is not the last character in the string, take off everything after it
                    //  and use it for the build metadata field
                    if (buildMetadataStart < value.Length - 1)
                    {
                        ver.BuildMetadata = value.Substring(buildMetadataStart + 1);
                    }

                    value = value.Substring(0, buildMetadataStart);
                }
            }

            string[] versionParts = value.Split('.');

            if (versionParts.Length > 0)
            {
                int major;
                int.TryParse(versionParts[0].TrimStart(LeadingVersionChars), out major);
                ver.Major = major;
            }

            if (versionParts.Length > 1)
            {
                int minor;
                int.TryParse(versionParts[1], out minor);
                ver.Minor = minor;
            }

            if (versionParts.Length > 2)
            {
                int patch;
                int.TryParse(versionParts[2], out patch);
                ver.Patch = patch;
            }

            // setup the componentized versions, for later comparison. Nulls are ok, SemanticVersionComponentizedString handles them.
            ver.BuildMetadataComponents = new SemanticVersionComponentizedString(ver.BuildMetadata);
            ver.PrereleaseVersionComponents = new SemanticVersionComponentizedString(ver.PrereleaseVersion);

            return ver;
        }

        public int CompareTo(SemanticVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = Major.CompareTo(other.Major);

            if (result != 0)
            {
                return result;
            }

            result = Minor.CompareTo(other.Minor);

            if (result != 0)
            {
                return result;
            }

            result = Patch.CompareTo(other.Patch);

            if (result != 0)
            {
                return result;
            }

            if (PrereleaseVersionComponents.Parts.Count == 0 && other.PrereleaseVersionComponents.Parts.Count > 0)
            {
                return 1;
            }
            else if (PrereleaseVersionComponents.Parts.Count > 0 && other.PrereleaseVersionComponents.Parts.Count == 0)
            {
                return -1;
            }
            // only compare the prerel parts if they both have them (or neither, which will compare equal
            result = PrereleaseVersionComponents.CompareTo(other.PrereleaseVersionComponents);
            if (result != 0)
            {
                return result;
            }

            if (BuildMetadataComponents.Parts.Count == 0 && other.BuildMetadataComponents.Parts.Count > 0)
            {
                return 1;
            }
            else if (BuildMetadataComponents.Parts.Count > 0 && other.BuildMetadataComponents.Parts.Count == 0)
            {
                return -1;
            }
            result = BuildMetadataComponents.CompareTo(other.BuildMetadataComponents);
            if (result != 0)
            {
                return result;
            }

            string thisOriginalText = OriginalText?.TrimStart(LeadingVersionChars) ?? string.Empty;
            string otherOriginalText = other.OriginalText?.TrimStart(LeadingVersionChars) ?? string.Empty;

            return StringComparer.OrdinalIgnoreCase.Compare(thisOriginalText, otherOriginalText);
        }

        public bool Equals(SemanticVersion other) => other != null && string.Equals(OriginalText, other.OriginalText, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => Equals(obj as SemanticVersion);
        public override int GetHashCode() => _hashCode;
        public static bool operator ==(SemanticVersion a, SemanticVersion b) => a?.Equals(b) != null;
        public static bool operator !=(SemanticVersion a, SemanticVersion b) => a?.Equals(b) == false;
        public static bool operator >=(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) >= 0;
        public static bool operator <=(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) <= 0;
        public static bool operator >(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) > 0;
        public static bool operator <(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) < 0;
        public override string ToString() => OriginalText;
    }
}

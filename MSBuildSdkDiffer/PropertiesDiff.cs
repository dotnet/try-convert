using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    internal struct PropertiesDiff
    {
        public readonly ImmutableArray<ProjectProperty> DefaultedProperties;
        public readonly ImmutableArray<ProjectProperty> NotDefaultedProperties;
        public readonly ImmutableArray<(ProjectProperty oldProp, ProjectProperty newProp)> ChangedProperties;

        public PropertiesDiff(ImmutableArray<ProjectProperty> defaultedProperties, ImmutableArray<ProjectProperty> notDefaultedPropeties, ImmutableArray<(ProjectProperty, ProjectProperty)> changedProperties) : this()
        {
            DefaultedProperties = defaultedProperties;
            NotDefaultedProperties = notDefaultedPropeties;
            ChangedProperties = changedProperties;
        }

        public ImmutableArray<string> GetDiffLines()
        {
            var lines = ImmutableArray.CreateBuilder<string>();

            if (!DefaultedProperties.IsEmpty)
            {
                lines.Add("Properties that are defaulted by the SDK:");
                lines.AddRange(DefaultedProperties.Select(prop => $"- {prop.Name} = {prop.EvaluatedValue}"));
                lines.Add("");
            }
            if (!NotDefaultedProperties.IsEmpty)
            {
                lines.Add("Properties that are not defaulted by the SDK:");
                lines.AddRange(DefaultedProperties.Select(prop => $"+ {prop.Name} = {prop.EvaluatedValue}"));
                lines.Add("");
            }
            if (!ChangedProperties.IsEmpty)
            {
                lines.Add("Properties whose value is different from the SDK's default:");
                var changedProps = ChangedProperties.SelectMany((diff) =>
                    new[]
                    {
                        $"- {diff.oldProp.Name} = {diff.oldProp.EvaluatedValue}",
                        $"- {diff.newProp.Name} = {diff.newProp.EvaluatedValue}"
                    }
                );
                lines.AddRange(changedProps);
                lines.Add("");
            }

            return lines.ToImmutable();
        }
    }
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WinUI.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UWPStructAnalyzer : DiagnosticAnalyzer
    {
        // Analyzer IDs are reported as Error Codes
        public const string ID = "UWPStructNeedsHelperClass";

        public static readonly Dictionary<string, string> Structs = new Dictionary<string, string>
        {
            { "Windows.Foundation.CornerRadius", "Microsoft.UI.Xaml.CornerRadiusHelper" },
            { "Windows.Foundation.Duration", "Microsoft.UI.Xaml.DurationHelper" },
            { "Windows.Foundation.GridLength", "Microsoft.UI.Xaml.GridLengthHelper" },
            { "Windows.Foundation.Thickness", "Microsoft.UI.Xaml.ThicknessHelper" },
            { "Windows.Foundation.GeneratorPosition", "Microsoft.UI.Xaml.Controls.Primitives.GeneratorPositionHelper" },
            { "Windows.Foundation.Media.Matrix", "Microsoft.UI.Xaml.MatrixHelper" },
            { "Windows.Foundation.Media.Animation.KeyTime", "Microsoft.UI.Xaml.Media.Animation.KeyTimeHelper" },
            { "Windows.Foundation.Media.Animation.RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorHelper" },
            { "Windows.UI.Xaml.CornerRadius", "Microsoft.UI.Xaml.CornerRadiusHelper" },
            { "Windows.UI.Xaml.Duration", "Microsoft.UI.Xaml.DurationHelper" },
            { "Windows.UI.Xaml.GridLength", "Microsoft.UI.Xaml.GridLengthHelper" },
            { "Windows.UI.Xaml.Thickness", "Microsoft.UI.Xaml.ThicknessHelper" },
            { "Windows.UI.Xaml.Controls.Primitives.GeneratorPosition", "Microsoft.UI.Xaml.Controls.Primitives.GeneratorPositionHelper" },
            { "Windows.UI.Xaml.Media.Matrix", "Microsoft.UI.Xaml.Media.MatrixHelper" },
            { "Windows.UI.Xaml.Media.Animation.KeyTime", "Microsoft.UI.Xaml.Media.Animation.KeyTimeHelper" },
            { "Windows.UI.Xaml.Media.Animation.RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorHelper" }
        };

        // TODO: FIX THese Strings! 
        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UWPStruct_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UWPStruct_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UWPStruct_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor UWPStructRule = new DiagnosticDescriptor(ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(UWPStructRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObject, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObject(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            // skip trivia
            if (node.IsPartOfStructuredTrivia()) return;
            var model = context.SemanticModel;
            var objectType = model.GetTypeInfo(node).Type;
            var compilation = model.Compilation;
            foreach (string s in Structs.Keys)
            {
                var types = Utils.GetTypesByMetadataName(compilation, s);

                if (types.Any(t => SymbolEqualityComparer.Default.Equals(objectType, t)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(UWPStructRule, node.GetLocation()));
                    return;
                }
            }
        }


    }
}

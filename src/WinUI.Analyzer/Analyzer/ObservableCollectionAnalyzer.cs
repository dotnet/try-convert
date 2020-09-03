using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WinUI.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ObservableCollectionAnalyzer : DiagnosticAnalyzer
    {
        // Analyzer IDs are reported as Error Codes
        public const string ID = "ObservableCollection";

        private static readonly string ObservableType = "System.Collections.ObjectModel.ObservableCollection`1";

        // TODO: FIX THese Strings! 
        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UWPObservable_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UWPObservable_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UWPObservable_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor ObservableRule = new DiagnosticDescriptor(ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ObservableRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObject, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObject(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            if (node.IsPartOfStructuredTrivia()) return;
            var model = context.SemanticModel;
            var objectType = model.GetTypeInfo(node).Type;
            if (objectType == null) return;
            var compilation = model.Compilation;
            var obsType = Utils.GetTypesByMetadataName(compilation, ObservableType);
            if (obsType.Any(t => SymbolEqualityComparer.Default.Equals(objectType.OriginalDefinition, t)))
            {
                context.ReportDiagnostic(Diagnostic.Create(ObservableRule, node.GetLocation()));
                return;
            }
        }
    }
}

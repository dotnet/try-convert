using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WinUI.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceAnalyzer : DiagnosticAnalyzer
    {
        // Analyzer IDs are reported as Error Codes
        public const string ID = "ConvertNamespace";
        public const string TypeName = "ConvertTypeNamespace";

        //Used for checking valid/invalid namespaces
        private static readonly string[] VALIDNAMES = Utils.GetNamespaceNames();

        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Namespace_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Namespace_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Namespace_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor NamespaceRule = new DiagnosticDescriptor(ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor TypeRule = new DiagnosticDescriptor(TypeName, "Update Type Namespace", MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(NamespaceRule, TypeRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // Call analyzer on all Identifier Name Nodes to see if need to implement diagnostic at that location
            context.RegisterSyntaxNodeAction(AnalyzeQualifiedNames, SyntaxKind.QualifiedName);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessNames, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeMemberAccessNames(SyntaxNodeAnalysisContext context)
        {
            var node = (MemberAccessExpressionSyntax)context.Node;
            // Filter out part of C# comments
            if (node.IsPartOfStructuredTrivia())
            {
                return;
            }

            // this is in the works... try to get type of simple expression
            var model = context.SemanticModel;
            var compilation = context.Compilation;
            var idNode = node.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
            if (idNode != null)
            {
                var idType = model.GetTypeInfo(idNode);
            }
            var nodeSymbol = model.GetTypeInfo(node);
            // below is actual working code...

            while (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                node = (MemberAccessExpressionSyntax)node.Parent;
            }
            var nodeRep = $"{node.Expression}.{node.Name}";
            if (VALIDNAMES.Any(s => nodeRep.StartsWith(s, StringComparison.OrdinalIgnoreCase)) || nodeRep.StartsWith("Windows.UI.Xaml"))
            {
                var idNameNode = node.ChildNodes().OfType<IdentifierNameSyntax>().First();
                context.ReportDiagnostic(Diagnostic.Create(TypeRule, context.Node.GetLocation()));
            }
        }

        private void AnalyzeQualifiedNames(SyntaxNodeAnalysisContext context)
        {
            var node = (QualifiedNameSyntax)context.Node;
            // Filter out Qualified Names that are not Windows or Microsoft or part of C# comments
            if (!node.Left.ToString().Equals("Windows") || node.IsPartOfStructuredTrivia())
            {
                return;
            }

            //Get fully qualified name
            var nodeRep = Utils.GetFullID(node);
            if (VALIDNAMES.Any(s => nodeRep.StartsWith(s, StringComparison.OrdinalIgnoreCase)) || nodeRep.StartsWith("Windows.UI.Xaml"))
            {
                var idNameNode = node.ChildNodes().OfType<IdentifierNameSyntax>().First();
                context.ReportDiagnostic(Diagnostic.Create(NamespaceRule, idNameNode.GetLocation()));
            }
        }
    }
}

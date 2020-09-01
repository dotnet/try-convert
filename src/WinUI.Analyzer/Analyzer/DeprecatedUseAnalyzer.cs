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
    public class DeprecatedUseAnalyzer : DiagnosticAnalyzer
    {
        public const string ID = "IncompatibleAPI"; 

        //Used for checking valid/invalid namespaces
        private static readonly String[] InvalidNames = Utils.GetDeprecatedNames();

        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Incompatible_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Incompatible_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Incompatible_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor DeprecatedRule = new DiagnosticDescriptor(ID, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        //Returns a set of descriptors (Rules) that this analyzer is capable of reproducing
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(DeprecatedRule); } }

        // Overide to implement DiagnosticAnalyzer Class
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeDeprecatedNames, SyntaxKind.QualifiedName);
        }

        /// <summary>
        /// Decides if a new diagnostic should be created given SyntaxNode context
        /// </summary>
        /// <param name="context"></param>
        private void AnalyzeDeprecatedNames(SyntaxNodeAnalysisContext context)
        {
            var node = (QualifiedNameSyntax)context.Node;
            var left = node.Left.ToString();
            // Some Deprecated Namespaces include Microsoft. Als Filter out Qualified names from Comments 
            if ((!left.Equals("Windows") && !left.Equals("Microsoft") && !left.Equals("Window")) || node.IsPartOfStructuredTrivia())
            {
                return;
            }
            //Get full Name
            String nodeRep = Utils.GetFullID(node);
            if (InvalidNames.Contains(nodeRep))
            {
                var idNameNode = node.ChildNodes().OfType<IdentifierNameSyntax>().First();
                context.ReportDiagnostic(Diagnostic.Create(DeprecatedRule, idNameNode.GetLocation()));
            }
        }
        
    }
}

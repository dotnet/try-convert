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
    public class EventArgsAnalyzer : DiagnosticAnalyzer
    {
        // ID if Fully qualified name is not used in method declaration
        public const string Param_ID = "LaunchActivatedEventArgsUpdate";

        // ID if wrapper does not access .UWPLaunchActivatedEventArgs
        public const string Use_ID = "EventArgsUWPUpdate";

        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.EventArgs_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.EventArgs_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.EventArgs_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor EventArgsRule = new DiagnosticDescriptor(Param_ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor UWPEventArgsRule = new DiagnosticDescriptor(Use_ID, "Must access UWPLaunchActivatedEventArgs", MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(EventArgsRule, UWPEventArgsRule); } }

        // Overide to implement DiagnosticAnalyzer Class
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeEventArgs, SyntaxKind.MethodDeclaration);
        }

        /// <summary>
        /// Analyzes a Method Declaration Syntax Node for proper WinUI3 OnLaunched() use
        /// </summary>
        /// <param name="context"></param>        
        private void AnalyzeEventArgs(SyntaxNodeAnalysisContext context)
        {
            var onLaunchedMethod = (MethodDeclarationSyntax)context.Node;
            // Skip any if in comments
            if (onLaunchedMethod.IsPartOfStructuredTrivia()) return;
            // Only investigate if OnLaunched method
            if (!onLaunchedMethod.Identifier.ToString().Equals("OnLaunched")) return;
            // Only if overide
            SemanticModel model = context.SemanticModel;
            Compilation compilation = context.Compilation;
            var onLaunchedSymbol = model.GetDeclaredSymbol(onLaunchedMethod);
            if (!onLaunchedSymbol.IsOverride) return;
            // Base class must be Microsoft.UI.Xaml.Application
            var classSymbol = onLaunchedSymbol.ContainingType;
            if (classSymbol == null) return;
            var baseType = classSymbol.BaseType;
            if (baseType == null) return;

            // get types to compare
            var MicrosoftApps = Utils.GetTypesByMetadataName(compilation, "Microsoft.UI.Xaml.Application");
            var WindowsApps = Utils.GetTypesByMetadataName(compilation, "Windows.UI.Xaml.Application");
            var bothApps = MicrosoftApps.Union(WindowsApps);

            // Roslyn Having issues comparing types, using toString...
            if (bothApps.Any(b => baseType.ToString().Equals(b.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                // Since derives from correct base method, should only have 1 parameter matching LaunchActivatedEventArgs
                var eventArgsParam = onLaunchedMethod.ParameterList.ChildNodes().OfType<ParameterSyntax>().SingleOrDefault(p => p.Type.ToString().Contains("LaunchActivatedEventArgs"));
                if (eventArgsParam == null) return;
                // check if parameter symbol correctly targets new LaunchActivatedEventArgs 
                var paramSymbol = model.GetDeclaredSymbol(eventArgsParam);
                var pType = model.GetTypeInfo(eventArgsParam);
                // Convert to fully qualified name string
                var typeStr = Utils.GetFullDisplayString(paramSymbol.Type);
                // If not using proper type throw diagnostic
                if (!typeStr.Equals("Microsoft.UI.Xaml.LaunchActivatedEventArgs"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(EventArgsRule, eventArgsParam.GetLocation()));
                }
                // If OnLaunched App Method then check if parameter is used correctly throughout body
                if (eventArgsParam != null)
                {
                    var paramName = paramSymbol.Name;
                    // Exit if no paramater name has been defined
                    if (paramName.Equals("")) return;
                    // Throw Diagnostic if parameter use does not access .UWPLaunchActivatedEventArgs
                    var paramUse = onLaunchedMethod.Body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.Identifier.ToString().Equals(paramName));
                    foreach (IdentifierNameSyntax i in paramUse)
                    {
                        var parent = i.Parent;
                        // Throw for all instances
                        if (!"UWPLaunchActivatedEventArgs".Equals(parent.TryGetInferredMemberName()))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(UWPEventArgsRule, i.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}

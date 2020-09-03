using System;
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
    public class UWPProjectionAnalyzer : DiagnosticAnalyzer
    {
        // Analyzer IDs are reported as Error Codes
        public const string ID = "UWPProjection";
        public const string InterfaceID = "UWPMovedInterface";
        public const string ObjectID = "UWPMovedObject";
        public const string TypeID = "UWPMovedType";
        public const string EventID = "UWPICommandEvent";

        private static readonly string[] iCommands = new string[] {
            "System.Windows.Input.ICommand",
            "Microsoft.UI.Xaml.Input.ICommand"
        };

        private static readonly string[] DotNetInterfaces = new string[]{
            "System.ComponentModel.INotifyPropertyChanged",
            "System.Windows.Input.ICommand"
        };

        private static readonly String[] DotNetClasses = new String[]{
            "System.ComponentModel.PropertyChangedEventArgs"
        };

        private static readonly String[] DotNetTypes = new String[]{
            "System.ComponentModel.PropertyChangedEventHandler",
            "System.ComponentModel.INotifyPropertyChanged",
            "System.Windows.Input.ICommand"
        };

        // TODO: FIX THese Strings! 
        // Localized analyzer descriptions
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UWPProjection_Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UWPProjection_MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UWPProjection_Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor InterfaceRule = new DiagnosticDescriptor(ID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor ChangedInterfaceRule = new DiagnosticDescriptor(InterfaceID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor ChangedObjectRule = new DiagnosticDescriptor(ObjectID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor ChangeTypeRule = new DiagnosticDescriptor(TypeID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor EventHandlerRule = new DiagnosticDescriptor(EventID, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(InterfaceRule, ChangedInterfaceRule, ChangedObjectRule, ChangeTypeRule, EventHandlerRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.BaseList);
            context.RegisterSyntaxNodeAction(AnalyzeObject, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeEvent, SyntaxKind.EventFieldDeclaration);
        }

        private void AnalyzeEvent(SyntaxNodeAnalysisContext context)
        {
            var node  = (EventFieldDeclarationSyntax)context.Node;
            if (node.Parent == null) return;
            // skip trivia
            if (node.IsPartOfStructuredTrivia()) return;
            var baseList = node.Parent.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();
            if (baseList == null) return;
            if (!implementsInterface(baseList, iCommands)) return;
            // check if EventHandler is CanExecuteChanged 
            var identifierNode = node.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            if (identifierNode == null || !identifierNode.Identifier.ToString().Equals("CanExecuteChanged", StringComparison.Ordinal)) return;
            // check if implements generic
            var generic = node.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();
            if (generic == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(EventHandlerRule, node.GetLocation()));
            }
            // TODO: continue checking if is wrong generic?
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
        {
            var node = (IdentifierNameSyntax)context.Node;

            // do not continue if part of simple name syntax
            if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)) return;
            // do not continue if interface declaration
            if (node.Ancestors().OfType<BaseListSyntax>().Any()) return;
            var model = context.SemanticModel;
            var compilation = model.Compilation;
            var type = model.GetTypeInfo(node).Type;
            if (type == null) return;
            foreach (string s in DotNetTypes)
            {
                var type1 = compilation.GetTypeByMetadataName(s);
                if (type1 == null) continue;
                if (SymbolEqualityComparer.Default.Equals(type, type1))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ChangeTypeRule, node.GetLocation()));
                    return;
                }
            }
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var baseListNode = (BaseListSyntax)context.Node;
            // skip if part of a commend or documentation
            if (baseListNode.IsPartOfStructuredTrivia()) return;
            var baseNodes = baseListNode.DescendantNodes().OfType<SimpleBaseTypeSyntax>();
            var model = context.SemanticModel;
            var compilation = model.Compilation;
            if (!(baseListNode.Parent is SyntaxNode baseParent)) return;
            var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(baseParent);
            if (symbol == null) return;
            var allInterfaces = symbol.AllInterfaces;
            // see if any interface implements one that needs to be changed
            foreach (var s in DotNetInterfaces)
            {
                var type1 = compilation.GetTypeByMetadataName(s);
                if (type1 == null) continue;
                foreach (INamedTypeSymbol i in allInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(i, type1))
                    {
                        AnalyzeInterfaces(baseNodes, context);
                    }
                }
            }
        }

        private void AnalyzeInterfaces(IEnumerable<SimpleBaseTypeSyntax> interfaces, SyntaxNodeAnalysisContext context)
        {
            foreach (SimpleBaseTypeSyntax simpleBase in interfaces)
            {
                var fullName = simpleBase.ToString();
                if (DotNetInterfaces.Any(str => str.Contains(fullName)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ChangedInterfaceRule, simpleBase.GetLocation()));
                    break;
                }
            }
        }

        private void AnalyzeObject(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            // skip trivia
            if (node.IsPartOfStructuredTrivia()) return;
            var model = context.SemanticModel;
            var objectType = model.GetTypeInfo(node).Type;
            var compilation = model.Compilation;
            foreach (string s in DotNetClasses)
            {
                var type1 = compilation.GetTypeByMetadataName(s);
                if (type1 == null) continue;
                if (SymbolEqualityComparer.Default.Equals(objectType, type1))
                {
                    var identifier = node.ChildNodes().OfType<IdentifierNameSyntax>().First();
                    context.ReportDiagnostic(Diagnostic.Create(ChangedObjectRule, identifier.GetLocation()));
                    return;
                }
            }
        }

        private bool implementsInterface(BaseListSyntax baseList, IEnumerable<string> interfaceTypes)
        {
            var interfaces = baseList.DescendantNodes().OfType<SimpleBaseTypeSyntax>();
            foreach (SimpleBaseTypeSyntax simpleBase in interfaces)
            {
                var fullName = simpleBase.ToString();
                if (interfaceTypes.Any(str => str.Contains(fullName)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinUI.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UWPProjectionCodeFix)), Shared]
    public class UWPProjectionCodeFix : CodeFixProvider
    {
        private const string title = "Fix Projections";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(UWPProjectionAnalyzer.InterfaceID,
              UWPProjectionAnalyzer.ObjectID, UWPProjectionAnalyzer.TypeID, UWPProjectionAnalyzer.EventID);
            }
        }

        // an optional overide to fix all occurences instead of just one.
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpanSrc = diagnostic.Location.SourceSpan;
            var idNode = root.FindNode(diagnosticSpanSrc);
            if (diagnostic.Id.Equals(UWPProjectionAnalyzer.InterfaceID))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceInterfaceAsync(context.Document, (SimpleBaseTypeSyntax)idNode, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            else if (diagnostic.Id.Equals(UWPProjectionAnalyzer.TypeID))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceTypeAsync(context.Document, (IdentifierNameSyntax)idNode, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            else if (diagnostic.Id.Equals(UWPProjectionAnalyzer.EventID))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceEventAsync(context.Document, (EventFieldDeclarationSyntax)idNode, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            else
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceTypeAsync(context.Document, (IdentifierNameSyntax)idNode, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Document> ReplaceEventAsync(Document doc, EventFieldDeclarationSyntax idNode, CancellationToken c)
        {
            var repNode = idNode.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
            if (repNode == null) return doc;
            var newEvent = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("EventHandler"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))));
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(repNode, newEvent);
            return doc.WithSyntaxRoot(newRoot);
        }

        internal async Task<Document> ReplaceTypeAsync(Document doc, IdentifierNameSyntax idNode, CancellationToken c)
        {
            //get name of node
            var nodeName = idNode.TryGetInferredMemberName();
            var sub = nodeName.Equals("ICommand") ? "Input" : "Data";
            var newQualified = SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("Microsoft"),
                            SyntaxFactory.IdentifierName("UI")),
                        SyntaxFactory.IdentifierName("Xaml")),
                    SyntaxFactory.IdentifierName(sub)),
                idNode);
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(idNode, newQualified);
            return doc.WithSyntaxRoot(newRoot);
        }

        internal async Task<Document> ReplaceInterfaceAsync(Document doc, SimpleBaseTypeSyntax idNode, CancellationToken c)
        {
            // Target the Identification Node for interface to Replace
            var lastType = idNode.DescendantNodes().OfType<IdentifierNameSyntax>().Last().TryGetInferredMemberName();
            SimpleBaseTypeSyntax newNode = null;
            if (lastType.Equals("INotifyPropertyChanged"))
            {
                newNode = SyntaxFactory.SimpleBaseType(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Microsoft"),
                                SyntaxFactory.IdentifierName("UI")),
                            SyntaxFactory.IdentifierName("Xaml")),
                        SyntaxFactory.IdentifierName("Data")),
                    SyntaxFactory.IdentifierName("INotifyPropertyChanged")));
            }
            else if (lastType.Equals("ICommand"))
            {
                newNode = SyntaxFactory.SimpleBaseType(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Microsoft"),
                                SyntaxFactory.IdentifierName("UI")),
                            SyntaxFactory.IdentifierName("Xaml")),
                        SyntaxFactory.IdentifierName("Input")),
                    SyntaxFactory.IdentifierName("ICommand")));
            }
            if (newNode == null)
            {
                return doc;
            }
            else
            {
                if (idNode.HasTrailingTrivia)
                {
                    newNode = newNode.WithTrailingTrivia(idNode.GetTrailingTrivia());
                }
                var oldRoot = await doc.GetSyntaxRootAsync(c);
                var newRoot = oldRoot.ReplaceNode(idNode, newNode);
                return doc.WithSyntaxRoot(newRoot);
            }
        }
    }
}


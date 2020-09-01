using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace WinUI.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceCodeFix)), Shared]
    public class NamespaceCodeFix : CodeFixProvider
    {
        private const string title = "Convert Namespace";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NamespaceAnalyzer.ID, NamespaceAnalyzer.TypeName); }
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
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceNameAsync(context.Document, idNode, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> ReplaceNameAsync(Document doc, SyntaxNode idNode, CancellationToken c)
        {
            var winToken = idNode.DescendantTokens().Single(n => n.Text == "Windows");
            var winLeadTrivia = winToken.LeadingTrivia;
            var winTrailTrivia = winToken.TrailingTrivia;
            var micToken = SyntaxFactory.Identifier(winLeadTrivia, SyntaxKind.IdentifierToken, "Microsoft", "Microsoft", winTrailTrivia);
            var newNode = idNode.ReplaceToken(winToken, micToken);
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(idNode, newNode);
            return doc.WithSyntaxRoot(newRoot);
        }
    }
}


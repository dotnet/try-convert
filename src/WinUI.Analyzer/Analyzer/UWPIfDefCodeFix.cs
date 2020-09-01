using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinUI.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UWPIfDefCodeFix)), Shared]
    public class UWPIfDefCodeFix : CodeFixProvider
    {
        private const string title = "Replace within IfDef";
        private bool includeObservable;

        // Composition classes
        UWPStructCodeFix UWPStruct = new UWPStructCodeFix();
        UWPProjectionCodeFix UWPProjection = new UWPProjectionCodeFix();
        ObservableCollectionCodeFix Observable = new ObservableCollectionCodeFix();

        public UWPIfDefCodeFix() : this(true) { }

        public UWPIfDefCodeFix(bool includeObservable)
        {
            this.includeObservable = includeObservable;
        }


        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return UWPStruct.FixableDiagnosticIds.AddRange(UWPProjection.FixableDiagnosticIds).AddRange(Observable.FixableDiagnosticIds); }
        }

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
            if (includeObservable && diagnostic.Id.Equals(ObservableCollectionAnalyzer.ID, StringComparison.OrdinalIgnoreCase))
            {
                // ObservableCollection returns a solution not a document
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => ReplaceSolutionLineAsync(context.Document, idNode, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            else if (!diagnostic.Id.Equals(ObservableCollectionAnalyzer.ID, StringComparison.OrdinalIgnoreCase))
            {
                // All other codefixes return a document
                context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceDocumentLineAsync(context.Document, idNode, diagnostic.Id, c),
                    equivalenceKey: title),
                diagnostic);
            }
        }

        private async Task<Solution> ReplaceSolutionLineAsync(Document doc, SyntaxNode idNode, CancellationToken c)
        {
            string tag = $"{doc.Id}idNode";
            var newDoc = Utils.GetIfDefDoc(doc, idNode, tag, c).Result;
            // find the original node in the new tree using the tag
            idNode = newDoc.GetSyntaxRootAsync().Result.GetAnnotatedNodes(tag).Single();
            return Observable.ReplaceObservableCollectionAsync(newDoc, (ObjectCreationExpressionSyntax)idNode, c).Result;
        }

        private async Task<Document> ReplaceDocumentLineAsync(Document doc, SyntaxNode idNode, string Id, CancellationToken c)
        {
            string tag = $"{doc.Id}idNode";
            var newDoc = Utils.GetIfDefDoc(doc, idNode, tag, c).Result;
            idNode = newDoc.GetSyntaxRootAsync().Result.GetAnnotatedNodes(tag).Single();
            // Call correct codefix method based on diagnostic id
            if (Id.Equals(UWPStructAnalyzer.ID, StringComparison.OrdinalIgnoreCase))
            {
                return UWPStruct.ReplaceStructAsync(newDoc, (ObjectCreationExpressionSyntax)idNode, c).Result;
            }
            else if (Id.Equals(UWPProjectionAnalyzer.InterfaceID, StringComparison.OrdinalIgnoreCase))
            {
                return UWPProjection.ReplaceInterfaceAsync(newDoc, (SimpleBaseTypeSyntax)idNode, c).Result;
            }
            else if (Id.Equals(UWPProjectionAnalyzer.ObjectID, StringComparison.OrdinalIgnoreCase))
            {
                return UWPProjection.ReplaceTypeAsync(newDoc, (IdentifierNameSyntax)idNode, c).Result;
            }
            return doc;
        }
    }
}


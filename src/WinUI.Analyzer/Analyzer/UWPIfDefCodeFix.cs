using System;
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
            if (!(await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is SyntaxNode root)) return;
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<Solution> ReplaceSolutionLineAsync(Document doc, SyntaxNode idNode, CancellationToken c)
        {
            var tag = $"{doc.Id}idNode";
            var newDoc = GetIfDefDoc(doc, idNode, tag, c).Result;
            // find the original node in the new tree using the tag
            if (!(newDoc.GetSyntaxRootAsync().Result is SyntaxNode root)) return doc.Project.Solution;
            idNode = root.GetAnnotatedNodes(tag).Single();
            return Observable.ReplaceObservableCollectionAsync(newDoc, (ObjectCreationExpressionSyntax)idNode, c).Result;
        }

        private async Task<Document> ReplaceDocumentLineAsync(Document doc, SyntaxNode idNode, string Id, CancellationToken c)
        {
            var tag = $"{doc.Id}idNode";
            var newDoc = GetIfDefDoc(doc, idNode, tag, c).Result;
            if (!(newDoc.GetSyntaxRootAsync().Result is SyntaxNode root)) return doc;
            idNode = root.GetAnnotatedNodes(tag).Single();
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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Returns the document with the original line surrounded by ifDef. 
        /// the original node can be found with the "originalContext" tag
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="idNode"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task<Document> GetIfDefDoc(Document doc, SyntaxNode idNode, string annotationTag, CancellationToken c)
        {
            // keep track of equals node with annotations
            // need to keep track of first node with annotations
            var syntaxAnnotation = new SyntaxAnnotation(annotationTag);
            var equalsAnnotated = idNode.WithAdditionalAnnotations(syntaxAnnotation);

            // replace it in the tree
            // return the new tree 
            if (!(await doc.GetSyntaxRootAsync(c) is SyntaxNode oldRoot)) return doc;
            if (!(oldRoot.ReplaceNode(idNode, equalsAnnotated) is SyntaxNode newRoot)) return doc;

            // find the entry node
            idNode = newRoot.GetAnnotatedNodes(annotationTag).Single();

            // Get span for line of original diagnostic location
            var testSpan = idNode.GetLocation().GetMappedLineSpan();
            var testPos = idNode.GetLocation().GetMappedLineSpan().StartLinePosition;
            var start = idNode.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
            var txt = doc.GetTextAsync().Result;
            var lineSpan = txt.Lines[start].Span;

            // get the line as text for the disabled portion of ifdef
            var disabledTxt = txt.GetSubText(lineSpan).ToString();

            // find the first node in that line
            var firstNode = idNode;
            var parentNode = idNode.Parent;
            while (parentNode != null && parentNode.SpanStart >= lineSpan.Start)
            {
                firstNode = parentNode;
                parentNode = firstNode.Parent;
            }

            // need to keep track of first node with annotations
            var firstNodeTag = "firstNode";
            syntaxAnnotation = new SyntaxAnnotation(firstNodeTag);
            var firstAnnotated = firstNode.WithAdditionalAnnotations(syntaxAnnotation);

            // replace it in the tree
            newRoot = newRoot.ReplaceNode(firstNode, firstAnnotated);

            // find the reference to the updated node in tree
            firstNode = newRoot.GetAnnotatedNodes(firstNodeTag).Single();

            // get first identifier token
            var firstIdToken = (SyntaxToken)firstNode.DescendantNodesAndTokensAndSelf().Where(t => t.IsToken).FirstOrDefault();

            // Generate the trivia for if def
            var ifDefTrivia = SyntaxFactory.TriviaList(
                new[]{
                    SyntaxFactory.Trivia(
                        SyntaxFactory.IfDirectiveTrivia(
                            SyntaxFactory.IdentifierName(" WINDOWS_UWP"), // remove?
                            true, false, false).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed))});

            // keep leading trivia if any
            if (firstIdToken.HasLeadingTrivia)
            {
                ifDefTrivia = ifDefTrivia.AddRange(firstIdToken.LeadingTrivia);
            }

            // replace its trivia
            var firstIdWithTrivia = firstIdToken.WithLeadingTrivia(ifDefTrivia);

            // return the new tree 
            newRoot = newRoot.ReplaceToken(firstIdToken, firstIdWithTrivia);


            // use annotation to get the firstNode again
            firstNode = newRoot.GetAnnotatedNodes(firstNodeTag).Single();

            if (!(firstNode.Parent is SyntaxNode firstParent)) return doc;

            // need to use firstIdWithTrivia to get nodes now old nodes belong to the old tree
            // get next sibling...
            var allSiblings = firstParent.ChildNodesAndTokens();
            SyntaxNodeOrToken nextSibling = null;
            for (var i = 0; i < allSiblings.Count(); i++)
            {
                var current = allSiblings.ElementAt(i);
                if (current.Equals(firstNode))
                {
                    nextSibling = allSiblings.ElementAt(i + 1);
                    break;
                }
            }

            // Generate the trivia for else end ifdef
            var endDefTrivia = SyntaxFactory.TriviaList(
                new[]{
                    SyntaxFactory.Trivia(SyntaxFactory.ElseDirectiveTrivia(
                        SyntaxFactory.Token(SyntaxKind.HashToken),
                        SyntaxFactory.Token(SyntaxKind.ElseKeyword),
                        SyntaxFactory.Token(SyntaxKind.EndOfDirectiveToken).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                        false,
                        false
                    )),
                    SyntaxFactory.DisabledText($"{disabledTxt}{SyntaxFactory.ElasticCarriageReturnLineFeed}"),
                    SyntaxFactory.Trivia(SyntaxFactory.EndIfDirectiveTrivia(
                        SyntaxFactory.Token(SyntaxKind.HashToken),
                        SyntaxFactory.Token(SyntaxKind.EndIfKeyword),
                        SyntaxFactory.Token(SyntaxKind.EndOfDirectiveToken).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                        false
                    ))
                });


            // keep leading trivia if any
            if (nextSibling.HasLeadingTrivia)
            {
                endDefTrivia = endDefTrivia.AddRange(nextSibling.GetLeadingTrivia());
            }

            // attach 
            var newSibling = nextSibling.WithLeadingTrivia(endDefTrivia);

            if (nextSibling != null && nextSibling.IsNode)
            {
                newRoot = newRoot.ReplaceNode((SyntaxNode)nextSibling, (SyntaxNode)newSibling);
            }
            else
            {
                newRoot = newRoot.ReplaceToken((SyntaxToken)nextSibling, (SyntaxToken)newSibling);
            }
            return doc.WithSyntaxRoot(newRoot);
        }
    }
}


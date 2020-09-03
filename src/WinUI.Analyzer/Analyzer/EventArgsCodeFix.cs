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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventArgsCodeFix)), Shared]
    public class EventArgsCodeFix : CodeFixProvider
    {
        private const string title = "Update Event Args";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EventArgsAnalyzer.Use_ID, EventArgsAnalyzer.Param_ID); }
        }

        // an optional overide to fix all error occurences instead of just one.
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpanSrc = diagnostic.Location.SourceSpan;
            if (root.FindNode(diagnosticSpanSrc) is SyntaxNode srcNode)
            {
                if (diagnostic.Id == EventArgsAnalyzer.Param_ID)
                {
                    context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceEventArgsAsync(context.Document, (ParameterSyntax)srcNode, c),
                        equivalenceKey: title),
                    diagnostic);
                }
                else if (diagnostic.Id == EventArgsAnalyzer.Use_ID)
                {
                    context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => ReplaceUWPUseAsync(context.Document, srcNode, c),
                        equivalenceKey: title),
                    diagnostic);
                }
            }
        }

        /// <summary>
        ///  Given an instance of OnLaunched Method with param type LaunchActivatedEventArgs, 
        ///  explicitly updates to Microsoft.UI.Xaml.LaunchActivatedEventArgs
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="eventNode"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task<Document> ReplaceEventArgsAsync(Document doc, ParameterSyntax paramNode, CancellationToken c)
        {
            var replaceNode = SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("Microsoft"), SyntaxFactory.IdentifierName("UI")),
                    SyntaxFactory.IdentifierName("Xaml")),
                SyntaxFactory.IdentifierName("LaunchActivatedEventArgs"));
            var parChild = paramNode.ChildNodes().First();
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            if (oldRoot == null) return doc;
            var newRoot = oldRoot.ReplaceNode(parChild, replaceNode);
            return doc.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Given a OnLaunched Method, replaces all param usage ie LaunchActivatedEventArgs e,
        /// with e.UWPLaunchActivatedEventArgs
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="tarNode"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task<Document> ReplaceUWPUseAsync(Document doc, SyntaxNode tarNode, CancellationToken c)
        {
            var idNode = tarNode.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
            var idName = idNode.TryGetInferredMemberName();
            if (idName == null) return doc;
            var replaceNode = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(idName),
                SyntaxFactory.IdentifierName("UWPLaunchActivatedEventArgs"));
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            if (oldRoot == null) return doc;
            var newRoot = oldRoot.ReplaceNode(idNode, replaceNode);
            return doc.WithSyntaxRoot(newRoot);
        }
    }
}


using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using System.ComponentModel.Design;
using System;

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
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpanSrc = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == EventArgsAnalyzer.Param_ID)
            {
                var t = root.FindNode(diagnosticSpanSrc) as ParameterSyntax;
                context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceEventArgsAsync(context.Document, t, c),
                    equivalenceKey: title),
                diagnostic);
            }
            else if (diagnostic.Id == EventArgsAnalyzer.Use_ID)
            {
                var t = root.FindNode(diagnosticSpanSrc);
                context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceUWPUseAsync(context.Document, t, c),
                    equivalenceKey: title),
                diagnostic);
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
            var replaceNode = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(idName),
                SyntaxFactory.IdentifierName("UWPLaunchActivatedEventArgs"));
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(idNode, replaceNode);
            return doc.WithSyntaxRoot(newRoot);
        }
    }
}


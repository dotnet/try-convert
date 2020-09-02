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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObservableCollectionCodeFix)), Shared]
    public class ObservableCollectionCodeFix : CodeFixProvider
    {
        private const string title = "Fix ObservableCollection";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ObservableCollectionAnalyzer.ID); }
        }

        // an optional overide to fix all occurences instead of just one.
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
            if (!(root.FindNode(diagnosticSpanSrc) is ObjectCreationExpressionSyntax idNode)) return;
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => ReplaceObservableCollectionAsync(context.Document, idNode, c),
                    equivalenceKey: title),
                diagnostic);

        }
        internal async Task<Solution> ReplaceObservableCollectionAsync(Document doc, ObjectCreationExpressionSyntax idNode, CancellationToken c)
        {
            var project = doc.Project;
            var originalSolution = project.Solution;
            var workspace = originalSolution.Workspace;
            var newSolution = GetCollectionReplacement(project, originalSolution);
            // replace object creation and declaration with the new type
            if (!(idNode.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault() is GenericNameSyntax genericNode)) return originalSolution;
            if (!(genericNode.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault() is TypeArgumentListSyntax typeArgList)) return originalSolution;
            var newGenericNode = SyntaxFactory.GenericName(SyntaxFactory.Identifier("AppUIBasics.ObservableCollection")).WithTypeArgumentList(typeArgList);
            // get new docs
            if (!(newSolution.GetDocument(doc.Id) is Document newDoc)) return originalSolution;
            if (!(await newDoc.GetSyntaxRootAsync(c) is SyntaxNode oldRoot)) return originalSolution;
            var newRoot = oldRoot.ReplaceNode(genericNode, newGenericNode);
            if (!idNode.Parent.IsKind(SyntaxKind.Argument))
            {
                // need to also replace the other kind get variable declaration
                var declaration = newRoot.FindNode(idNode.Span).Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
                if (declaration != null)
                {
                    // replace generic
                    var declareGeneric = declaration.ChildNodes().OfType<GenericNameSyntax>().FirstOrDefault();
                    if (declareGeneric != null)
                    {
                        // need to check for trivia first and keep any
                        if (declareGeneric.HasLeadingTrivia)
                        {
                            newGenericNode = newGenericNode.WithLeadingTrivia(declareGeneric.GetLeadingTrivia());
                        }
                        newRoot = newRoot.ReplaceNode(declareGeneric, newGenericNode);
                    }
                }
            }
            newSolution = newDoc.WithSyntaxRoot(newRoot).Project.Solution;
            return newSolution;
        }

        private Solution GetCollectionReplacement(Project project, Solution solution)
        {
            //TODO, check if class exists instead?
            // use type instead of this.
            foreach (var d in project.Documents)
            {
                if (d.Name.Equals("CollectionsInterop.cs"))
                {
                    var dText = d.GetTextAsync().Result.ToString();
                    if (dText.Equals(Utils.GetCollectionString()))
                    {
                        return solution;
                    }
                }
            }
            // Create new doc, did not exist
            var newDocID = DocumentId.CreateNewId(project.Id, "testID");
            return solution.AddAdditionalDocument(newDocID, "CollectionsInterop.cs", Utils.GetCollectionString());
        }
    }
}


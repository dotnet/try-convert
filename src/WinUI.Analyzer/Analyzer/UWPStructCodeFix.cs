using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinUI.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UWPStructCodeFix)), Shared]
    public class UWPStructCodeFix : CodeFixProvider
    {
        private const string title = "Replace Struct With Helper Class";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(UWPStructAnalyzer.ID); }
        }

        // an optional overide to fix all occurences instead of just one.
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!(await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is SyntaxNode root)) return ;
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpanSrc = diagnostic.Location.SourceSpan;
            var idNode = root.FindNode(diagnosticSpanSrc);
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceStructAsync(context.Document, idNode, c),
                    equivalenceKey: title),
                diagnostic);
        }

        internal async Task<Document> ReplaceStructAsync(Document doc, SyntaxNode node, CancellationToken c)
        {
            // Need to get old argument list
            var creationNode = node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            if (creationNode == null) return doc;
            var argList = creationNode.DescendantNodes().OfType<ArgumentListSyntax>().First();
            if (argList == null) return doc;
            // Need to find the creation syntax, metadata and decide which version to create
            // ObjectCreationExpressionSyntax node = equalsClause.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
            if (!(doc.GetSemanticModelAsync().Result is SemanticModel model)) return doc;
            if (!(model.GetTypeInfo(creationNode).Type is ITypeSymbol objectType)) return doc;
            var compilation = model.Compilation;
            // var structs = UWPStructAnalyzer.Structs;
            if (!UWPStructAnalyzer.Structs.TryGetValue(objectType.ToString(), out var helper)) return doc;
            var newCreation = getEqualsClause(helper, argList, model, compilation);
            // need a new class to decide how to replace
            if (newCreation == null) return doc;
            if (!(await doc.GetSyntaxRootAsync(c) is SyntaxNode oldRoot)) return doc;
            var newRoot = oldRoot.ReplaceNode(creationNode, newCreation);
            return newRoot != null ? doc.WithSyntaxRoot(newRoot) : doc;
        }

        /// <summary>
        /// Creates an Equivelant node if possible, Returns Null if matching version does not exist
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="argList"></param>
        /// <returns></returns>
        private InvocationExpressionSyntax? getEqualsClause(string helper, ArgumentListSyntax argList, SemanticModel model, Compilation compilation)
        {
            var regex = new Regex(@"([^.]+$)");
            var helperName = regex.Match(helper).Value;
            var helperMethod = "";
            // decide which methods to create from arguments and types
            if (helperName.Equals("CornerRadiusHelper"))
            {
                if (argList.Arguments.Count == 4)
                {
                    helperMethod = "FromRadii";
                }
                else if (argList.Arguments.Count == 1)
                {
                    helperMethod = "FromUniformRadius";
                }
                else
                {
                    return null;
                }
            }
            else if (helperName.Equals("DurationHelper"))
            {
                helperMethod = "FromTimeSpan";
            }
            else if (helperName.Equals("GridLengthHelper"))
            {
                if (argList.Arguments.Count == 2)
                {
                    helperMethod = "FromValueAndType";
                }
                else if (argList.Arguments.Count == 1)
                {
                    helperMethod = "FromPixels";
                }
                else
                {
                    return null;
                }
            }
            else if (helperName.Equals("ThicknessHelper"))
            {
                if (argList.Arguments.Count == 4)
                {
                    helperMethod = "FromLengths";
                }
                else if (argList.Arguments.Count == 1)
                {
                    helperMethod = "FromUniformLength";
                }
                else
                {
                    return null;
                }
            }
            else if (helperName.Equals("GeneratorPositionHelper"))
            {
                // Lives in Windows.UI.Xaml.Controls.Primitives
                helperName = "Controls.Primitives.GeneratorPositionHelper";
                if (argList.Arguments.Count == 2)
                {
                    helperMethod = "FromIndexAndOffset";
                }
                else
                {
                    return null;
                }
            }
            else if (helperName.Equals("MatrixHelper"))
            {
                // Lives in Windows.UI.Xaml.Media
                helperName = "Media.MatrixHelper";
                if (argList.Arguments.Count == 6)
                {
                    helperMethod = "FromElements";
                }
                else
                {
                    return null;
                }
            }
            else if (helperName.Equals("KeyTimeHelper"))
            {
                // Lives in Windows.UI.Xaml.Media.Animation
                helperName = "Media.Animation.KeyTimeHelper";
                helperMethod = "FromTimeSpan";
            }
            else if (helperName.Equals("RepeatBehaviorHelper"))
            {
                // Lives in windows.ui.xaml.media.animation
                helperName = "Media.Animation.RepeatBehaviorHelper";
                if (argList.Arguments.Count == 1)
                {
                    var arg1 = argList.Arguments.First().Expression;
                    if (arg1 is MemberAccessExpressionSyntax)
                    {
                        var arg2 = ((MemberAccessExpressionSyntax)arg1).Expression;
                        var realtype = model.GetTypeInfo(arg2).Type;
                    }
                    var argType = model.GetTypeInfo(argList.Arguments.First().Expression).Type;
                    var doub = compilation.GetSpecialType(SpecialType.System_Double);
                    var intType = compilation.GetSpecialType(SpecialType.System_Int32);
                    var uintType = compilation.GetSpecialType(SpecialType.System_UInt32);
                    var timeSpans = Utils.GetTypesByMetadataName(compilation, "System.TimeSpan");
                    if (SymbolEqualityComparer.Default.Equals(doub, argType)
                        || SymbolEqualityComparer.Default.Equals(uintType, argType)
                        || SymbolEqualityComparer.Default.Equals(intType, argType))
                    {
                        helperMethod = "FromCount";
                    }
                    else if (timeSpans.Any(t => SymbolEqualityComparer.Default.Equals(t, argType)))
                    {
                        helperMethod = "FromDuration";
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("Microsoft"),
                                    SyntaxFactory.IdentifierName("UI")),
                                SyntaxFactory.IdentifierName("Xaml")),
                            SyntaxFactory.IdentifierName(helperName)),
                        SyntaxFactory.IdentifierName(helperMethod)))
                .WithArgumentList(argList);
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using System.Threading;

/**
 * Reads Txt files and returns an array of strings representing valid and invalid namespaces
 */
namespace WinUI.Analyzer
{
    class Utils
    {
        internal static string[] GetNamespaceNames()
        {
            StringReader sr = new StringReader(Resources.namespaceAnalyzer);
            var strs = sr.ReadToEnd();
            var strArr = strs.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            return strArr;
        }

        internal static String[] GetDeprecatedNames()
        {
            StringReader sr = new StringReader(Resources.deprecatedUseAnalyzer);
            var strs = sr.ReadToEnd();
            var strArr = strs.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            return strArr;
        }

        internal static string GetCollectionString()
        {
            StringReader sr = new StringReader(Resources.CollectionsInterop);
            return sr.ReadToEnd();
        }

        //Navigate up the tree to the full QualifiedId name.
        internal static String GetFullID(QualifiedNameSyntax node)
        {
            //get to the top level QualifiedName Node parent
            while (node.Parent.IsKind(SyntaxKind.QualifiedName))
            {
                node = (QualifiedNameSyntax)node.Parent;
            }
            //Return a string rep of the fully Qualified Name
            return $"{node.Left}.{node.Right}";
        }

        /// <summary>
        /// Returns True if symbol inherits from a base type
        /// </summary>
        /// <param name="symbol">Symbol to investigate</param>
        /// <param name="type">BaseType to match</param>
        /// <returns></returns>
        internal static bool InheritsFrom(INamedTypeSymbol symbol, ITypeSymbol type)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (type.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Returns the Fully Qualified name of a type
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static string GetFullDisplayString(ITypeSymbol symbol)
        {
            return symbol.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        }

        /// <summary>
        /// The method GetTypeByMetadataName returns null if a type is defined in 2 different assemblies.
        /// This method returns all types that match a full name
        /// (you have to look at all assemblies and call GetTypeByMetadataName per assembly.)
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="typeMetadataName"></param>
        /// <returns></returns>
        internal static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(Compilation compilation, string typeMetadataName)
        {
            return compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
                .Where(t => t != null);
        }

        /// <summary>
        /// Returns the document with the original line surrounded by ifDef. 
        /// the original node can be found with the "originalContext" tag
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="idNode"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static async Task<Document> GetIfDefDoc(Document doc, SyntaxNode idNode, string annotationTag, CancellationToken c)
        {
            // keep track of equals node with annotations
            // need to keep track of first node with annotations
            var syntaxAnnotation = new SyntaxAnnotation(annotationTag);
            var equalsAnnotated = idNode.WithAdditionalAnnotations(syntaxAnnotation);

            // replace it in the tree
            // return the new tree 
            var oldRoot = await doc.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(idNode, equalsAnnotated);

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
            SyntaxNode firstNode = idNode;
            SyntaxNode parentNode = idNode.Parent;
            while (parentNode.SpanStart >= lineSpan.Start)
            {
                firstNode = parentNode;
                parentNode = firstNode.Parent;
            }

            // need to keep track of first node with annotations
            string firstNodeTag = "firstNode";
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

            // need to use firstIdWithTrivia to get nodes now old nodes belong to the old tree
            // get next sibling...
            var allSiblings = firstNode.Parent.ChildNodesAndTokens();
            SyntaxNodeOrToken nextSibling = null;
            for (int i = 0; i < allSiblings.Count(); i++)
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

            if (nextSibling.IsNode)
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

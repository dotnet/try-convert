using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        internal static IEnumerable<INamedTypeSymbol?> GetTypesByMetadataName(Compilation compilation, string typeMetadataName)
        {
            return compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
                .Where(t => t != null);
        }
    }
}

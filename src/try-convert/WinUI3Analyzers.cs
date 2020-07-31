using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.Build.Locator;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Security.AccessControl;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.WinUI.Convert;
//using VSDiagnostics.Diagnostics.Async.AsyncMethodWithoutAsyncSuffix;
//using VSDiagnostics.Diagnostics.Exceptions.ArgumentExceptionWithoutNameofOperator;

namespace MSBuild.Conversion
{
    class WinUI3Analyzers
    {
        const string solutionFilePath = @"C:\Users\t-estes\Desktop\CsProjs\OldCsProj\OldCsProj.sln";
        internal static DiagnosticAnalyzer[] GetAnalyzers()
        {
            // Get all analyzers
            //var assembly = Assembly.LoadFrom(@"C:\Users\t-estes\Desktop\try-convert\src\try-convert\temp\Analyzer.dll");
            return new[] { new Analyzer.NamespaceAnalyzer() };
        }

        internal static CodeFixProvider GetCodeFixer(DiagnosticAnalyzer analyzer)
        {
            return new Analyzer.NamespaceCodeFix();
        }

        internal static async Task RunWinUIAnalysis()
        {
            // The test solution is copied to the output directory when you build this sample.
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            // Open the solution within the workspace.
            Solution originalSolution = workspace.OpenSolutionAsync(solutionFilePath).Result;

            // Declare a variable to store the intermediate solution snapshot at each step.
            Solution newSolution = originalSolution;
            if (newSolution == null) return;

            //Get an array of all the analyzers to apply
            var analyzers = GetAnalyzers();

            // Note how we can't simply iterate over originalSolution.Projects or project.Documents
            // because it will return objects from the unmodified originalSolution, not from the newSolution.
            // We need to use the ProjectIds and DocumentIds (that don't change) to look up the corresponding
            // snapshots in the newSolution.
            foreach (ProjectId projectId in originalSolution.ProjectIds)
            {
                // Look up the snapshot for the original project in the latest forked solution.
                var project = newSolution.GetProject(projectId);

                if (project == null) continue;
                foreach (DocumentId documentId in project.DocumentIds)
                {
                    if (documentId == null) continue;
                    // Look up the snapshot for the original document in the latest forked solution.
                    Document document = newSolution.GetDocument(documentId);
                    if (document == null) continue;
                    foreach (var analyzer in analyzers)
                    {
                        var codeFixProvider = GetCodeFixer(analyzer);
                        //Get all instances of that analyzer in the document
                        var analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
                        var attempts = analyzerDiagnostics.Count();
                        //apply the changes to the document 
                        for (int i = 0; i < attempts; ++i)
                        {
                            var actions = new List<CodeAction>();
                            var context = new CodeFixContext(document, analyzerDiagnostics.First(), (a, d) => actions.Add(a), CancellationToken.None);
                            codeFixProvider.RegisterCodeFixesAsync(context).Wait();

                            if (!actions.Any())
                            {
                                break;
                            }

                            document = ApplyFix(document, actions.ElementAt(0));


                            //apply the new document to the solution
                            analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
                            //check if there are analyzer diagnostics left after the code fix
                            if (!analyzerDiagnostics.Any())
                            {
                                break;
                            }
                        }
                        // Store the solution implicitly constructed in the previous step as the latest
                        // one so we can continue building it up in the next iteration.
                        newSolution = document.Project.Solution;
                    }
                }


                // Actually apply the accumulated changes and save them to disk. At this point
                // workspace.CurrentSolution is updated to point to the new solution.
                if (workspace.TryApplyChanges(newSolution))
                {
                    Console.WriteLine("Solution updated.");
                }
                else
                {
                    Console.WriteLine("Update failed!");
                }
            }
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        internal static IEnumerable<Diagnostic> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            // get hashest of all the documents
            var projects = new HashSet<Microsoft.CodeAnalysis.Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            // create a list to hold all the diagnostics
            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                // for each project, get compilation and pull analyzer use in that compilation
                var compilationWithAnalyzers = project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        internal static IEnumerable<Diagnostic> SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        internal static Document ApplyFix(Document document, CodeAction codeAction)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }
    }
}

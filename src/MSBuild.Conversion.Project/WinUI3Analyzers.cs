using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuild.Conversion.Project
{
    public class WinUI3Analyzers
    {
        // make an enum for the type
        public enum ProjectOutputType { UWPApp, ClassLibrary, DesktopApp};
        private ProjectOutputType projectType;

        // metadata refrences
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
#pragma warning disable ConvertNamespace // Windows Namespace should be Microsoft
        private static readonly MetadataReference WindowsXamlReference = MetadataReference.CreateFromFile(typeof(Windows.UI.Xaml.DependencyObject).Assembly.Location);
#pragma warning restore ConvertNamespace // Windows Namespace should be Microsoft
        private static readonly MetadataReference MicrosoftXamlReference = MetadataReference.CreateFromFile(typeof(Microsoft.UI.Xaml.DependencyObject).Assembly.Location);
        private static readonly MetadataReference INotifyReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location);

        public WinUI3Analyzers(ProjectOutputType projectType)
        {
            this.projectType = projectType;
        }

        internal DiagnosticAnalyzer[] GetAnalyzers()
        {
            // Get all analyzers, Order Matters!
            return new DiagnosticAnalyzer[] {// new Analyzer.UWPStructAnalyzer(), new Analyzer.UWPProjectionAnalyzer(), 
                new Analyzer.EventArgsAnalyzer() , new Analyzer.NamespaceAnalyzer() };
            // Cannot create new Documents in this workspace, Analyzer will not work: new Analyzer.ObservableCollectionAnalyzer()
        }

        internal CodeFixProvider[] GetCodeFixes()
        {
            return new CodeFixProvider[] { }; // remove this after testing
            if (projectType == ProjectOutputType.ClassLibrary)
            {
                // return ifDefFixes for libraries instead, passing in false ensures ObservableCollectionFix is not implemented
                return new CodeFixProvider[] {// new Analyzer.UWPIfDefCodeFix(true),
                    new Analyzer.EventArgsCodeFix(), new Analyzer.NamespaceCodeFix() };
            }
            else
            {
                return new CodeFixProvider[] {// new Analyzer.UWPStructCodeFix(), new Analyzer.UWPProjectionCodeFix(),
                    new Analyzer.EventArgsCodeFix(), new Analyzer.NamespaceCodeFix() };
            }
            // Will not currently work : new Analyzer.ObservableCollectionCodeFix()
        }

        internal CodeFixProvider? GetCodeFixer(DiagnosticAnalyzer analyzer)
        {
            var codeFixes = GetCodeFixes();
            var v = analyzer.SupportedDiagnostics;
            foreach (var c in codeFixes)
            {
                if (v.Any(a => c.FixableDiagnosticIds.Contains(a.Id)))
                {
                    return c;
                }
            }
            return null;         
        }

        public async Task RunWinUIAnalysis(string projectFilePath)
        {
            Console.WriteLine($"Running Analyzers on {projectFilePath}");
            // The test solution is copied to the output directory when you build this sample.
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            //workspace.LoadMetadataForReferencedProjects = true;
            
            // Open the solution within the workspace.
            Microsoft.CodeAnalysis.Project originalProject = workspace.OpenProjectAsync(projectFilePath).Result;

            // try add metadata to solution
            var withMeta = originalProject
                .AddMetadataReference(CorlibReference)
                .AddMetadataReference(SystemCoreReference)
                .AddMetadataReference(CSharpSymbolsReference)
                .AddMetadataReference(CodeAnalysisReference)
                .AddMetadataReference(WindowsXamlReference)
                .AddMetadataReference(MicrosoftXamlReference)
                .AddMetadataReference(INotifyReference);

            // Declare a variable to store the intermediate solution snapshot at each step.
            Microsoft.CodeAnalysis.Project? newProject = withMeta;
            if (newProject == null)
            {
                Console.WriteLine("Error Running Conversion Analyzers. Exiting...");
                return;
            }
            //Get an array of all the analyzers to apply
            var analyzers = GetAnalyzers();

            // Note how we can't simply iterate over originalSolution.Projects or project.Documents
            // because it will return objects from the unmodified originalSolution, not from the newSolution.
            // We need to use the ProjectIds and DocumentIds (that don't change) to look up the corresponding
            // snapshots in the newSolution.
            int count = -1;
            foreach (DocumentId documentId in newProject.DocumentIds)
            {
                count++;
                if (documentId == null) continue;
                // Look up the snapshot for the original document in the latest forked solution.
#nullable disable
                Document document = newProject.GetDocument(documentId);
#nullable enable
                if (document == null) continue;
                Console.WriteLine($"Converting Document {count} of {newProject.DocumentIds.Count}");
                foreach (var analyzer in analyzers)
                {
                    Console.WriteLine($"Running {analyzer.GetType().Name} on {document.FilePath}");

                    var codeFixProvider = GetCodeFixer(analyzer);
                    if (codeFixProvider == null) continue;
                    //Get all instances of that analyzer in the document
                    IEnumerable<Diagnostic> analyzerDiagnostics;
                    analyzerDiagnostics = GetSortedDiagnosticsFromDocument(analyzer, document, newProject);
                    var attempts = analyzerDiagnostics.Count();
                    //apply the changes to the document use total initial diagnostics as max attempts
                    for (int i = 0; i < attempts; ++i)
                    {
                        var actions = new List<CodeAction>();
                        var context = new CodeFixContext(document, analyzerDiagnostics.First(), (a, d) => actions.Add(a), CancellationToken.None);
                        codeFixProvider.RegisterCodeFixesAsync(context).Wait();
                        if (!actions.Any())
                        {
                            break;
                        }
                        //Apply fix and store in new solution
                        document = ApplyFix(document, actions.ElementAt(0));
                        newProject = document.Project;
                        //apply the new document to the solution
                        analyzerDiagnostics = GetSortedDiagnosticsFromDocument(analyzer, document, newProject);
                        //check if there are analyzer diagnostics left after the code fix
                        if (!analyzerDiagnostics.Any())
                        {
                            break;
                        }
                    }
                }
            }
            // Remove Metadata references to avoid errors
            newProject = newProject
                .RemoveMetadataReference(CorlibReference)
                .RemoveMetadataReference(SystemCoreReference)
                .RemoveMetadataReference(CSharpSymbolsReference)
                .RemoveMetadataReference(CodeAnalysisReference)
                .RemoveMetadataReference(WindowsXamlReference)
                .RemoveMetadataReference(MicrosoftXamlReference)
                .RemoveMetadataReference(INotifyReference);

            // Actually apply the accumulated changes and save them to disk. At this point
            // workspace.CurrentSolution is updated to point to the new solution.
            try
            {
                workspace.TryApplyChanges(newProject.Solution);
                Console.WriteLine("Solution updated.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not update solution!");
                Console.WriteLine(e.Message);
            }
            
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        internal IEnumerable<Diagnostic> GetSortedDiagnosticsFromDocument(DiagnosticAnalyzer analyzer, Document document, Microsoft.CodeAnalysis.Project project)
        {
            // create a list to hold all the diagnostics
            var diagnostics = new List<Diagnostic>();
            var tree = document.GetSyntaxTreeAsync().Result;
            // get compilation and pull analyzer use in that compilation
            var comp = project.GetCompilationAsync().Result;
            if (comp == null) return diagnostics;
            ImmutableArray<Diagnostic> diags;
            try
            {
                var compilationWithAnalyzers = comp.WithAnalyzers(ImmutableArray.Create(analyzer));
                diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            }
            catch (Exception e)
            {
                return diagnostics;
            }
            
            foreach (var diag in diags)
            {
                if (diag.Location == Location.None || diag.Location.IsInMetadata)
                {
                    diagnostics.Add(diag);
                }
                else
                {
                    if (tree == diag.Location.SourceTree)
                    {
                        diagnostics.Add(diag);
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
        internal IEnumerable<Diagnostic> SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        /// <summary>
        /// Apply codefix to document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="codeAction"></param>
        /// <returns></returns>
        internal Document ApplyFix(Document document, CodeAction codeAction)
        {
            try
            {
                var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
                var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                return solution.GetDocument(document.Id);
            } 
            catch (Exception e)
            {
                return document;
            }
        }
    }
}

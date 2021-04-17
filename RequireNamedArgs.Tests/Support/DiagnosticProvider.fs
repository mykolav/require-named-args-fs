namespace RequireNamedArgs.Tests.Support


open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.Tests.Support.DocumentFactory


module DiagnosticProvider =
    
    
    /// <summary>
    /// Extensions for turning strings into documents and getting the diagnostics on them
    /// </summary>
    type DiagnosticAnalyzer with
        
        
        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="lang">The language the source classes are in</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        member analyzer.GetSortedDiagnostics(lang: Langs, sources: seq<string>) =
            analyzer.GetSortedDiagnosticsFromDocs(mkDocuments(sources, lang))

        
        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        member analyzer.GetSortedDiagnosticsFromDocs(documents: Document list) =
            let shouldTake (diag: Diagnostic) =
                let loc = diag.Location
                if loc = Location.None || loc.IsInMetadata then true
                else documents 
                     |> Seq.exists (fun doc -> doc.GetSyntaxTreeAsync().Result = loc.SourceTree)

            let compilations =
               documents 
               |> Seq.map (fun doc -> doc.Project) 
               |> Seq.distinct
               |> Seq.map (fun proj -> proj.GetCompilationAsync().Result)

            // For details, see https://stackoverflow.com/a/54129600/818321                       
            let compilationErrors =
                compilations
                |> Seq.collect (fun c -> c.GetDiagnostics() |> Seq.where (fun d -> d.Severity = DiagnosticSeverity.Error))
                |> Seq.sortBy (fun d -> d.Location.SourceSpan.Start)
                |> List.ofSeq
                
            if compilationErrors.Length > 0
            then
                compilationErrors
            else
                                           
            let sortedDiags =
               compilations
               |> Seq.map (fun compilation -> compilation.WithAnalyzers(ImmutableArray.Create(analyzer)))
               |> Seq.collect (fun compilation -> compilation.GetAnalyzerDiagnosticsAsync()
                                                             .Result)
               |> Seq.filter shouldTake
               |> Seq.sortBy (fun diag -> diag.Location.SourceSpan.Start)
               |> List.ofSeq
               
            sortedDiags

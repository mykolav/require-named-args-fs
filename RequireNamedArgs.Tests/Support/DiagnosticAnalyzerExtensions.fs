namespace RequireNamedArgs.Tests.Support


open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.Tests.Support


[<AutoOpen>]
module DiagnosticAnalyzerExtensions =


    type DiagnosticAnalyzer
    with


        member analyzer.Analyze(language: Document.Language, sources: seq<string>)
                               : Diagnostic[] =
            analyzer.Analyze(Document.From(language, sources))


        member analyzer.Analyze(documents: seq<Document>)
                               : Diagnostic[] =

            let compilations =
               documents
               |> Seq.map (_.Project)
               |> Seq.distinct
               |> Seq.map (_.GetCompilationAsync().Result)

            // For details, see https://stackoverflow.com/a/54129600/818321
            let compilationErrors =
                compilations
                |> Seq.collect (fun compilation ->
                       compilation.GetDiagnostics()
                       |> Seq.where (fun diagnostic -> diagnostic.Severity = DiagnosticSeverity.Error))
                |> Seq.sortBy (_.Location.SourceSpan.Start)
                |> Array.ofSeq

            if compilationErrors.Length > 0
            then
                compilationErrors
            else

            let analyzerDiagnostics =
               compilations
               |> Seq.map (_.WithAnalyzers(ImmutableArray.Create(analyzer)))
               |> Seq.collect (_.GetAnalyzerDiagnosticsAsync().Result)
               |> Seq.filter (fun diagnostic ->
                    let location = diagnostic.Location
                    location = Location.None ||
                    location.IsInMetadata ||
                    documents |> Seq.exists (fun document ->
                        document.GetSyntaxTreeAsync().Result = location.SourceTree))
               |> Seq.sortBy (_.Location.SourceSpan.Start)
               |> Array.ofSeq

            analyzerDiagnostics

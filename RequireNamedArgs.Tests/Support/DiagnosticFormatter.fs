module RequireNamedArgs.Tests.Support.DiagnosticFormatter

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Diagnostics
open System
open System.Text
open RequireNamedArgs.MaybeBuilder

/// <summary>
/// Helper method to format a Diagnostic into an easily readable string
/// </summary>
/// <param name="diagnostics">The Diagnostics to be formatted</param>
/// <returns>The Diagnostics formatted as a string</returns>
type DiagnosticAnalyzer with
    member analyzer.Format (diags: seq<Diagnostic>) = 
        let analyzerType = analyzer.GetType()
        let lastDiagPos = Seq.length diags - 1

        let tryFindRule (diag: Diagnostic) =
            analyzer.SupportedDiagnostics |> Seq.tryFind (fun rule -> rule.Id = diag.Id)

        let describe (descr: StringBuilder, i) (diag: Diagnostic, rule: DiagnosticDescriptor) =
            let descr = match diag.Location with
                        | loc when loc = Location.None -> 
                            descr.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id)
                        | loc when not loc.IsInSource  -> 
                            raise (NotSupportedException(
                                        "Test base does not currently handle diagnostics in metadata locations. " +
                                        "Diagnostic in metadata: " + (sprintf "%A" diag) + "\r\n")
                                  )
                        | loc -> 
                            let resultMethodName = if loc.SourceTree.FilePath.EndsWith(".cs") 
                                                   then "GetCSharpResultAt" 
                                                   else "GetBasicResultAt"
                            let linePos = loc.GetLineSpan().StartLinePosition
                            descr.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                                resultMethodName,
                                                linePos.Line + 1,
                                                linePos.Character + 1,
                                                analyzerType.Name,
                                                rule.Id)
            ((if i <> lastDiagPos 
              then descr.Append(',') 
              else descr).AppendLine(), i + 1)

        let (description, _) = diags |> Seq.choose (fun d -> tryFindRule d |>> fun r -> d, r) 
                                     |> Seq.fold describe (StringBuilder(), 0)
        description.ToString()

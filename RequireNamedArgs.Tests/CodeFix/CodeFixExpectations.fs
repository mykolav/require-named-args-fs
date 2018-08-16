module RequireNamedArgs.Tests.Support.CodeFixExpectations

[<RequireQualifiedAccess>]
module Expect =
    open System
    open System.Collections.Generic
    open System.Threading
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CodeActions
    open Microsoft.CodeAnalysis.CodeFixes
    open Microsoft.CodeAnalysis.Diagnostics
    open Microsoft.CodeAnalysis.Formatting
    open Expecto
    open DiagnosticProvider
    open DocumentExtensions
    open DocumentFactory
    open RequireNamedArgs.MaybeBuilder

    /// <summary>
    /// Compare two collections of Diagnostics 
    /// and return a list of any diagnostics that appear only in the second collection.
    /// Note: Considers Diagnostics to be the same if they have the same Ids.
    ///       In the case of multiple diagnostics with the same Id in a row,
    ///       this method may not necessarily return the new one.
    /// Note: This method was rewritten from scratch as opposed to translating from its C# counterpart.
    ///       It's not entirely clear how the original method was supposed to work.
    ///       For details see 
    ///         <see href="https://github.com/dotnet/roslyn-analyzers/issues/1787">this question in the roslyn-analyzers repo</see>
    /// </summary>
    /// <param name="diagnostics">The Diagnostics that existed in the code before the CodeFix was applied</param>
    /// <param name="newDiagnostics">The Diagnostics that exist in the code after the CodeFix was applied</param>
    let private getAddedDiags (prevDiags: Diagnostic seq) (curDiags: Diagnostic seq) =
        let prevDiags = prevDiags |> List.ofSeq |> List.sortBy (fun d -> d.Id, d.Location.SourceSpan.Start)
        let currDiags = curDiags |> List.ofSeq |> List.sortBy (fun d -> d.Id, d.Location.SourceSpan.Start)

        match prevDiags, currDiags with
        | [], [] -> []
        | _, []  -> []
        | [], _  -> currDiags
        | _      ->
            let rec compare (newDiags: Diagnostic list) currDiagPos prevDiagPos =
                let prevDiag = prevDiags.[prevDiagPos]
                let currDiag = currDiags.[currDiagPos]
                if currDiag.Id < prevDiag.Id then 
                    if currDiagPos = currDiags.Length - 1 
                    then currDiag::newDiags
                    else compare (currDiag::newDiags) (currDiagPos + 1) prevDiagPos
                elif currDiag.Id = prevDiag.Id then
                    if currDiagPos = currDiags.Length - 1 
                    then newDiags
                    else compare newDiags (currDiagPos + 1) prevDiagPos
                else // currDiag.Id > prevDiag.Id
                    if prevDiagPos = prevDiags.Length - 1 then 
                        if currDiagPos = currDiags.Length - 1 
                        then currDiag::newDiags
                        else compare (currDiag::newDiags) (currDiagPos + 1) prevDiagPos
                    else compare newDiags currDiagPos (prevDiagPos + 1)
            let newDiags = compare [] 0 0 |> List.sortBy (fun d -> d.Location.SourceSpan.Start)
            newDiags

    let private formatDiags (diags: Diagnostic list) =
        String.Join("\r\n", diags |> Seq.map (fun d -> d.ToString()))

    let private fixCode (analyzer: DiagnosticAnalyzer) 
                        (codeFixProvider: CodeFixProvider) 
                        (doc: Document) (analyzerDiags: Diagnostic list)
                        (codeFixIndex: int option) (allowNewCompilerDiags: bool) =
        let compilerDiags = doc.GetCompilerDiags()

        let getCodeActions() =
            let actions = List<CodeAction>()
            let context = CodeFixContext(doc, 
                                         analyzerDiags |> Seq.item 0, 
                                         registerCodeFix = Action<_, _>(fun a _ -> actions.Add(a)),
                                         cancellationToken = CancellationToken.None)
            codeFixProvider.RegisterCodeFixesAsync(context).Wait()
            actions

        let actions = getCodeActions()

        if not (Seq.any actions) then Some (doc, []) else
        if codeFixIndex.IsSome 
        then Some (doc.ApplyFix(actions.[codeFixIndex.Value]), []) else
        let doc = doc.ApplyFix(actions.[0])
        let analyzerDiags = analyzer.GetSortedDiagnosticsFromDocs([doc])
        let newCompilerDiags = getAddedDiags compilerDiags <| doc.GetCompilerDiags()

        //check if applying the code fix introduced any new compiler diagnostics
        if allowNewCompilerDiags || not (Seq.any newCompilerDiags)
        then Some (doc, analyzerDiags) else
        // Format and get the compiler diagnostics again so that the locations make sense in the output
        let doc = doc.WithSyntaxRoot(
                        Formatter.Format(doc.GetSyntaxRootAsync().Result, 
                                            Formatter.Annotation, 
                                            doc.Project.Solution.Workspace))
        let newCompilerDiags = getAddedDiags compilerDiags <| doc.GetCompilerDiags()
        Tests.failtestf "Fix introduced new compiler diagnostics:\r\n%s\r\n\r\nNew document:\r\n%s\r\n"
                        (formatDiags newCompilerDiags)
                        (doc.GetSyntaxRootAsync().Result.ToFullString())
        None

    /// <summary>
    /// General verifier for codefixes.
    /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
    /// Then gets the string after the codefix is applied and compares it with the expected result.
    /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
    /// </summary>
    /// <param name="analyzer">The analyzer to be applied to the source code</param>
    /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
    /// <param name="lang">The language the source code is in</param>
    /// <param name="originalSource">A class in the form of a string before the CodeFix was applied to it</param>
    /// <param name="expectedSource">A class in the form of a string after the CodeFix was applied to it</param>
    /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
    /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
    let toMatchFixedCode (analyzer: DiagnosticAnalyzer) 
                         (codeFixProvider: CodeFixProvider) 
                         (lang: Langs) (originalSource: string) 
                         (codeFixIndex: int option) (allowNewCompilerDiags: bool)
                         (expectedSource: string) =
        let doc = mkDocument(originalSource, lang)
        let analyzerDiags = analyzer.GetSortedDiagnosticsFromDocs([doc])
        let maxAttempts = analyzerDiags |> Seq.length

        let rec attemptToFixCode attemptsCount = maybe {
            if attemptsCount >= maxAttempts 
            then return doc else
            //check if there are analyzer diagnostics left after the code fix
            let! doc, analyzerDiags = fixCode analyzer codeFixProvider 
                                              doc analyzerDiags codeFixIndex 
                                              allowNewCompilerDiags
            if Seq.any analyzerDiags
            then return! attemptToFixCode <| attemptsCount + 1 
            else return doc
        }

        maybe {
            let! fixedDoc = attemptToFixCode 0
            let actualFixedSource = fixedDoc.ToSourceCode().Replace("\r\n", "\n")
            let expectedFixedSource = expectedSource.Replace("\r\n", "\n")
            return Expect.equal actualFixedSource expectedFixedSource ""
        } |> ignore

namespace RequireNamedArgs.Tests.CodeFix.Support


open System.Collections.Immutable
open System.Runtime.CompilerServices
open System.Threading
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Simplification


[<RequireQualifiedAccess>]
module private Seq =
    let ofType<'T> source =
        source |> Seq.choose (fun it -> match box it with
                                        | :? 'T as a -> Some a
                                        | _          -> None)


[<Sealed; AbstractClass; Extension>]
type DocumentExtensions private () =


    [<Extension>]
    static member WithApplied(this: Document, action: CodeAction): Document =
        let operations = action.GetOperationsAsync(CancellationToken.None).Result
        let solution = operations
                       |> Seq.cast
                       |> Seq.ofType<ApplyChangesOperation>
                       |> Seq.exactlyOne
                       |> _.ChangedSolution

        solution.GetDocument(this.Id)


    [<Extension>]
    static member GetDiagnostics(this: Document): ImmutableArray<Diagnostic> =
        this.GetSemanticModelAsync().Result.GetDiagnostics()


    [<Extension>]
    static member ToSourceCode(this: Document): string =
        let simplifiedDocument = Simplifier.ReduceAsync(this, Simplifier.Annotation).Result
        let syntaxRoot = simplifiedDocument.GetSyntaxRootAsync().Result

        let formattedSyntaxRoot = Formatter.Format(
            syntaxRoot,
            Formatter.Annotation,
            simplifiedDocument.Project.Solution.Workspace)

        formattedSyntaxRoot.GetText().ToString()

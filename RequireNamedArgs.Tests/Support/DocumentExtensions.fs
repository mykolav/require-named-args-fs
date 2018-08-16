module RequireNamedArgs.Tests.Support.DocumentExtensions

open System.Threading
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Simplification

type Document with
    /// <summary>
    /// Apply the inputted CodeAction to the inputted document.
    /// Meant to be used to apply codefixes.
    /// </summary>
    /// <param name="action">A CodeAction that will be applied to the Document.</param>
    /// <returns>A Document with the changes from the CodeAction</returns>
    member this.ApplyFix(action: CodeAction) =
        let operations = action.GetOperationsAsync(CancellationToken.None).Result
        let solution = operations |> Seq.cast
                                  |> Seq.ofType<ApplyChangesOperation>
                                  |> Seq.exactlyOne
                                  |> fun it -> it.ChangedSolution
        solution.GetDocument(this.Id)
    
    /// <summary>
    /// Get the existing compiler diagnostics on the inputted document.
    /// </summary>
    /// <param name="document">The Document to run the compiler diagnostic analyzers on</param>
    /// <returns>The compiler diagnostics that were found in the code</returns>
    member this.GetCompilerDiags() = this.GetSemanticModelAsync().Result.GetDiagnostics()

    /// <summary>
    /// Given a document, turn it into a string based on the syntax root
    /// </summary>
    /// <param name="document">The Document to be converted to a string</param>
    /// <returns>A string containing the syntax of the Document after formatting</returns>
    member this.ToSourceCode() =
        let simplifiedDoc = Simplifier.ReduceAsync(this, Simplifier.Annotation).Result
        let root = simplifiedDoc.GetSyntaxRootAsync().Result
        let formattedRoot = Formatter.Format(root, 
                                             Formatter.Annotation, 
                                             simplifiedDoc.Project.Solution.Workspace)
        formattedRoot.GetText().ToString()

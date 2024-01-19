module RequireNamedArgs.CodeFixProvider


open System.Collections.Immutable
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open FSharp.Control.Tasks.V2.ContextInsensitive
open RequireNamedArgs.Analysis
open RequireNamedArgs.Analyzer
open RequireNamedArgs.Analysis.SyntaxNodeArgumentExtensions
open RequireNamedArgs.Analysis.SemanticModelParameterInfoExtensions


[<ExportCodeFixProvider(LanguageNames.CSharp, Name = "RequireNamedArgsCodeFixProvider")>]
[<Shared>]
type public RequireNamedArgsCodeFixProvider() =
    inherit CodeFixProvider()


    static let title = "Use named args"


    // This tells the infrastructure that this code-fix provider corresponds to
    // the `RequireNamedArgsAnalyzer` analyzer.
    override val FixableDiagnosticIds = ImmutableArray.Create(DiagnosticDescriptors.NamedArgumentsRequired.Id)


    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md
    // for more information on Fix All Providers
    override this.GetFixAllProvider() = WellKnownFixAllProviders.BatchFixer


    override this.RegisterCodeFixesAsync(context) = (task {
        let! root = context.Document.GetSyntaxRootAsync(context.CancellationToken)
        let diagnostic = context.Diagnostics |> Seq.head
        let diagnosticSpan = diagnostic.Location.SourceSpan
        let syntaxNode = root.FindNode(diagnosticSpan)

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedDocument = fun cancellationToken -> task {
                    return! this.PrefixArgsWithNamesAsync(
                        context.Document,
                        root,
                        syntaxNode,
                        cancellationToken)
                },
                equivalenceKey = title),
            diagnostic)
        return ()
    } :> Task)


    member private this.PrefixArgsWithNamesAsync(document: Document,
                                                 root: SyntaxNode,
                                                 syntaxNode: SyntaxNode,
                                                 cancellationToken: CancellationToken)
                                                 : Task<Document> = task {

        let! sema = document.GetSemanticModelAsync(cancellationToken)

        let withNamedArguments (list: IArgumentListSyntax<'T>): Document =
            let parentSymbol = sema.GetSymbolInfo(list.Parent).Symbol

            let argumentWithNames =
                list.Arguments
                |> Seq.mapi (fun at a ->
                    match sema.GetParameterInfo(parentSymbol, at, a.NameColon) with
                    | Some pi -> a.WithNameColon(pi.Symbol.Name)
                    | None    -> a.Syntax)

            let listWithNamedArguments = list.WithArguments(argumentWithNames)

            // An argument list is an "addressable" syntax element, that we can directly
            // replace in the document's root.
            document.WithSyntaxRoot(root.ReplaceNode(list.Syntax, listWithNamedArguments.Syntax))

        // As it's the named args code fix provider,
        // the analyzer has already checked that it's OK to make these args named.
        match syntaxNode.ArgumentList with
        | None ->
            return document

        | Some argumentListSyntaxNode ->
            match argumentListSyntaxNode with
            | :? ArgumentListSyntaxNode as alsn           -> return withNamedArguments alsn
            | :? AttributeArgumentListSyntaxNode as aalsn -> return withNamedArguments aalsn
            | _                                           -> return document
    }

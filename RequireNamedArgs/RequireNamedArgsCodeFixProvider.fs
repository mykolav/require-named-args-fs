module RequireNamedArgs.CodeFixProvider


open System.Collections.Immutable
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
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


    static let Title = "Use named arguments"


    // This tells the infrastructure that this code-fix provider corresponds to
    // the `RequireNamedArgsAnalyzer` analyzer.
    override val FixableDiagnosticIds = ImmutableArray.Create(DiagnosticDescriptors.NamedArgumentsRequired.Id)


    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md
    // for more information on Fix All Providers
    override this.GetFixAllProvider() = WellKnownFixAllProviders.BatchFixer


    override this.RegisterCodeFixesAsync(context) = (task {
        let! root = context.Document.GetSyntaxRootAsync(context.CancellationToken)
        let diagnostic = Seq.head context.Diagnostics
        let diagnosticSpan = diagnostic.Location.SourceSpan
        let syntaxNode = root.FindNode(diagnosticSpan)

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                createChangedDocument = fun cancellationToken -> task {
                    return! this.UseNamedArguments(
                        context.Document,
                        root,
                        syntaxNode,
                        cancellationToken)
                },
                equivalenceKey = Title),
            diagnostic)
        return ()
    } :> Task)


    member private this.UseNamedArguments(document: Document,
                                          root: SyntaxNode,
                                          syntaxNode: SyntaxNode,
                                          ct: CancellationToken)
                                          : Task<Document> = task {

        // As we're inside of a code fix provider,
        // the analyzer has already checked that we should and can
        // make these arguments named.
        match syntaxNode.ArgumentList with
        | None ->
            return document

        | Some argumentListSyntaxNode ->
            match argumentListSyntaxNode with
            | :? IArgumentListSyntax<ArgumentSyntax> as list ->
                return! this.ReplaceWithNamedArguments(document, root, list, ct)

            | :? IArgumentListSyntax<AttributeArgumentSyntax> as list ->
                return! this.ReplaceWithNamedArguments(document, root, list, ct)

            | _ ->
                return document
    }


    member private _.ReplaceWithNamedArguments<'T when 'T :> SyntaxNode>(
        document: Document,
        root: SyntaxNode,
        list: IArgumentListSyntax<'T>,
        ct: CancellationToken)
        : Task<Document> = task {

        let! sema = document.GetSemanticModelAsync(ct)

        let parentSymbol = sema.GetSymbolInfo(list.Parent, ct).Symbol

        let argumentWithNames =
            list.Arguments
            |> Seq.mapi (fun at a ->
                match sema.GetParameterInfo(parentSymbol, at, a.NameColon) with
                | Some pi -> a.WithNameColon(pi.Symbol.Name)
                              .WithTriviaFrom(a.Syntax)
                | None    -> a.Syntax)

        let listWithNamedArguments = list.WithArguments(argumentWithNames)

        // An argument list is an "addressable" syntax element, that we can directly
        // replace in the document's root.
        return document.WithSyntaxRoot(
            root.ReplaceNode(list.Syntax, listWithNamedArguments.Syntax))
    }

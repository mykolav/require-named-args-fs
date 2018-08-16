module RequireNamedArgs.CodeFixProvider

open System.Collections.Immutable
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open FSharp.Control.Tasks.V2.ContextInsensitive
open RequireNamedArgs.Analyzer
open RequireNamedArgs.ParameterInfo
open RequireNamedArgs.InvocationExprSyntax

[<ExportCodeFixProvider(LanguageNames.CSharp, Name = "RequireNamedArgsCodeFixProvider")>]
[<Shared>]
type public RequireNamedArgsCodeFixProvider() = 
    inherit CodeFixProvider()

    static let title = "Use named args"
    
    // This tells the infrastructure that this code-fix provider corresponds to
    // the `RequireNamedArgsAnalyzer` analyzer.
    override val FixableDiagnosticIds = ImmutableArray.Create(RequireNamedArgsAnalyzer.DiagnosticId)

    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md 
    // for more information on Fix All Providers
    override this.GetFixAllProvider() = WellKnownFixAllProviders.BatchFixer

    override this.RegisterCodeFixesAsync(context) = (task {
        let! root = context.Document.GetSyntaxRootAsync(context.CancellationToken)
        let diagnostic = context.Diagnostics |> Seq.head;
        let diagnosticSpan = diagnostic.Location.SourceSpan;
        let invocationExprSyntax = root.FindNode(diagnosticSpan) :?> InvocationExpressionSyntax;

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedDocument = fun cancellationToken -> task {
                    return! this.RequireNamedArgumentsAsync(
                        context.Document, 
                        root,
                        invocationExprSyntax, 
                        cancellationToken) 
                }, 
                equivalenceKey = title),
            diagnostic)
        return ()
    } :> Task)

    member private this.RequireNamedArgumentsAsync(document: Document,
                                                   root: SyntaxNode,
                                                   invocationExprSyntax: InvocationExpressionSyntax,
                                                   cancellationToken: CancellationToken) = task {
        let! sema = document.GetSemanticModelAsync(cancellationToken)

        let maybeNameArg (arg: ArgumentSyntax) =
            match sema.GetParameterInfo arg with
            | Some { Parameter = param } -> arg.WithNameColon(SyntaxFactory.NameColon(param.Name))
                                                // Preserve whitespaces, etc. from the original code.
                                               .WithTriviaFrom(arg)
            | _ -> arg
        
        // As it's the named args code fix provider, 
        // the analyzer has already checked that it's OK to make these args named.
        let originalArgList = invocationExprSyntax.ArgumentList
        let maybeNamedArgSyntaxes = originalArgList.Arguments |> Seq.map maybeNameArg

        let newArgumentList = originalArgList.WithArguments(
                                 SyntaxFactory.SeparatedList(
                                     maybeNamedArgSyntaxes,
                                     originalArgList.Arguments.GetSeparators()))
        // An argument list is an "addressable" syntax element, that we can directly
        // replace in the document's root.
        return document.WithSyntaxRoot(root.ReplaceNode(originalArgList, newArgumentList))
    }

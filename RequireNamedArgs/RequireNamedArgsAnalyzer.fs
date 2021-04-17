namespace RequireNamedArgs.Analyzer

open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.ArgumentAndParameter // Actually it's used
open RequireNamedArgs.CSharpAdapters
open RequireNamedArgs.InvocationExprSyntax
open RequireNamedArgs.MaybeBuilder

[<DiagnosticAnalyzer(LanguageNames.CSharp)>]
type public RequireNamedArgsAnalyzer() = 
    inherit DiagnosticAnalyzer()

    static let diagnosticId = "RequireNamedArgs"
    static let messageFormat = "A [RequireNamedArgs] method '{0}' must be invoked with named arguments"
    static let description = "Methods marked with `[RequireNamedArgs]` must be invoked with named arguments."
    static let descriptor = 
        DiagnosticDescriptor(
            id=diagnosticId,
            title="[RequireNamedArgs] method invocation with positional arguments.",
            messageFormat=messageFormat,
            category="Code style",
            defaultSeverity=DiagnosticSeverity.Error, 
            isEnabledByDefault=true, 
            description=description,
            helpLinkUri=null)

    static member DiagnosticId = diagnosticId
    static member MessageFormat = messageFormat

    override val SupportedDiagnostics = ImmutableArray.Create(descriptor)

    override this.Initialize (context: AnalysisContext) =
        // Register ourself to get invoked to analyze 
        //   - invocation expressions; e. g., calling a method. 
        //   - and object creation expressions; e. g., invoking a constructor.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.InvocationExpression, 
            SyntaxKind.ObjectCreationExpression)

    member private this.filterSupported (methodSymbol: IMethodSymbol) = 
        match methodSymbol.MethodKind with
        // So far we only support analyzing of the four kinds of methods listed below.
        | MethodKind.Ordinary
        | MethodKind.Constructor 
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> Some methodSymbol 
        | _                           -> None

    member private this.formatDiagMessage argsWhichShouldBeNamed =
        String.Join(
            ", ",
            argsWhichShouldBeNamed |> Seq.map (fun it -> sprintf "'%s'" it.Parameter.Name))

    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        maybe {
            let! exprSyntax = context.Node |> Option.ofType<ExpressionSyntax>
            let! methodSymbol = context.SemanticModel.GetSymbolInfo(exprSyntax).Symbol 
                                |> Option.ofType<IMethodSymbol>
            let! methodSymbol = methodSymbol |> this.filterSupported
            // We got a supported kind of method.
            // Delegate heavy-lifting to the call below.
            let! argsWhichShouldBeNamed = getArgsWhichShouldBeNamed context.SemanticModel 
                                                                    exprSyntax

            // We inspected the arguments of invocation expression.
            if argsWhichShouldBeNamed |> Seq.any 
            then // There are arguments that should be named -- emit the diagnostic.
                 return context.ReportDiagnostic(
                     Diagnostic.Create(
                         descriptor, 
                         exprSyntax.GetLocation(),
                         // messageArgs
                         methodSymbol.Name, 
                         this.formatDiagMessage argsWhichShouldBeNamed))
            // If none of them should be named or, maybe, they already are named,
            // we have nothing more to do.
            else return ()
        } |> ignore

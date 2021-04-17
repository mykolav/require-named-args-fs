namespace RequireNamedArgs.Analyzer

open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.ArgumentAndParameter
open RequireNamedArgs.CSharpAdapters
open RequireNamedArgs.InvocationExprSyntax

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


    let isSupported (methodSymbol: IMethodSymbol) = 
        match methodSymbol.MethodKind with
        // So far we only support analyzing of the four kinds of methods listed below.
        | MethodKind.Ordinary
        | MethodKind.Constructor 
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> true 
        | _                           -> false

    let formatDiagMessage argsWhichShouldBeNamed =
        String.Join(
            ", ",
            argsWhichShouldBeNamed |> Seq.map (fun it -> sprintf "'%s'" it.ParamSymbol.Name))

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

    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        match Option.ofType<ExpressionSyntax>(context.Node) with
        | Some exprSyntax ->
            match Option.ofType<IMethodSymbol>(context.SemanticModel.GetSymbolInfo(exprSyntax).Symbol) with
            | Some analyzedMethodSymbol ->
                if not (isSupported analyzedMethodSymbol)
                then
                    ()
                else
                    
                // We got a supported kind of method.
                // Delegate heavy-lifting to the call below.
                let argsWhichShouldBeNamedRes =
                    getArgsWhichShouldBeNamed context.SemanticModel 
                                              exprSyntax

                // We inspected the arguments of invocation expression.
                if argsWhichShouldBeNamedRes.ShouldStopAnalysis ||
                   argsWhichShouldBeNamedRes.Value.Length = 0 
                then
                    ()
                else

                // There are arguments that should be named -- emit the diagnostic.
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        descriptor, 
                        exprSyntax.GetLocation(),
                        // messageArgs
                        analyzedMethodSymbol.Name, 
                        formatDiagMessage argsWhichShouldBeNamedRes.Value))
                
            | None ->
                ()
        | None ->
            ()
            

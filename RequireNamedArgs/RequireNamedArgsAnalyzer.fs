namespace RequireNamedArgs.Analyzer


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.Analysis


[<DiagnosticAnalyzer(LanguageNames.CSharp)>]
type public RequireNamedArgsAnalyzer() =
    inherit DiagnosticAnalyzer()


    override val SupportedDiagnostics =
        ImmutableArray.Create(
            DiagnosticDescriptors.NamedArgumentsRequired,
            DiagnosticDescriptors.InternalError)


    override this.Initialize (context: AnalysisContext) =
        // We don't want to require named args in generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
        // We can handle concurrent invocations.
        context.EnableConcurrentExecution()

        // Register ourself to get invoked to analyze
        //   - invocation expressions; e. g., calling a method.
        //   - and object creation expressions; e. g., invoking a constructor.
        context.RegisterSyntaxNodeAction(
            (fun c -> this.Analyze c),
            SyntaxKind.InvocationExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.Attribute)


    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
        try
            this.DoAnalyze(context)
        with
        | ex ->
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InternalError,
                    context.Node.GetLocation(),
                    // messageArgs
                    ex.ToString()))


    member private this.DoAnalyze(context: SyntaxNodeAnalysisContext) =
        let invocationAnalysis = InvocationAnalysis.Of(context.SemanticModel, context.Node)
        match invocationAnalysis with
        | StopAnalysis ->
            ()

        | OK invocationAnalysis ->
            let argumentWithMissingNames = invocationAnalysis.GetArgumentWithMissingNames()
            match argumentWithMissingNames with
            | StopAnalysis
            | OK [||]      ->
                ()

            | OK argumentWithMissingNames ->
                let parameterNames = String.Join(", ", argumentWithMissingNames
                                                       |> Seq.map (fun it -> "'" + it.ParameterSymbol.Name + "'"))

                // There are arguments that are required to have names.
                // Emit a corresponding diagnostic.
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NamedArgumentsRequired,
                        context.Node.GetLocation(),
                        // messageArgs
                        invocationAnalysis.MethodName,
                        parameterNames))

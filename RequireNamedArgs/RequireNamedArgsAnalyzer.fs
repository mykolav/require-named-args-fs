namespace RequireNamedArgs.Analyzer


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.Analysis
open RequireNamedArgs.ArgumentAndParameter


module DiagnosticDescriptors =
    let NamedArgumentsRequired =
        DiagnosticDescriptor(
            id="RequireNamedArgs",
            title="A [RequireNamedArgs] method invoked with positional arguments",
            messageFormat="The method `{0}` must be invoked with named arguments",
            category="Code style",
            defaultSeverity=DiagnosticSeverity.Error,
            isEnabledByDefault=true,
            description="Methods marked with `[RequireNamedArgs]` must be invoked with named arguments",
            helpLinkUri=null)

    let InternalError =
        DiagnosticDescriptor(
            id="RequireNamedArgs9999",
            title="Require named arguments analysis experienced an internal error",
            messageFormat="An internal error in `{0}`",
            category="Code style",
            defaultSeverity=DiagnosticSeverity.Hidden,
            description="Require named arguments analysis experienced an internal error",
            isEnabledByDefault=false,
            helpLinkUri=null)



[<DiagnosticAnalyzer(LanguageNames.CSharp)>]
type public RequireNamedArgsAnalyzer() = 
    inherit DiagnosticAnalyzer()

    let formatDiagMessage argsWhichShouldBeNamed =
        String.Join(
            ", ",
            argsWhichShouldBeNamed |> Seq.map (fun it -> sprintf "'%s'" it.ParamSymbol.Name))


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
            let invocationAnalyzerRes = InvocationAnalyzer.Create(context.SemanticModel, context.Node)
            if invocationAnalyzerRes.ShouldStopAnalysis
            then
                ()
            else

            let invocationAnalyzer = invocationAnalyzerRes.Value

            let argsMissingNamesRes = invocationAnalyzer.GetArgsMissingNames()
            if argsMissingNamesRes.ShouldStopAnalysis ||
               argsMissingNamesRes.Value.Length = 0
            then
                ()
            else

            // There are arguments that are required to have names.
            // Emit a corresponding diagnostic.
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.NamedArgumentsRequired,
                    context.Node.GetLocation(),
                    // messageArgs
                    invocationAnalyzer.MethodName,
                    formatDiagMessage argsMissingNamesRes.Value))
        with
        | ex ->
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.InternalError,
                    context.Node.GetLocation(),
                    // messageArgs
                    ex.ToString()))

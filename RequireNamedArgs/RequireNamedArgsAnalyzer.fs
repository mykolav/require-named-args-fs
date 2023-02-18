namespace RequireNamedArgs.Analyzer


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Diagnostics
open RequireNamedArgs.Analysis
open RequireNamedArgs.ArgumentAndParameter


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
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression)

    
    member private this.Analyze(context: SyntaxNodeAnalysisContext) =
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
                descriptor, 
                context.Node.GetLocation(),
                // messageArgs
                invocationAnalyzer.MethodName,
                formatDiagMessage argsMissingNamesRes.Value))
            

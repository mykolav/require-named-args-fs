namespace RequireNamedArgs.Analyzer


open Microsoft.CodeAnalysis


module DiagnosticDescriptors =


    let NamedArgumentsRequired =
        DiagnosticDescriptor(
            id="RequireNamedArgs",
            title="A [RequireNamedArgs] method invoked with positional arguments",
            messageFormat="`{0}` must be invoked with named arguments",
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

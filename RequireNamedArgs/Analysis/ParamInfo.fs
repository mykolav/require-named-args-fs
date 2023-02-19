namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis


type ParamInfo = {
    MethodOrPropertySymbol : ISymbol
    ParamSymbol : IParameterSymbol }

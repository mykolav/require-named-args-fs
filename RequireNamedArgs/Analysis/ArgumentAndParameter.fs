module RequireNamedArgs.ArgumentAndParameter


open Microsoft.CodeAnalysis
open RequireNamedArgs.Analysis


type ArgWithParamSymbol = {
    ArgSyntax: ArgumentSyntaxInfo;
    ParamSymbol: IParameterSymbol }

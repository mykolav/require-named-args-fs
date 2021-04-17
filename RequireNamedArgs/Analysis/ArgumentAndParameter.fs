module RequireNamedArgs.ArgumentAndParameter

open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis

type ArgWithParamSymbol = {
    ArgSyntax: ArgumentSyntax;
    ParamSymbol: IParameterSymbol }

module RequireNamedArgs.ArgumentAndParameter

open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis

type ArgumentAndParameter = {
    Argument: ArgumentSyntax;
    Parameter: IParameterSymbol }

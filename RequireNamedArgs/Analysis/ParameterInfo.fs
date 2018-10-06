module RequireNamedArgs.ParameterInfo

open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.CSharpAdapters
open RequireNamedArgs.MaybeBuilder

type ParameterInfo = {
    MethodOrProperty : ISymbol;
    Parameter : IParameterSymbol }

type ISymbol with
    member symbol.GetParameters() =
        match symbol with
        | :? IMethodSymbol as s   -> s.Parameters
        | :? IPropertySymbol as s -> s.Parameters
        | _                       -> ImmutableArray<IParameterSymbol>.Empty
        |> Seq.toList

/// <summary>
/// To be able to convert positional arguments to named we need to find
/// corresponding <see cref="IParameterSymbol" /> for each argument.
/// </summary>
type SemanticModel with
    member sema.GetParameterInfo (argument: ArgumentSyntax) = maybe {
        let argList = argument.Parent :?> ArgumentListSyntax
        let exprSyntax = argList.Parent  :?> ExpressionSyntax
        let methodOrProperty = sema.GetSymbolInfo(exprSyntax).Symbol

        let parameters = methodOrProperty.GetParameters()
        if parameters.IsEmpty
        then return! None
        else

        if isNull argument.NameColon 
        then
            // A positional argument.
            match argList.Arguments.IndexOf(argument) with
            | index when index >= 0 && index < parameters.Length -> 
                return { MethodOrProperty = methodOrProperty;
                         Parameter = parameters.[index] }
            | index when index >= parameters.Length 
                            && parameters.[parameters.Length - 1].IsParams ->
                return { MethodOrProperty = methodOrProperty;
                         Parameter = parameters.[parameters.Length - 1] }
            | _ -> return! None
        else 
            // Potentially, this is a named argument.
            let! name = argument.NameColon.Name |> Option.ofObj
            let! nameText = name.Identifier.ValueText |> Option.ofObj
            // Yes, it's a named argument.
            let! parameter = parameters |> Seq.tryFind (fun param -> 
                String.Equals(param.Name, 
                              nameText, 
                              StringComparison.Ordinal))

            return { MethodOrProperty = methodOrProperty;
                     Parameter = parameter }
    }

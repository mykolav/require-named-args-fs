namespace RequireNamedArgs.Analysis


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax


type ParameterInfo = {
    // The method or property
    ParentSymbol: ISymbol
    Symbol: IParameterSymbol }


module SemanticModelParameterInfoExtensions =


    /// <summary>
    /// To be able to convert positional arguments to named we need to find
    /// corresponding <see cref="IParameterSymbol" /> for each argument.
    /// </summary>
    type SemanticModel
        with
        member sema.GetParameterInfo(methodOrPropertySymbol: ISymbol,
                                     argumentPosition: int,
                                     argumentName: NameColonSyntax)
                                    : ParameterInfo option =
            let parameterSymbols =
                match methodOrPropertySymbol with
                | :? IMethodSymbol as s   -> s.Parameters
                | :? IPropertySymbol as s -> s.Parameters
                | _                       -> ImmutableArray<IParameterSymbol>.Empty

            if parameterSymbols.IsEmpty
            then
                // We have an ArgumentSyntax but the corresponding method
                // doesn't take any parameters.
                // Looks like a compile error in the analyzed invocation:
                // it passes an argument to a method that doesn't take any.
                // We pass up on analyzing this invocation, compiler will emit a diagnostic about it.
                None
            else

            if isNull argumentName
            then
                //
                // We found a positional argument.
                //
                if 0 <= argumentPosition && argumentPosition < parameterSymbols.Length
                then
                    Some { ParentSymbol = methodOrPropertySymbol;
                           Symbol = parameterSymbols[argumentPosition] }
                else

                // Is this argument passed as one of the `params`?
                if argumentPosition >= parameterSymbols.Length &&
                   parameterSymbols[parameterSymbols.Length - 1].IsParams
                then
                    Some { ParentSymbol = methodOrPropertySymbol;
                           Symbol = parameterSymbols[parameterSymbols.Length - 1] }
                else

                None
            else

            //
            // Potentially, we found a named argument.
            //
            if (isNull argumentName.Name) ||
               (isNull argumentName.Name.Identifier.ValueText)
            then
                // We encountered an argument in the analyzed invocation,
                // that we don't know how to handle.
                // Pass up on analyzing this invocation.
                // (How can `NameColon.Name` or `NameColon.Name.Identifier.ValueText` actually be null?)
                None
            else

            // Yes, it's a named argument.
            let parameterName = argumentName.Name.Identifier.ValueText
            let parameterSymbol =
                parameterSymbols
                |> Seq.tryFind (fun it -> String.Equals(it.Name, parameterName, StringComparison.Ordinal))

            match parameterSymbol with
            | None ->
                // We could not find a parameter with the name matching the argument's name.
                // Looks like a compile error in the analyzed invocation: it's using a wrong argument name.
                // We pass up on analyzing this invocation, the compiler will emit a diagnostic about it.
                None

            | Some parameterSymbol ->
                Some { ParentSymbol = methodOrPropertySymbol;
                       Symbol = parameterSymbol }

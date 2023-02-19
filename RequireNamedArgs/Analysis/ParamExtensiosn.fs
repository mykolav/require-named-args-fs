namespace RequireNamedArgs.Analysis


open System
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open RequireNamedArgs.Support


module ParamExtensions =


    type ISymbol with
        member symbol.GetParameters(): ImmutableArray<IParameterSymbol> =
            match symbol with
            | :? IMethodSymbol as s   -> s.Parameters
            | :? IPropertySymbol as s -> s.Parameters
            | _                       -> ImmutableArray<IParameterSymbol>.Empty


    /// <summary>
    /// To be able to convert positional arguments to named we need to find
    /// corresponding <see cref="IParameterSymbol" /> for each argument.
    /// </summary>
    type SemanticModel with
        member sema.GetParameterInfo (methodOrPropertySymbol: ISymbol,
                                      argIndex: int,
                                      argSyntax: ArgumentSyntaxInfo)
                                     : Res<ParamInfo> =
            let paramSymbols = methodOrPropertySymbol.GetParameters()
            if paramSymbols.IsEmpty
            then
                // We have an ArgumentSyntax but the corresponding method
                // doesn't take any parameters.
                // Looks like a compile error in the analyzed invocation:
                // it passes an argument to a method that doesn't take any.
                // We pass up on analyzing this invocation, compiler will emit a diagnostic about it.
                StopAnalysis
            else

            if isNull argSyntax.NameColon 
            then
                //
                // We found a positional argument.
                //
                if argIndex >= 0 && argIndex < paramSymbols.Length
                then
                    Ok { MethodOrPropertySymbol = methodOrPropertySymbol;
                         ParamSymbol = paramSymbols[argIndex] }
                else
                    
                if argIndex >= paramSymbols.Length &&
                   paramSymbols[paramSymbols.Length - 1].IsParams
                then
                    Ok { MethodOrPropertySymbol = methodOrPropertySymbol;
                         ParamSymbol = paramSymbols[paramSymbols.Length - 1] }
                else
                    
                StopAnalysis
            else

            //
            // Potentially, we found a named argument.
            //
            if (isNull argSyntax.NameColon.Name) ||
               (isNull argSyntax.NameColon.Name.Identifier.ValueText)
            then
                // We encountered an argument in the analyzed invocation,
                // that we don't know how to handle.
                // Pass up on analyzing this invocation.
                // (How can `NameColon.Name` or `NameColon.Name.Identifier.ValueText` actually be null?)
                StopAnalysis
            else
                
            // Yes, it's a named argument.
            let paramName = argSyntax.NameColon.Name.Identifier.ValueText
            let parameterOpt =
                paramSymbols
                |> Seq.tryFind (fun param -> String.Equals(param.Name, paramName, StringComparison.Ordinal))
                
            match parameterOpt with
            | Some parameter ->
                Ok { MethodOrPropertySymbol = methodOrPropertySymbol;
                     ParamSymbol = parameter }
            | None ->
                // We could not find a parameter with the name matching the argument's name.
                // Looks like a compile error in the analyzed invocation: it's using a wrong name to name an argument.
                // We pass up on analyzing this invocation, compiler will emit a diagnostic about it.
                StopAnalysis

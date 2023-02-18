namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.ArgumentAndParameter
open RequireNamedArgs.Support
open RequireNamedArgs.Support.CSharpAdapters
open RequireNamedArgs.Analysis.ParamInfo


type InvocationAnalyzer private(_sema: SemanticModel,
                                _exprSyntax: ExpressionSyntax,
                                _methodSymbol: IMethodSymbol) = 


    static let NoArgsMissingNames: ArgWithParamSymbol[] = [||]

    
    static let isSupportedMethodKind (methodSymbol: IMethodSymbol): bool = 
        // So far we only support analyzing the four kinds of methods listed below.
        match methodSymbol.MethodKind with
        | MethodKind.Ordinary
        | MethodKind.Constructor 
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> true 
        | _                           -> false


    static let hasRequireNamedArgsAttribute (methodSymbol: IMethodSymbol): bool =
        methodSymbol.GetAttributes()
        |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = "RequireNamedArgsAttribute")
        
        
    let zipArgSyntaxesWithParamSymbols (argSyntaxes: SeparatedSyntaxList<ArgumentSyntax>)
                                       : Res<ArgWithParamSymbol[]> =
        let argWithParamInfos = 
            argSyntaxes 
            |> Seq.mapi (fun argIndex argSyntax -> (argSyntax, _sema.GetParameterInfo(_methodSymbol, argIndex, argSyntax)))
            
        let stopAnalysis =
            argWithParamInfos
            |> Seq.exists (fun (_, paramInfoRes) -> paramInfoRes.ShouldStopAnalysis)
        
        if stopAnalysis
        then
            StopAnalysis
        else
        
        let argWithParamSymbols =
            argWithParamInfos
            |> Seq.map (fun (argSyntax, paramInfoRes) -> { ArgSyntax = argSyntax
                                                           ParamSymbol = paramInfoRes.Value.ParamSymbol })
            |> Array.ofSeq
            
        Ok argWithParamSymbols


    static member Create(sema: SemanticModel, analyzedSyntaxNode: SyntaxNode): Res<InvocationAnalyzer> =
        let analyzedExprSyntaxOpt = analyzedSyntaxNode |> asOptional<ExpressionSyntax>
        if analyzedExprSyntaxOpt.IsNone
        then
            // If the supplied syntax node doesn't represent an expression,
            // it cannot be an invocation.
            // As a result we're not interested.
            StopAnalysis
        else
            
        let analyzedExprSyntax = analyzedExprSyntaxOpt.Value
        let analyzedMethodSymbolOpt = sema.GetSymbolInfo(analyzedExprSyntax).Symbol |> asOptional<IMethodSymbol>
        if analyzedMethodSymbolOpt.IsNone
        then
            // If the symbol that corresponds to the supplied expression syntax is not a method symbol,
            // it cannot be an invocation.
            // As a result we're not interested.
            StopAnalysis
        else
            
        
        let analyzedMethodSymbol = analyzedMethodSymbolOpt.Value
        if not (isSupportedMethodKind analyzedMethodSymbol &&
                hasRequireNamedArgsAttribute analyzedMethodSymbol)
        then
            // It is an invocation, but
            // - we don't supported analyzing invocations of methods of this kind
            // - or the invoked method doesn't require its arguments to be named.
            StopAnalysis
        else
            
        // OK, we're ready to analyze this method invocation/object creation.
        Ok (InvocationAnalyzer(sema, analyzedExprSyntax, analyzedMethodSymbol))
        
        
    member this.MethodName: string = _methodSymbol.Name    


    /// <summary>
    /// This method analyzes the invocation/object creation expression _exprSyntax
    /// to see if any of the arguments supplied to the method/constructor in this expression
    /// are required to be named.
    /// </summary>
    /// <returns>
    /// Either array of arguments which should be named grouped by their types.
    /// Or a value `StopAnalysis` which surprisingly means
    /// we should stop analysis of the current expression.
    /// </returns>
    member this.GetArgsMissingNames(): Res<ArgWithParamSymbol[]> =
        let argSyntaxes =
            match _exprSyntax with
            | :? ImplicitObjectCreationExpressionSyntax as it -> it.ArgumentList.Arguments
            | _ -> _exprSyntax.GetArguments()
            
        if not (argSyntaxes.Any())
        then
            Ok NoArgsMissingNames 
        else

        let lastArgIndex = argSyntaxes.Count - 1
        let lastParamInfoRes = _sema.GetParameterInfo(_methodSymbol, lastArgIndex, argSyntaxes.[lastArgIndex])
        if lastParamInfoRes.ShouldStopAnalysis
        then
            StopAnalysis
        else
            
        if lastParamInfoRes.Value.ParamSymbol.IsParams 
        then
            Ok NoArgsMissingNames 
        else

        let argWithParamSymbolsRes = zipArgSyntaxesWithParamSymbols argSyntaxes
        if argWithParamSymbolsRes.ShouldStopAnalysis
        then
            StopAnalysis
        else
            
        let argsMissingNames =
            argWithParamSymbolsRes.Value
            |> Array.filter (fun it -> isNull it.ArgSyntax.NameColon)
            
        Ok argsMissingNames

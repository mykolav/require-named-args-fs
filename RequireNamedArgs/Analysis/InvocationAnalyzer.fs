namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.ArgumentAndParameter
open RequireNamedArgs.Support
open RequireNamedArgs.Support.CSharpAdapters
open RequireNamedArgs.Analysis.ArgumentSyntaxInfo
open RequireNamedArgs.Analysis.ParamExtensions


type InvocationAnalyzer private(_sema: SemanticModel,
                                _exprSyntax: SyntaxNode,
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


    static let symbolHasRequireNamedArgsAttribute (symbol: ISymbol): bool =
        symbol.GetAttributes()
        |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = "RequireNamedArgsAttribute")


    static let methodSymbolHasRequireNamedArgsAttribute (methodSymbol: IMethodSymbol): bool =
        if symbolHasRequireNamedArgsAttribute methodSymbol
        then
            // If the method has been marked with the attribute, we're done.
            true
        else

        // The method has not been marked with the attribute.
        // See if the method is a primary constructor.
        // In case it is, we're going to check if the type itself has been marked with the attribute,
        // as currently there is no way in C# to specify
        // an attribute should apply to a type's primary constructor.

        if methodSymbol.MethodKind <> MethodKind.Constructor
        then
            // Captain Obvious tells me, if it's not a constructor,
            // it cannot be a primary constructor.
            false
        else

        // Just to be on the safe side, let's check `methodSymbol` has some declaring syntaxes.
        if methodSymbol.DeclaringSyntaxReferences.Length = 0
        then
            false
        else

        // The declaring syntax of a primary constructor is its type declaration.
        match methodSymbol.DeclaringSyntaxReferences[0].GetSyntax() with
        | :? RecordDeclarationSyntax
        // The following two cover C# 12's class/struct primary constructors
        | :? ClassDeclarationSyntax
        | :? StructDeclarationSyntax ->
            // The type declaration syntax corresponds to the primary constructor's containing type.
            // See if the method's containing type has been marked with the attribute.
            symbolHasRequireNamedArgsAttribute methodSymbol.ContainingType
        | _ -> false

        
    let zipArgSyntaxesWithParamSymbols (argSyntaxes: ArgumentSyntaxInfo[])
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
        let analyzedMethodSymbolOpt =
            match analyzedSyntaxNode with
            | :? ExpressionSyntax as analyzedExprSyntax ->
                 sema.GetSymbolInfo(analyzedExprSyntax).Symbol |> asOptional<IMethodSymbol>
            | :? AttributeSyntax as analyzedAttrSyntax ->
                sema.GetSymbolInfo(analyzedAttrSyntax).Symbol |> asOptional<IMethodSymbol>
            | _ ->
                None

        if analyzedMethodSymbolOpt.IsNone
        then
            // If the symbol that corresponds to the supplied expression syntax is not a method symbol,
            // it cannot be an invocation.
            // As a result we're not interested.
            StopAnalysis
        else
        
        let analyzedMethodSymbol = analyzedMethodSymbolOpt.Value
        if not (isSupportedMethodKind analyzedMethodSymbol &&
                methodSymbolHasRequireNamedArgsAttribute analyzedMethodSymbol)
        then
            // It is an invocation, but
            // - we don't supported analyzing invocations of methods of this kind
            // - or the invoked method doesn't require its arguments to be named.
            StopAnalysis
        else
            
        // OK, we're ready to analyze this method invocation/object creation.
        Ok (InvocationAnalyzer(sema, analyzedSyntaxNode, analyzedMethodSymbol))
        
        
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
        let argSyntaxes = _exprSyntax.GetArguments()
            
        if argSyntaxes.Length = 0
        then
            Ok NoArgsMissingNames 
        else

        let lastArgIndex = argSyntaxes.Length - 1
        let lastParamInfoRes = _sema.GetParameterInfo(_methodSymbol, lastArgIndex, argSyntaxes[lastArgIndex])
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

namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.Support
open RequireNamedArgs.Analysis.SyntaxNodeArgumentExtensions
open RequireNamedArgs.Analysis.SemanticModelParameterInfoExtensions


type ArgumentInfo = {
    Syntax: ArgumentSyntaxNode;
    ParameterSymbol: IParameterSymbol }


type InvocationAnalysis private(_sema: SemanticModel,
                                _exprSyntax: SyntaxNode,
                                _methodSymbol: IMethodSymbol) =


    static let NoMissingArgumentNames: ArgumentInfo[] = [||]


    let getArgumentInfos (argumentSyntaxes: ArgumentSyntaxNode[])
                         : AnalysisResult<ArgumentInfo[]> =

        let parameterInfoResults =
            argumentSyntaxes
            |> Seq.mapi (fun i syntax -> _sema.GetParameterInfo(_methodSymbol, i, syntax))

        if parameterInfoResults |> Seq.exists AnalysisResult.isStopAnalysis
        then
            StopAnalysis
        else

        let argumentInfos =
            parameterInfoResults
            |> Seq.mapi (fun i (OK parameterInfo) -> { Syntax = argumentSyntaxes[i]
                                                       ParameterSymbol = parameterInfo.ParamSymbol })
            |> Array.ofSeq

        OK argumentInfos


    static let isSupported (methodSymbol: IMethodSymbol): bool =
        // So far we only support analyzing the four kinds of methods listed below.
        match methodSymbol.MethodKind with
        | MethodKind.Ordinary
        | MethodKind.Constructor
        | MethodKind.LocalFunction
        | MethodKind.ReducedExtension -> true
        | _                           -> false


    static let hasRequireNamedArgsAttribute (symbol: ISymbol): bool =
        symbol.GetAttributes()
        |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = "RequireNamedArgsAttribute")


    static let doesRequireNamedArgs (methodSymbol: IMethodSymbol): bool =
        if hasRequireNamedArgsAttribute methodSymbol
        then
            // If the method has been marked with the attribute, we're done.
            true
        else

        // The method has not been marked with the attribute.
        // See if the method is a primary constructor.
        // In case it is, we're going to check if the containing type
        // itself has been marked with the attribute.
        // (Currently, there is no way in C# to specify
        // an attribute should apply to a type's primary constructor).

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
            hasRequireNamedArgsAttribute methodSymbol.ContainingType
        | _ -> false


    static let resolveMethodSymbol (sema: SemanticModel)
                                   (analyzedSyntaxNode: SyntaxNode)
                                   : AnalysisResult<IMethodSymbol> =
        // We want to inspect a syntax node
        // if it's a method/ctor invocation.
        // We expect a method or ctor invocation corresponds to
        // an expression or an attribute syntax node (invoking the attribute ctor).
        if not (analyzedSyntaxNode :? ExpressionSyntax ||
                analyzedSyntaxNode :? AttributeSyntax)
        then
            // This syntax node we're looking at cannot be an invocation.
            StopAnalysis
        else

        let symbolInfo = sema.GetSymbolInfo(analyzedSyntaxNode)
        match symbolInfo.Symbol with
        | :? IMethodSymbol as methodSymbol ->
            OK methodSymbol

        | _                                ->
            // If the symbol that corresponds to
            // the supplied syntax node is not an `IMethodSymbol`,
            // we cannot be looking at an invocation.
            StopAnalysis


    static member Of(sema: SemanticModel,
                     analyzedSyntaxNode: SyntaxNode)
                     : AnalysisResult<InvocationAnalysis> =
        let result = resolveMethodSymbol sema analyzedSyntaxNode
        match result with
        | StopAnalysis            ->
            StopAnalysis

        | OK analyzedMethodSymbol ->
            if not (isSupported analyzedMethodSymbol &&
                    doesRequireNamedArgs analyzedMethodSymbol)
            then
                // It is an invocation, but
                // - we don't supported analyzing invocations of methods of this kind
                // - or the invoked method doesn't require its arguments to be named.
                StopAnalysis
            else

            // OK, we're ready to analyze this method invocation/object creation.
            OK (InvocationAnalysis(sema, analyzedSyntaxNode, analyzedMethodSymbol))


    member this.MethodName: string = sprintf "%s.%s"_methodSymbol.ContainingType.Name _methodSymbol.Name


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
    member this.GetArgumentWithMissingNames(): AnalysisResult<ArgumentInfo[]> =
        let argumentSyntaxes = _exprSyntax.Arguments

        if argumentSyntaxes.Length = 0
        then
            OK NoMissingArgumentNames
        else

        let lastArgumentAt = argumentSyntaxes.Length - 1
        let lastParameterInfo = _sema.GetParameterInfo(_methodSymbol, lastArgumentAt, argumentSyntaxes[lastArgumentAt])
        match lastParameterInfo with
        | StopAnalysis ->
            StopAnalysis

        | OK lastParameterInfo ->
            // If the last parameter is `params`
            // we don't require named arguments.
            // TODO: Is this limitation still present in C#?
            if lastParameterInfo.ParamSymbol.IsParams
            then
                OK NoMissingArgumentNames
            else

            let argumentInfos = getArgumentInfos argumentSyntaxes
            match argumentInfos with
            | StopAnalysis     ->
                StopAnalysis

            | OK argumentInfos ->
                let argumentWithMissingNames =
                    argumentInfos
                    |> Array.filter (fun it -> isNull it.Syntax.NameColon)

                OK argumentWithMissingNames

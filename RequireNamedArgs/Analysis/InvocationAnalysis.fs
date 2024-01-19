namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.Analysis.SyntaxNodeArgumentExtensions
open RequireNamedArgs.Analysis.SemanticModelParameterInfoExtensions


type ArgumentInfo = {
    Syntax: IArgumentSyntax;
    ParameterSymbol: IParameterSymbol }


[<Struct>]
type AnalysisResult<'T>
    = StopAnalysis
    | OK of 'T


type InvocationAnalysis private(_sema: SemanticModel,
                                _expressionSyntax: SyntaxNode,
                                _methodSymbol: IMethodSymbol) =


    static let NoMissingArgumentNames: ArgumentInfo[] = [||]


    let getArgumentInfos (argumentSyntaxes: IArgumentSyntax[])
                         : AnalysisResult<ArgumentInfo[]> =

        let parameterInfoResults =
            argumentSyntaxes
            |> Seq.mapi (fun at a -> _sema.GetParameterInfo(_methodSymbol, at, a.NameColon))

        if parameterInfoResults |> Seq.exists Option.isNone
        then
            StopAnalysis
        else

        let argumentInfos =
            parameterInfoResults
            |> Seq.mapi (fun i (Some parameterInfo) -> { Syntax = argumentSyntaxes[i]
                                                         ParameterSymbol = parameterInfo.Symbol })
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


    static let requiresNamedArgs (methodSymbol: IMethodSymbol): bool =
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
            if isSupported analyzedMethodSymbol &&
               requiresNamedArgs analyzedMethodSymbol
            then
                // OK, we're ready to analyze this method invocation/object creation.
                OK (InvocationAnalysis(sema, analyzedSyntaxNode, analyzedMethodSymbol))
            else
                // It is an invocation, but
                // - we don't supported analyzing invocations of methods of this kind
                // - or the invoked method doesn't require its arguments to be named.
                StopAnalysis


    member this.MethodName: string = sprintf "%s.%s"_methodSymbol.ContainingType.Name _methodSymbol.Name


    /// <summary>
    /// This method analyzes the invocation/object creation expression `_expressionSyntax`
    /// to see if any of the arguments supplied to the method/constructor in this expression
    /// are required to be named.
    /// </summary>
    /// <returns>
    /// Either an array of arguments which should be named, grouped by their types.
    /// Or `StopAnalysis` which, somewhat surprisingly, means we should
    /// stop the analysis of current expression.
    /// </returns>
    member this.GetArgumentWithMissingNames(): AnalysisResult<ArgumentInfo[]> =
        let argumentSyntaxes = _expressionSyntax.Arguments

        if argumentSyntaxes.Length = 0
        then
            OK NoMissingArgumentNames
        else

        let lastArgumentAt = argumentSyntaxes.Length - 1
        let lastArgument = argumentSyntaxes[lastArgumentAt]

        let lastParameterInfo = _sema.GetParameterInfo(_methodSymbol,
                                                       lastArgumentAt,
                                                       lastArgument.NameColon)
        match lastParameterInfo with
        | None ->
            StopAnalysis

        | Some lastParameterInfo ->
            // If the last parameter is `params`
            // we don't require named arguments.
            // TODO: Is this limitation still present in C#?
            if lastParameterInfo.Symbol.IsParams
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

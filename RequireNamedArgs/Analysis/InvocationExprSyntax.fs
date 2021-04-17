/// <summary>
/// This module contains code that looks at an invocation expression and its arguments
/// and decides whether the arguments must be named.
/// The rules are:
///   - Look at an invocation expression (e.g., a method call).
///   - Find the callee's definition.
///   - If the definition is prefixed with a comment of the form `//[RequireNamedArgs]`,  
///     the the invocation's arguments must be named.
///   - If the last parameter is <see langword="params" />, the analyzer
///     doesn't emit the diagnostic, as we cannot use named arguments in this case.
/// It's used by both 
///   - the <see cref="RequireNamedArgsAnalyzer"/> class and
///   - the <see cref="RequireNamedArgsCodeFixProvider"/> class.
/// </summary>
module RequireNamedArgs.InvocationExprSyntax

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.ArgumentAndParameter
open RequireNamedArgs.ParameterInfo
open RequireNamedArgs.Res

let private getParamSymbolForArgs
    (sema: SemanticModel) 
    (argSyntaxes: SeparatedSyntaxList<ArgumentSyntax>): Res<ArgWithParamSymbol[]> =
    let argWithParamInfos = 
        argSyntaxes 
        |> Seq.map (fun it -> (it, sema.GetParameterInfo it))
        
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

let private isMarkedWithNamedArgsRequiredAttr (sema: SemanticModel) 
                                              (exprSyntax: ExpressionSyntax) =
    let methodSymbol = sema.GetSymbolInfo(exprSyntax).Symbol :?> IMethodSymbol
    methodSymbol.GetAttributes()
    |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = "RequireNamedArgsAttribute")

let private NoArgsShouldBeNamed: ArgWithParamSymbol[] = [||]

/// <summary>
/// This method analyzes the supplied <paramref name="exprSyntax" />
/// to see if any of the arguments need to be named.
/// </summary>
/// <param name="sema">The semantic model is necessary for the analysis</param>
/// <param name="exprSyntax">The invocation to analyze</param>
/// <returns>
/// An optional array of arguments which should be named grouped by their types.
/// </returns>
let getArgsWhichShouldBeNamed (sema: SemanticModel) 
                              (exprSyntax: ExpressionSyntax): Res<ArgWithParamSymbol[]> =
        let argSyntaxes = exprSyntax.GetArguments()

        if not (argSyntaxes.Any())
        then
            Ok NoArgsShouldBeNamed 
        else

        if not (isMarkedWithNamedArgsRequiredAttr sema exprSyntax) 
        then
            Ok NoArgsShouldBeNamed
        else

        let lastParamInfoRes = sema.GetParameterInfo (Seq.last argSyntaxes)
        if lastParamInfoRes.ShouldStopAnalysis
        then
            StopAnalysis
        else
            
        if lastParamInfoRes.Value.ParamSymbol.IsParams 
        then
            Ok NoArgsShouldBeNamed 
        else

        let argWithParamSymbolsRes = getParamSymbolForArgs sema argSyntaxes
        if argWithParamSymbolsRes.ShouldStopAnalysis
        then
            StopAnalysis
        else
            
        let argsWhichShouldBeNamed =
            argWithParamSymbolsRes.Value
            |> Array.filter (fun it -> isNull it.ArgSyntax.NameColon)
            
        Ok argsWhichShouldBeNamed

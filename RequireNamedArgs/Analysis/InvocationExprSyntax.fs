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

open System
open System.Text.RegularExpressions
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open RequireNamedArgs.MaybeBuilder
open RequireNamedArgs.ArgumentAndParameter
open RequireNamedArgs.ParameterInfo
open RequireNamedArgs.CSharpAdapters

let private getArgAndParams
    (sema: SemanticModel) 
    (argumentSyntaxes: SeparatedSyntaxList<ArgumentSyntax>) =
    let syntaxAndMaybeInfos = 
        argumentSyntaxes 
        |> Seq.map (fun it -> (it, sema.GetParameterInfo it))
    let folder (syntax, maybeInfo) acc = maybe {
        let! argAndParams = acc 
        let! { ParameterInfo.Parameter = param } = maybeInfo
        return { Argument = syntax; Parameter = param }::argAndParams
    }
    Seq.foldBack folder syntaxAndMaybeInfos (Some [])

let private namedArgsRequired (sema: SemanticModel) 
                              (exprSyntax: ExpressionSyntax) =
    let methodSymbolOpt = sema.GetSymbolInfo(exprSyntax).Symbol |> Option.ofType<IMethodSymbol>
    match methodSymbolOpt with
    | None -> false
    | Some methodSymbol ->
           (methodSymbol.GetAttributes()
            |> Seq.exists (fun attrData -> attrData.AttributeClass.Name = "RequireNamedArgsAttribute"))

/// <summary>
/// This method analyzes the supplied <paramref name="invocationExprSyntax" />
/// to see if any of the arguments need to be named.
/// </summary>
/// <param name="sema">The semantic model is necessary for the analysis</param>
/// <param name="invocationExprSyntax">The invocation to analyze</param>
/// <returns>
/// An option of list of arguments which should be named grouped by their types.
/// </returns>
let getArgsWhichShouldBeNamed 
    (sema: SemanticModel) 
    (exprSyntax: ExpressionSyntax) =
    let NoArgsShouldBeNamed = []
    maybe {
        let argSyntaxes = match exprSyntax with 
            | :? InvocationExpressionSyntax as i -> i.ArgumentList.Arguments
            | :? ObjectCreationExpressionSyntax as o -> o.ArgumentList.Arguments
            | _ -> new SeparatedSyntaxList<ArgumentSyntax>()

        if Seq.isEmpty argSyntaxes 
        then return NoArgsShouldBeNamed 
        else

        if not (namedArgsRequired sema exprSyntax) 
        then return NoArgsShouldBeNamed 
        else

        let! { Parameter = lastParam } = argSyntaxes |> Seq.last |> sema.GetParameterInfo
        if lastParam.IsParams 
        then return NoArgsShouldBeNamed 
        else

        return! getArgAndParams sema argSyntaxes |>> List.filter (fun it -> isNull it.Argument.NameColon)
    }

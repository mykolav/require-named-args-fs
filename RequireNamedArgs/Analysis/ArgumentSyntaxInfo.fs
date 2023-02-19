namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax


type ArgumentSyntaxInfo =
    | ArgumentSyntax of ArgumentSyntax
    | AttributeArgumentSyntax of AttributeArgumentSyntax with
    
    member this.NameColon =
        match this with
        | ArgumentSyntax argSyntax -> argSyntax.NameColon
        | AttributeArgumentSyntax attrArgSyntax -> attrArgSyntax.NameColon
        
    member this.WithNameColon(paramInfo: ParamInfo): ArgumentSyntaxInfo =
        match this with
        | ArgumentSyntax argSyntax ->
                ArgumentSyntax (argSyntax.WithNameColon(
                    SyntaxFactory.NameColon(paramInfo.ParamSymbol.Name))
                                 .WithTriviaFrom(argSyntax)) // Preserve whitespaces, etc. from the original code.
        | AttributeArgumentSyntax attrArgSyntax ->
                AttributeArgumentSyntax (attrArgSyntax.WithNameColon(
                    SyntaxFactory.NameColon(paramInfo.ParamSymbol.Name))
                                 .WithTriviaFrom(attrArgSyntax)) // Preserve whitespaces, etc. from the original code.


type ArgumentListSyntaxInfo =
    | ArgumentListSyntax of ArgumentListSyntax
    | AttributeArgumentListSyntax of AttributeArgumentListSyntax with
    
    
    member this.Syntax
        with get(): SyntaxNode =
            match this with
            | ArgumentListSyntax list -> list
            | AttributeArgumentListSyntax list -> list
        
        
    member this.Parent =
        match this with
        | ArgumentListSyntax list -> list.Parent
        | AttributeArgumentListSyntax list -> list.Parent
        
        
    member this.Arguments =
        match this with
        | ArgumentListSyntax list -> list.Arguments |> Seq.map(fun it -> ArgumentSyntax it)
        | AttributeArgumentListSyntax list -> list.Arguments |> Seq.map(fun it -> AttributeArgumentSyntax it)
        |> Array.ofSeq
        
        
    member this.WithArguments(argumentSyntaxInfos: seq<ArgumentSyntaxInfo>): ArgumentListSyntaxInfo =
        match this with
        | ArgumentListSyntax list ->
            let argumentSyntaxes = argumentSyntaxInfos
                                   |> Seq.choose (fun it -> match it with 
                                                            | ArgumentSyntax it -> Some it
                                                            | _ -> None)
                                   |> Array.ofSeq
            if argumentSyntaxes.Length = 0
            then
                this
            else
                
            ArgumentListSyntax (list.WithArguments(
                SyntaxFactory.SeparatedList(
                    argumentSyntaxes,
                    list.Arguments.GetSeparators())))
        | AttributeArgumentListSyntax list ->
            let attributeArgumentSyntaxes = argumentSyntaxInfos
                                            |> Seq.choose (fun it -> match it with 
                                                                     | AttributeArgumentSyntax it -> Some it
                                                                     | _ -> None)
                                            |> Array.ofSeq
            if attributeArgumentSyntaxes.Length = 0
            then
                this
            else
                
            AttributeArgumentListSyntax (list.WithArguments(
                SyntaxFactory.SeparatedList(
                    attributeArgumentSyntaxes,
                    list.Arguments.GetSeparators())))
        


module ArgumentSyntaxInfo =

    
    type SyntaxNode with
        member syntaxNode.GetArgumentList(): ArgumentListSyntaxInfo option =
            match syntaxNode with 
            | :? InvocationExpressionSyntax as it -> Some (ArgumentListSyntax it.ArgumentList)
            | :? ObjectCreationExpressionSyntax as it -> Some (ArgumentListSyntax  it.ArgumentList)
            | :? ImplicitObjectCreationExpressionSyntax as it -> Some (ArgumentListSyntax it.ArgumentList)
            | :? AttributeSyntax as it -> Some (AttributeArgumentListSyntax it.ArgumentList)
            | _ -> None


        member syntaxNode.GetArguments(): ArgumentSyntaxInfo[] =
            match syntaxNode.GetArgumentList() with 
            | Some info -> info.Arguments
            | None -> [||]

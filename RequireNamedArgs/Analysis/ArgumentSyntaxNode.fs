namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax


type ArgumentSyntaxNode =
    | ArgumentSyntax of ArgumentSyntax
    | AttributeArgumentSyntax of AttributeArgumentSyntax
    with


    static member valueOfArgumentSyntax = function
        | ArgumentSyntax it          -> Some it
        | AttributeArgumentSyntax _  -> None


    static member valueOfAttributeArgumentSyntax = function
        | ArgumentSyntax _           -> None
        | AttributeArgumentSyntax it -> Some it


    member this.NameColon =
        match this with
        | ArgumentSyntax argSyntax -> argSyntax.NameColon
        | AttributeArgumentSyntax attrArgSyntax -> attrArgSyntax.NameColon


    member this.WithNameColon(name: string): ArgumentSyntaxNode =
        match this with
        | ArgumentSyntax argSyntax ->
                ArgumentSyntax (argSyntax.WithNameColon(
                    SyntaxFactory.NameColon(name))
                                 // Preserve whitespaces, etc. from the original code.
                                 .WithTriviaFrom(argSyntax))

        | AttributeArgumentSyntax attrArgSyntax ->
                AttributeArgumentSyntax (attrArgSyntax.WithNameColon(
                    SyntaxFactory.NameColon(name))
                                 // Preserve whitespaces, etc. from the original code.
                                 .WithTriviaFrom(attrArgSyntax))


type ArgumentListSyntaxNode =
    | ArgumentListSyntax of ArgumentListSyntax
    | AttributeArgumentListSyntax of AttributeArgumentListSyntax
    with


    member this.Syntax
        with get(): SyntaxNode =
            match this with
            | ArgumentListSyntax list -> list
            | AttributeArgumentListSyntax list -> list


    member this.Parent =
        match this with
        | ArgumentListSyntax list -> list.Parent
        | AttributeArgumentListSyntax list -> list.Parent


    member this.Arguments: ArgumentSyntaxNode[] =
        match this with
        | ArgumentListSyntax list -> list.Arguments |> Seq.map(fun it -> ArgumentSyntax it)
        | AttributeArgumentListSyntax list -> list.Arguments |> Seq.map(fun it -> AttributeArgumentSyntax it)
        |> Array.ofSeq


    member this.WithArguments(argumentSyntaxNodes: seq<ArgumentSyntaxNode>)
                             : ArgumentListSyntaxNode =
        match this with
        | ArgumentListSyntax als ->
            let argumentSyntaxes =
               argumentSyntaxNodes
               |> Seq.choose (fun it -> ArgumentSyntaxNode.valueOfArgumentSyntax it)
               |> Array.ofSeq

            if Array.isEmpty argumentSyntaxes
            then
                this
            else

            ArgumentListSyntax (als.WithArguments(
                SyntaxFactory.SeparatedList(
                    argumentSyntaxes,
                    als.Arguments.GetSeparators())))

        | AttributeArgumentListSyntax aals ->
            let attributeArgumentSyntaxes =
                argumentSyntaxNodes
                |> Seq.choose (fun it -> ArgumentSyntaxNode.valueOfAttributeArgumentSyntax it)
                |> Array.ofSeq

            if Array.isEmpty attributeArgumentSyntaxes
            then
                this
            else

            AttributeArgumentListSyntax (aals.WithArguments(
                SyntaxFactory.SeparatedList(
                    attributeArgumentSyntaxes,
                    aals.Arguments.GetSeparators())))


    // member this.WithArguments(argumentSyntaxes: seq<ArgumentSyntax>)
    //                          : ArgumentListSyntaxNode =
    //     match this with
    //     | ArgumentListSyntax als ->
    //         let argumentSyntaxes =
    //            argumentSyntaxes
    //            |> Seq.choose (fun it -> ArgumentSyntaxNode.valueOfArgumentSyntax it)
    //            |> Array.ofSeq
    //
    //         if Array.isEmpty argumentSyntaxes
    //         then
    //             this
    //         else
    //
    //         ArgumentListSyntax (als.WithArguments(
    //             SyntaxFactory.SeparatedList(
    //                 argumentSyntaxes,
    //                 als.Arguments.GetSeparators())))
    //
    //     | AttributeArgumentListSyntax aals ->
    //         let attributeArgumentSyntaxes =
    //             argumentSyntaxes
    //             |> Seq.choose (fun it -> ArgumentSyntaxNode.valueOfAttributeArgumentSyntax it)
    //             |> Array.ofSeq
    //
    //         if Array.isEmpty attributeArgumentSyntaxes
    //         then
    //             this
    //         else
    //
    //         AttributeArgumentListSyntax (aals.WithArguments(
    //             SyntaxFactory.SeparatedList(
    //                 attributeArgumentSyntaxes,
    //                 aals.Arguments.GetSeparators())))


module SyntaxNodeArgumentExtensions =


    type SyntaxNode with
        member syntaxNode.ArgumentList: ArgumentListSyntaxNode option =
            match syntaxNode with
            | :? InvocationExpressionSyntax as it -> Some (ArgumentListSyntax it.ArgumentList)
            | :? ObjectCreationExpressionSyntax as it -> Some (ArgumentListSyntax  it.ArgumentList)
            | :? ImplicitObjectCreationExpressionSyntax as it -> Some (ArgumentListSyntax it.ArgumentList)
            | :? AttributeSyntax as it -> Some (AttributeArgumentListSyntax it.ArgumentList)
            | _ -> None


        member syntaxNode.Arguments: ArgumentSyntaxNode[] =
            match syntaxNode.ArgumentList with
            | Some info -> info.Arguments
            | None      -> Array.empty

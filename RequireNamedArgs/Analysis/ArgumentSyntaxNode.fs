namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax

// While both `ArgumentSyntax` and `AttributeArgumentSyntax`
// structurally implement the same interface,
// nominally their closest common ancestor is `SyntaxNode`.
// We introduce a couple of adapters, so that we can
// have common code handling arguments syntax.

type IArgumentSyntax =
    abstract NameColon: NameColonSyntax


type IArgumentSyntax<'T when 'T :> SyntaxNode> =
    inherit IArgumentSyntax
    abstract Syntax: 'T
    abstract WithNameColon: (*name*) string -> 'T


[<AutoOpen>]
module private ArgumentSyntaxExtensions =


    type IArgumentSyntax
        with
        static member Of(argument: ArgumentSyntax) =
            { new IArgumentSyntax with
                member _.NameColon = argument.NameColon }


        static member Of(argument: AttributeArgumentSyntax) =
            { new IArgumentSyntax with
                member _.NameColon = argument.NameColon }


    type IArgumentSyntax<'T when 'T :> SyntaxNode>
        with
        static member OfT(argument: ArgumentSyntax) =
            { new IArgumentSyntax<ArgumentSyntax> with
                member _.Syntax = argument
                member _.NameColon = argument.NameColon
                member _.WithNameColon(name: string) =
                    argument.WithNameColon(SyntaxFactory.NameColon(name)) }


        static member OfT(argument: AttributeArgumentSyntax) =
            { new IArgumentSyntax<AttributeArgumentSyntax> with
                member _.Syntax = argument
                member _.NameColon = argument.NameColon
                member _.WithNameColon(name: string) =
                    argument.WithNameColon(SyntaxFactory.NameColon(name)) }


type IArgumentListSyntax =
    abstract Parent: SyntaxNode
    abstract Syntax: SyntaxNode
    abstract Arguments: IArgumentSyntax[]


type IArgumentListSyntax<'T when 'T :> SyntaxNode> =
    inherit IArgumentListSyntax
    abstract Arguments: IArgumentSyntax<'T>[]
    abstract WithArguments: seq<'T> -> IArgumentListSyntax<'T>


type ArgumentListSyntaxNode(_syntax: ArgumentListSyntax) =
    interface IArgumentListSyntax with
        member this.Parent: SyntaxNode = _syntax.Parent
        member this.Syntax: SyntaxNode = _syntax
        member this.Arguments: IArgumentSyntax[] =
            this.Arguments |> Array.map (fun it -> it :> IArgumentSyntax)


    interface IArgumentListSyntax<ArgumentSyntax> with
        member this.Arguments: IArgumentSyntax<ArgumentSyntax>[] = this.Arguments


        member this.WithArguments(arguments: seq<ArgumentSyntax>): IArgumentListSyntax<ArgumentSyntax> =
            ArgumentListSyntaxNode (_syntax.WithArguments(
                SyntaxFactory.SeparatedList(
                    arguments,
                    _syntax.Arguments.GetSeparators())))


    member private _.Arguments: IArgumentSyntax<ArgumentSyntax>[] =
        _syntax.Arguments
            |> Seq.map (fun it -> IArgumentSyntax.OfT(it))
            |> Array.ofSeq


type AttributeArgumentListSyntaxNode(_syntax: AttributeArgumentListSyntax) =
    interface IArgumentListSyntax with
        member this.Parent: SyntaxNode = _syntax.Parent
        member this.Syntax: SyntaxNode = _syntax
        member this.Arguments: IArgumentSyntax[] =
            this.Arguments |> Array.map (fun it -> it :> IArgumentSyntax)


    interface IArgumentListSyntax<AttributeArgumentSyntax> with
        member this.Arguments: IArgumentSyntax<AttributeArgumentSyntax>[] = this.Arguments


        member this.WithArguments(arguments: seq<AttributeArgumentSyntax>)
                                 : IArgumentListSyntax<AttributeArgumentSyntax> =
            AttributeArgumentListSyntaxNode (_syntax.WithArguments(
                SyntaxFactory.SeparatedList(
                    arguments,
                    _syntax.Arguments.GetSeparators())))


    member private _.Arguments: IArgumentSyntax<AttributeArgumentSyntax>[] =
        _syntax.Arguments
            |> Seq.map (fun it -> IArgumentSyntax.OfT(it))
            |> Array.ofSeq


[<AutoOpen>]
module private ArgumentListSyntaxExtensions =
    type IArgumentListSyntax<'T when 'T :> SyntaxNode>
        with
        static member Of(list: ArgumentListSyntax)
                        : IArgumentListSyntax<ArgumentSyntax> =
            ArgumentListSyntaxNode(list)


        static member Of(list: AttributeArgumentListSyntax)
                        : IArgumentListSyntax<AttributeArgumentSyntax> =
            AttributeArgumentListSyntaxNode(list)


module SyntaxNodeArgumentExtensions =
    type SyntaxNode
    with
        member syntaxNode.ArgumentList: IArgumentListSyntax option =
            match syntaxNode with
            | :? InvocationExpressionSyntax as it ->
                Some (IArgumentListSyntax.Of(it.ArgumentList))

            | :? ObjectCreationExpressionSyntax as it ->
                Some (IArgumentListSyntax.Of(it.ArgumentList))

            | :? ImplicitObjectCreationExpressionSyntax as it ->
                Some (IArgumentListSyntax.Of(it.ArgumentList))

            | :? AttributeSyntax as it ->
                Some (IArgumentListSyntax.Of(it.ArgumentList))

            | _ -> None


        member syntaxNode.Arguments: IArgumentSyntax[] =
            match syntaxNode.ArgumentList with
            | None              -> Array.empty
            | Some argumentList -> argumentList.Arguments

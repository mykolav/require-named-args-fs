namespace RequireNamedArgs.Analysis


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax


type IArgumentSyntax =
    abstract NameColon: NameColonSyntax


type IArgumentSyntax<'T when 'T :> SyntaxNode> =
    inherit IArgumentSyntax
    abstract Syntax: 'T
    abstract WithNameColon: (*name*) string -> 'T


type ArgumentSyntaxNode(_syntax: ArgumentSyntax) =
    interface IArgumentSyntax with
        member _.NameColon: NameColonSyntax = _syntax.NameColon

    interface IArgumentSyntax<ArgumentSyntax> with
        member _.Syntax = _syntax
        member _.WithNameColon(name: string): ArgumentSyntax =
            _syntax.WithNameColon(SyntaxFactory.NameColon(name))
                   .WithTriviaFrom(_syntax)


type AttributeArgumentSyntaxNode(_syntax: AttributeArgumentSyntax) =
    interface IArgumentSyntax with
        member _.NameColon: NameColonSyntax = _syntax.NameColon


    interface IArgumentSyntax<AttributeArgumentSyntax> with
        member _.Syntax = _syntax
        member _.WithNameColon(name: string): AttributeArgumentSyntax =
            _syntax.WithNameColon(SyntaxFactory.NameColon(name))
                   .WithTriviaFrom(_syntax)


type IArgumentListSyntax =
    abstract Arguments: IArgumentSyntax[]


type IArgumentListSyntax<'T when 'T :> SyntaxNode> =
    abstract Parent: SyntaxNode
    abstract Syntax: SyntaxNode
    abstract Arguments: IArgumentSyntax<'T>[]
    abstract WithArguments: seq<'T> -> IArgumentListSyntax<'T>


type ArgumentListSyntaxNode(_syntax: ArgumentListSyntax) =
    interface IArgumentListSyntax with
        member this.Arguments: IArgumentSyntax[] =
            this.Arguments |> Array.map (fun it -> it :> IArgumentSyntax)


    interface IArgumentListSyntax<ArgumentSyntax> with
        member this.Parent: SyntaxNode = _syntax.Parent
        member this.Syntax: SyntaxNode = _syntax
        member this.Arguments: IArgumentSyntax<ArgumentSyntax>[] = this.Arguments


        member this.WithArguments(arguments: seq<ArgumentSyntax>): IArgumentListSyntax<ArgumentSyntax> =
            ArgumentListSyntaxNode (_syntax.WithArguments(
                SyntaxFactory.SeparatedList(
                    arguments,
                    _syntax.Arguments.GetSeparators())))


    member private _.Arguments: IArgumentSyntax<ArgumentSyntax>[] =
        _syntax.Arguments
            |> Seq.map (fun it -> ArgumentSyntaxNode(it)
                                  :> IArgumentSyntax<ArgumentSyntax>)
            |> Array.ofSeq


type AttributeArgumentListSyntaxNode(_syntax: AttributeArgumentListSyntax) =
    interface IArgumentListSyntax with
        member this.Arguments: IArgumentSyntax[] =
            this.Arguments |> Array.map (fun it -> it :> IArgumentSyntax)


    interface IArgumentListSyntax<AttributeArgumentSyntax> with
        member this.Parent: SyntaxNode = _syntax.Parent
        member this.Syntax: SyntaxNode = _syntax
        member this.Arguments: IArgumentSyntax<AttributeArgumentSyntax>[] =
            this.Arguments


        member this.WithArguments(arguments: seq<AttributeArgumentSyntax>)
                                 : IArgumentListSyntax<AttributeArgumentSyntax> =
            AttributeArgumentListSyntaxNode (_syntax.WithArguments(
                SyntaxFactory.SeparatedList(
                    arguments,
                    _syntax.Arguments.GetSeparators())))


    member private _.Arguments: IArgumentSyntax<AttributeArgumentSyntax>[] =
        _syntax.Arguments
            |> Seq.map (fun it -> AttributeArgumentSyntaxNode(it)
                                  :> IArgumentSyntax<AttributeArgumentSyntax>)
            |> Array.ofSeq


module SyntaxNodeArgumentExtensions =


    type SyntaxNode
    with


        member syntaxNode.ArgumentList: IArgumentListSyntax option =
            match syntaxNode with
            | :? InvocationExpressionSyntax as it ->
                Some (ArgumentListSyntaxNode(it.ArgumentList))

            | :? ObjectCreationExpressionSyntax as it ->
                Some (ArgumentListSyntaxNode(it.ArgumentList))

            | :? ImplicitObjectCreationExpressionSyntax as it ->
                Some (ArgumentListSyntaxNode(it.ArgumentList))

            | :? AttributeSyntax as it ->
                Some (AttributeArgumentListSyntaxNode(it.ArgumentList))

            | _ -> None


        member syntaxNode.Arguments: IArgumentSyntax[] =
            match syntaxNode.ArgumentList with
            | Some info -> info.Arguments
            | None      -> Array.empty

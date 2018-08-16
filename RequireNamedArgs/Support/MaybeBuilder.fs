module RequireNamedArgs.MaybeBuilder

type MaybeBuilder() =
    member this.Return(value) = Some value
    member this.ReturnFrom(m) = m
    member this.Bind(m, f)    = Option.bind f m

let maybe = new MaybeBuilder()

let (>>=) m f = Option.bind f m
let (|>>) m f = Option.map f m

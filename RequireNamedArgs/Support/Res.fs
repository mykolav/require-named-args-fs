module RequireNamedArgs.Res

[<Struct; DefaultAugmentation(false)>]
type Res<'T>
    = StopAnalysis
    | Ok of 'T
    with
    member this.ShouldStopAnalysis: bool =
        match this with
        | StopAnalysis -> true
        | Ok _  -> false
    member this.IsOk: bool = not this.ShouldStopAnalysis
    member this.Value: 'T =
        match this with
        | Ok value -> value
        | _ -> invalidOp "Res<'T>.Value"

namespace RequireNamedArgs.Support

[<Struct>]
type AnalysisResult<'T>
    = StopAnalysis
    | OK of 'T
    with


    static member isStopAnalysis(it: AnalysisResult<'T>): bool =
        match it with
        | StopAnalysis -> true
        | OK _         -> false


    static member isOK(it: AnalysisResult<'T>): bool =
        match it with
        | StopAnalysis -> false
        | OK _         -> true


    static member valueOf(it: AnalysisResult<'T>): 'T =
        match it with
        | OK value -> value
        | _        -> invalidOp "AnalysisResult<'T>.Value"

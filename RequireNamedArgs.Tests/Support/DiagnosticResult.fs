module RequireNamedArgs.Tests.Support.DiagnosticResult

open Microsoft.CodeAnalysis

/// <summary>
/// Location where the diagnostic appears, as determined by path, line number, and column number.
/// </summary>
type DiagResultLocation = { 
    Path: string
    Line: uint32
    Col:  uint32 }

/// <summary>
/// Type that stores information about a Diagnostic appearing in a source
/// </summary>
type DiagResult(severity            : DiagnosticSeverity,
                id                  : string,
                message             : string,
                ?location           : DiagResultLocation,
                ?additionalLocations: DiagResultLocation list) = 
        member val Location            = location
        member val AdditionalLocations = defaultArg additionalLocations []
        member val Severity            = severity
        member val Id                  = id
        member val Message             = message

//type DiagResult1 = { Location           : DiagResultLocation option
//                     AdditionalLocations: seq<DiagResultLocation>
//                     Severity           : DiagnosticSeverity
//                     Id                 : string
//                     Message            : string }

module RequireNamedArgs.Tests.Support.DiagnosticMatcher

[<RequireQualifiedAccess>]
module Expect = 
    open Expecto
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.Diagnostics
    open RequireNamedArgs.Tests.Support.DiagnosticFormatter
    open RequireNamedArgs.Tests.Support.DiagnosticResult

    let private expectLocationsToMatch describeDiag (expectedLoc: DiagResultLocation, actual: Location) =
        let actualSpan = actual.GetLineSpan()
        let expectedMatchesActualPath = actualSpan.Path = expectedLoc.Path 
                                        || (not (isNull actualSpan.Path) 
                                            && actualSpan.Path.Contains("Test0.") 
                                            && expectedLoc.Path.Contains("Test."))
        Expect.isTrue expectedMatchesActualPath
                        (sprintf "Expected diagnostic to be in file \"%s\" was actually in file \"%s\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                expectedLoc.Path actualSpan.Path <| describeDiag())

        let actualLinePos = actualSpan.StartLinePosition

        let expectLinesToMatch() =
            // Only check line position if there is an actual line in the real diagnostic
            if actualLinePos.Line <= 0 then () else
            Expect.isTrue (actualLinePos.Line + 1 = int expectedLoc.Line)
                            (sprintf "Expected diagnostic to be on line \"%d\" was actually on line \"%d\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                    expectedLoc.Line (actualLinePos.Line + 1) <| describeDiag())
        expectLinesToMatch()

        let expectColsToMatch() =
            // Only check column position if there is an actual column position in the real diagnostic
            if actualLinePos.Character <= 0 then () else
            Expect.isTrue (actualLinePos.Character + 1 = int expectedLoc.Col)
                            (sprintf "Expected diagnostic to start at column \"%d\" was actually at column \"%d\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                    expectedLoc.Col (actualLinePos.Character + 1) <| describeDiag())
        expectColsToMatch()

    /// <summary>
    /// Checks each of the actual Diagnostics found and compares them with 
    /// the corresponding DiagnosticResult in the array of expected results.
    /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of 
    /// the DiagnosticResult match the actual diagnostic.
    /// </summary>
    /// <param name="analyzer">The analyzer that was being run on the sources</param>
    /// <param name="expected">Diagnostic Results that should have appeared in the code</param>
    /// <param name="actual">The Diagnostics found by the compiler after running the analyzer on the source code</param>
    let diagnosticsToMatch (analyzer: DiagnosticAnalyzer) 
                           (actual: seq<Diagnostic>) (expected: seq<DiagResult>) =
        let expectCountsToMatch() = 
            let expectedCount = expected |> Seq.length
            let actualCount = actual |> Seq.length
            Expect.isTrue (expectedCount = actualCount)
                          (sprintf "Mismatch between number of diagnostics returned, expected \"%d\" actual \"%d\"\r\n\r\nDiagnostics:\r\n%s\r\n"
                                   expectedCount actualCount (if Seq.any actual then analyzer.Format actual else "    NONE."))

        let expectDiagsToMatch (expected: DiagResult, actual: Diagnostic) =
            let describeDiag() = analyzer.Format [actual]
            // Locations are expected to match
            match expected.Location with
            | Some expectedLoc -> expectLocationsToMatch describeDiag (expectedLoc, actual.Location)
            | None -> Expect.isTrue (actual.Location = Location.None)
                                    (sprintf "Expected:\nA project diagnostic with No location\nActual:\n%s"
                                     <| describeDiag())
            // Additional locations are expected to match
            Expect.isTrue (expected.AdditionalLocations.Length = actual.AdditionalLocations.Count)
                          (sprintf "Expected %d additional locations but got %d for Diagnostic:\r\n    %s\r\n"
                                   expected.AdditionalLocations.Length actual.AdditionalLocations.Count <| describeDiag())

            Seq.zip expected.AdditionalLocations actual.AdditionalLocations 
                    |> Seq.iter (expectLocationsToMatch describeDiag)
            // Id, Severity, and Message are expected to match
            Expect.isTrue (actual.Id = expected.Id)
                          (sprintf "Expected diagnostic id to be \"%s\" was \"%s\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                   expected.Id actual.Id <| describeDiag())

            Expect.isTrue (actual.Severity = expected.Severity)
                          (sprintf "Expected diagnostic severity to be \"%A\" was \"%A\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                   expected.Severity actual.Severity <| describeDiag())

            Expect.isTrue ((actual.GetMessage()) = expected.Message)
                          (sprintf "Expected diagnostic message to be \"%s\" was \"%s\"\r\n\r\nDiagnostic:\r\n    %s\r\n"
                                   expected.Message (actual.GetMessage()) <| describeDiag())
        
        expectCountsToMatch()
        Seq.zip expected actual |> Seq.iter expectDiagsToMatch

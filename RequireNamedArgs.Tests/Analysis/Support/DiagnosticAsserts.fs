namespace RequireNamedArgs.Tests.Analysis.Support


open System.Collections.Generic
open System.IO
open System.Runtime.CompilerServices
open System.Text
open Microsoft.CodeAnalysis
open RequireNamedArgs.Tests.Support


[<IsReadOnly; Struct>]
type Mismatch =
    { Expectation: string
      Expected: string
      Actual: string }


[<Sealed; AbstractClass>]
type private Description private () =


    static member Of(location: Location): string =
        match location with
        | location when not location.IsInSource  -> ""
        | location when location = Location.None -> ""
        | location ->
            let fileName = Path.GetFileName(location.SourceTree.FilePath)
            let startPosition = location.GetLineSpan().StartLinePosition
            $"{fileName}({startPosition.Line + 1},{startPosition.Character + 1})"


    static member Of(diagnostic: Diagnostic): string =
        // The final description looks similar to the following:
        // Test0.cs(3,35): Error RequireNamedArgs : `Foo.Bar` must be invoked with named arguments
        $"{Description.Of(diagnostic.Location)}: {diagnostic.Severity} {diagnostic.Id}: {diagnostic.GetMessage()}"


    static member Of(diagnostic: ExpectedDiagnostic): string =
        let locationDescription =
            match diagnostic.Location with
            | None          -> ""
            | Some location ->
                let fileName = Path.GetFileName(location.Path)
                $"{fileName}({location.Line},{location.Column})"

        // The final description looks similar to the following:
        // Test0.cs(3,35): Error RequireNamedArgs : `Foo.Bar` must be invoked with named arguments
        $"{locationDescription}: {diagnostic.Severity} {diagnostic.Id}: {diagnostic.Message}"


    static member Of(mismatches: seq<Mismatch>, diagnostic: Diagnostic): string =
        let sbDescription = StringBuilder()
        for mismatch in mismatches do
            sbDescription.AppendLine(Description.Of(mismatch))
                         |> ignore

        let description =
            sbDescription.AppendLine()
                         .Append("Diagnostic:")
                         .AppendLine()
                         .Append(Description.Of(diagnostic))
                         .AppendLine()
                         .ToString()

        description


    static member Of(mismatch: Mismatch): string =
        $"Expected {mismatch.Expectation} to be {mismatch.Expected} was {mismatch.Actual}"


[<Sealed; AbstractClass>]
type private Mismatches private () =


    static member Of(actual: Diagnostic, expected: ExpectedDiagnostic)
                    : Mismatch[] =

        let mismatches = List<Mismatch>()

        // Locations are expected to match
        match expected.Location with
        | Some expectedLocation ->
            let locationMismatches = Mismatches.Of(actual.Location, expectedLocation)
            mismatches.AddRange(locationMismatches)

        | None ->
            if actual.Location <> Location.None
            then
                mismatches.Add({ Expectation = "the location"
                                 Expected    = "`Location.None`"
                                 Actual      = $"'{Description.Of(actual.Location)}'" })

        mismatches.AddRange(
            Mismatches.OfAdditionalLocations(actual, expected))

        if actual.Id <> expected.Id
        then
            mismatches.Add({ Expectation = "the diagnostic id"
                             Expected    = $"'{expected.Id}'"
                             Actual      = $"'{actual.Id}'" })

        if actual.Severity <> expected.Severity
        then
            mismatches.Add({ Expectation = "the severity"
                             Expected    = $"'{expected.Severity}'"
                             Actual      = $"'{actual.Severity}'" })

        if actual.GetMessage() <> expected.Message
        then
            mismatches.Add({ Expectation = "the message"
                             Expected    = $"'{expected.Message}'"
                             Actual      = $"'{actual.GetMessage()}'" })

        mismatches.ToArray()


    static member OfAdditionalLocations(actual: Diagnostic, expected: ExpectedDiagnostic)
                                       : Mismatch[] =
        let mismatches = List<Mismatch>()

        if actual.AdditionalLocations.Count <> expected.AdditionalLocations.Length
        then
            mismatches.Add({ Expectation = "the number of additional locations"
                             Expected    = $"{expected.AdditionalLocations.Length}"
                             Actual      = $"{actual.AdditionalLocations.Count}" })
            mismatches.ToArray()
        else

        let locations = Seq.zip expected.AdditionalLocations actual.AdditionalLocations
        for expectedLocation, actualLocation in locations do
            mismatches.AddRange(Mismatches.Of(actualLocation, expectedLocation))

        Array.empty


    static member Of(actual: Location, expected: ExpectedLocation)
                    : Mismatch[] =
        let mismatches = List<Mismatch>()

        let actualLineSpan = actual.GetLineSpan()
        if actualLineSpan.Path <> expected.Path
        then
            mismatches.Add({ Expectation = "the diagnostic's file"
                             Expected    = $"'{expected.Path}'"
                             Actual      = $"'{actualLineSpan.Path}'" })

        let actualPosition = actualLineSpan.StartLinePosition

        // Only check line position if the actual diagnostic contains a line
        if actualPosition.Line > 0 &&
           actualPosition.Line + 1 <> expected.Line
        then
            mismatches.Add({ Expectation = "the diagnostic's line"
                             Expected    = $"{expected.Line}"
                             Actual      = $"{actualPosition.Line + 1}" })

        // Only check column position if the actual diagnostic contains a column
        if actualPosition.Character > 0 &&
           actualPosition.Character + 1 <> expected.Column
        then
            mismatches.Add({ Expectation = "the diagnostic's column"
                             Expected    = $"{expected.Column}"
                             Actual      = $"{actualPosition.Character + 1}" })

        mismatches.ToArray()


[<Sealed; AbstractClass; Extension>]
type DiagnosticAsserts private () =


    [<Extension>]
    static member AreEmpty(assert_that: IAssertThat<Diagnostic[]>): unit =
        Assert.That(assert_that.Actual).Match(Array.empty)


    [<Extension>]
    static member Match(assertThat: IAssertThat<Diagnostic[]>,
                        expected: seq<ExpectedDiagnostic>): unit =

        let expectedCount = Seq.length expected
        let actualCount = Seq.length assertThat.Actual

        if actualCount <> expectedCount
        then
            let sbMessage = StringBuilder()

            sbMessage
                .Append($"Expected {expectedCount} diagnostics got {actualCount}")
                .AppendLine()
                .AppendLine()
                |> ignore

            sbMessage
                .AppendLine("Expected diagnostics:") |> ignore

            for expected in expected do
                sbMessage.AppendLine(Description.Of(expected)) |> ignore

            sbMessage
                .AppendLine()
                .AppendLine("Actual diagnostics:") |> ignore

            for actual in assertThat.Actual do
                sbMessage.AppendLine(Description.Of(actual)) |> ignore

            Assert.It.Fails(sbMessage.ToString())

        let mutable anyMismatches = false
        let sbDescription = StringBuilder()

        let diagnostics = Seq.zip expected assertThat.Actual
        for expected, actual in diagnostics do
            let mismatches = Mismatches.Of(actual, expected)
            if mismatches.Length > 0
            then
                anyMismatches <- true
                sbDescription.Append(Description.Of(mismatches, actual))
                             .AppendLine()
                             .AppendLine()
                             |> ignore

        if anyMismatches
        then
            Assert.It.Fails(sbDescription.ToString())

namespace RequireNamedArgs.Tests


open System
open System.Runtime.CompilerServices
open System.Text
open System.Text.RegularExpressions
open RequireNamedArgs.Tests.Support


[<Sealed; AbstractClass; Extension>]
type SourceCodeAsserts private () =


    static let re_ws = Regex(@"\s+", RegexOptions.Compiled)


    [<Extension>]
    static member IsEqualIgnoringWhitespaceTo(assertThat: IAssertThat<string>,
                                              expected: string) : unit =
        let expectedCondensed = re_ws.Replace(expected, "")
        let actualCondensed = re_ws.Replace(assertThat.Actual, "")

        if String.CompareOrdinal(expectedCondensed, actualCondensed) <> 0
        then
            let message =
                StringBuilder()
                    .AppendFormat("EXPECTED [CONDENSED LENGTH = {0}]: ", expectedCondensed.Length)
                    .AppendLine()
                    .AppendLine(expected)
                    .AppendLine()
                    .AppendFormat("ACTUAL [CONDENSED LENGTH = {0}]: ", actualCondensed.Length)
                    .AppendLine()
                    .AppendLine(assertThat.Actual)
                    .ToString()

            Assert.It.Fails(message)

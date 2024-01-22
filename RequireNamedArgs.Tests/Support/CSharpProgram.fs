namespace RequireNamedArgs.Tests.Support

open System
open System.Text


[<RequireQualifiedAccess>]
module CSharpProgram =


    let private ProgramHeaderLines = [|
        "using System;"
        "namespace DragonBall"
        "{"
    |]


    let private ProgramFooterLines = [|
        ""
        "    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]"
        "    class RequireNamedArgsAttribute : Attribute {}"
        ""
        "    class Program { static void Main(string[] args) {} }"
        "}"
    |]


    let private ClassHeaderLines = [|
        "class Character"
        "{"
    |]


    let private ClassFooterLines = [|
        "}"
    |]


    let private Split(text: string): string[] =
        let lines = text.Replace(Environment.NewLine, "\n")
                        .Split('\n')
                         |> Seq.skip 1
                         |> Array.ofSeq

        let trimWidth = lines
                        |> Seq.filter (fun it -> not (String.IsNullOrWhiteSpace(it)))
                        |> Seq.map (fun it -> it.Length - it.TrimStart().Length)
                        |> Seq.min

        let lines = lines
                         |> Seq.map (fun it ->
                               if it.Length > trimWidth
                               then it.Substring(trimWidth)
                               else it)
                         |> Array.ofSeq

        lines


    let WithClasses(classes: string): string =
        let sbProgram = StringBuilder()

        for line in ProgramHeaderLines do
            sbProgram.AppendLine(line) |> ignore

        // let classLines = Split(classes)
        // for line in classLines do
        //     sbProgram.AppendLine($"    {line}") |> ignore

        sbProgram.Append(classes) |> ignore

        for line in ProgramFooterLines do
            sbProgram.AppendLine(line) |> ignore

        sbProgram.ToString()


    let WithStatements(statements: string): string =
        let sbClass = StringBuilder()

        for line in ClassHeaderLines do
            sbClass.AppendLine(line) |> ignore

        // let statementLines = Split(statements)
        // for line in statementLines do
        //     sbClass.AppendLine($"    {line}") |> ignore

        sbClass.Append(statements) |> ignore

        for line in ClassFooterLines do
            sbClass.AppendLine(line) |> ignore

        let csharpClass = sbClass.ToString()

        WithClasses(csharpClass)

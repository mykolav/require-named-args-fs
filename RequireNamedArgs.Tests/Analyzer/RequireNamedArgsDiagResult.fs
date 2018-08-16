module RequireNamedArgs.Tests.Support.RequireNamedArgsDiagResult

open System
open System.Text
open Microsoft.CodeAnalysis
open DiagnosticResult
open RequireNamedArgs.Analyzer

type RequireNamedArgsDiagResult() =

    static member Create(invokedMethod: string,
                         paramNamesByType: string seq seq,
                         fileName: string,
                         line: uint32,
                         column: uint32) =
        let sbParamsDescr = StringBuilder()
        let describeParamGroup (groupSeparator: string) 
                               (paramGroup: seq<string>) =
            sbParamsDescr.Append(groupSeparator)
                         .Append(String.Join(", ", paramGroup 
                                                   |> Seq.map (fun paramName -> "'" + paramName + "'")))
            |> ignore
            " and "
        paramNamesByType |> Seq.fold describeParamGroup "" |> ignore
        let paramsDescr = sbParamsDescr.ToString()
        let message = String.Format(RequireNamedArgsAnalyzer.MessageFormat, 
                                    invokedMethod, 
                                    paramsDescr)
        let diagResult = DiagResult(id       = RequireNamedArgsAnalyzer.DiagnosticId,
                                    message  = message,
                                    severity = DiagnosticSeverity.Error,
                                    location = {Path=fileName; Line=line; Col=column})
        diagResult

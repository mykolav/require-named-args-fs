module RequireNamedArgs.Tests.AnalyzerTests

open Expecto
open Support.RequireNamedArgsDiagResult
open RequireNamedArgs.Analyzer

module private Expect =
    open Support.DiagnosticMatcher
    open Support.DiagnosticProvider
    open Support.DocumentFactory

    let formatSource (source: string) =
        sprintf
            "namespace Frobnitz
            {
                class Wombat
                {
                    %s
                }
                
                [AttributeUsage(AttributeTargets.Method)]
                class RequireNamedArgsAttribute : Attribute {}    
            }" source
    

    let toBeEmittedFrom snippet expectedDiags =
        let code = formatSource snippet
        let analyzer = RequireNamedArgsAnalyzer()
        expectedDiags 
        |> Expect.diagnosticsToMatch analyzer 
                                     (analyzer.GetSortedDiagnostics(CSharp, [code]))

    let emptyDiagnostics snippet = [||] |> toBeEmittedFrom (formatSource snippet)

// TODO: Consider the following use-cases:
// TODO:   Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
// TODO:   Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
// TODO:   `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
// TODO:   `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }
// TODO:   ctor. class C { C(int arg1, int arg2) { new C(1, 2); } }
// TODO:   Attribute's parameters and properties?
[<Tests>]
let analyzerTests = 
    testList "The RequireNamedArgs analyzer tests" [
        test "Empty code does not trigger diagnostics" {
            Expect.emptyDiagnostics @"";
        }
        test "Method w/o params does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                void Gork() {}
                void Bork() { Gork(); } 
            "
        }
        test "Method w/o params w/ [RequireNamedArgs] does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                [RequireNamedArgs]
                void Gork() {}
                void Bork() { Gork(); } 
            "
        }
        test "Method w/o [RequireNamedArgs] does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                class Wombat
                {
                    void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(name: ""Goku"", powerLevel: 9001); }
                } 
            "
        }
        test "Method w/ [RequireNamedArgs] invoked w/ named args does not trigger diagnostics" {
            Expect.emptyDiagnostics @"
                [RequireNamedArgs]
                void TellPowerLevel(string name, int powerLevel) {}
                void Bork() { TellPowerLevel(name: ""Goku"", powerLevel: 9001); }
            "
        }
        test "Method w/ [RequireNamedArgs] invoked w/ positional args triggers diagnostic" {
            let testCodeSnippet = @"
                [RequireNamedArgs]
                void TellPowerLevel(string name, int powerLevel) {}
                void Bork() { TellPowerLevel(""Goku"", 9001); }
            "

            let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="TellPowerLevel",
                                                             paramNamesByType=[[ "line"; "column" ]],
                                                             fileName="Test0.cs", line=8u, column=31u)

            [|expectedDiag|] |> Expect.toBeEmittedFrom testCodeSnippet
        }
        test "Method w/ [RequireNamedArgs] attribute invoked w/ positional args triggers diagnostic" {
            let testCodeSnippet = @"
                [RequireNamedArgs]
                void TellPowerLevel(string name, int powerLevel) {}
                void Bork() { TellPowerLevel(""Goku"", 9001); }
            "

            let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="TellPowerLevel",
                                                             paramNamesByType=[[ "line"; "column" ]],
                                                             fileName="Test0.cs", line=8u, column=31u)

            [|expectedDiag|] |> Expect.toBeEmittedFrom testCodeSnippet
        }
        test "Static method w/ [RequireNamedArgs] invoked w/ positional args triggers diagnostic" {
            let testCodeSnippet = @"
                [RequireNamedArgs]
                static void TellPowerLevel(string name, int powerLevel) {}
                void Bork() { TellPowerLevel(""Goku"", 9001); }
            "

            let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="TellPowerLevel",
                                                             paramNamesByType=[[ "line"; "column" ]],
                                                             fileName="Test0.cs", line=8u, column=31u)

            [|expectedDiag|] |> Expect.toBeEmittedFrom testCodeSnippet
        }
        test "Private static method w/ [RequireNamedArgs] invoked w/ positional args triggers diagnostic" {
            let testCodeSnippet = @"
                [RequireNamedArgs]
                private static void TellPowerLevel(string name, int powerLevel) {}
                void Bork() { TellPowerLevel(""Goku"", 9001); }
            "

            let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="TellPowerLevel",
                                                             paramNamesByType=[[ "line"; "column" ]],
                                                             fileName="Test0.cs", line=8u, column=31u)

            [|expectedDiag|] |> Expect.toBeEmittedFrom testCodeSnippet
        } ]

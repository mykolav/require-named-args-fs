namespace RequireNamedArgs.Tests


open System
open System.Collections.Generic
open System.Threading
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CodeActions
open Microsoft.CodeAnalysis.CodeFixes
open RequireNamedArgs.Analyzer
open RequireNamedArgs.CodeFixProvider
open RequireNamedArgs.Tests.Support
open RequireNamedArgs.Tests.Support.DiagnosticAnalyzerExtensions
open RequireNamedArgs.Tests.CodeFix.Support
open Xunit


[<Sealed; AbstractClass>]
type private CSharpProgram private () =


    static member private FixedFrom(originalSourceCode: string): string =
        let document = Document.SingleFrom(Document.Language.CSharp, originalSourceCode)

        let analyzer = RequireNamedArgsAnalyzer()
        let analyzerDiagnostics = Array.ofSeq (analyzer.Analyze([ document ]))
        let analyzerDiagnostic = analyzerDiagnostics[0]

        let codeFixProvider = RequireNamedArgsCodeFixProvider()
        let codeActions = CSharpProgram.CodeActionsFor(codeFixProvider, document, analyzerDiagnostic)
        let codeAction = codeActions[0]

        let sourceCodeWithAppliedCodeFix =
           document.WithApplied(codeAction)
                   .ToSourceCode()

        sourceCodeWithAppliedCodeFix


    static member private CodeActionsFor(codeFixProvider: CodeFixProvider,
                                         document: Document,
                                         diagnostic: Diagnostic)
                                         : CodeAction[] =
        let codeActions = List<CodeAction>()
        let context = CodeFixContext(document,
                                     diagnostic,
                                     registerCodeFix = Action<_, _>(fun codeAction _ -> codeActions.Add(codeAction)),
                                     cancellationToken = CancellationToken.None)
        codeFixProvider.RegisterCodeFixesAsync(context).Wait()

        Array.ofSeq codeActions


    static member FixedFromClasses(classes: string): string =
        CSharpProgram.FixedFrom(
            CSharpProgram.WithClasses(classes))


    static member FixedFromStatements(statements: string): string =
        CSharpProgram.FixedFrom(
            CSharpProgram.WithStatements(statements))


type CodeFixProviderTests() =


    // TODO: Consider the following use-cases:
    // TODO:   Delegate. class C { void M(System.Action<int, int> f) => f(1, 2); }
    // TODO:   Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO:   `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO:   `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }


    [<Fact>]
    member _.``Invocation of method is fixed to named arguments``() =
        let original = @"
            [RequireNamedArgs]
            void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(""Gizmo.cs"", 9000, 1); }
        "

        let expected = @"
            [RequireNamedArgs]
            void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(original))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expected))


    [<Fact>]
    member _.``Invocation of method is fixed to named arguments preserving trivia``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(
                    ""Gizmo.cs"",


                    9000,
                    1);
            }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            void Gork(string fileName, int line, int column) {}
            void Bork()
            {
                Gork(
                    fileName: ""Gizmo.cs"",


                    line: 9000,
                    column: 1);
            }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of method w/ 1st argument named and 2 arguments positional is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(foo: ""pupper"", ""doggo"", ""woofer""); }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer""); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of method w/ 2nd argument named and 2 arguments positional is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(""pupper"", bar: ""doggo"", ""woofer""); }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer""); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of method w/ 3rd argument named and 2 arguments positional is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(""pupper"", ""doggo"", baz: ""woofer""); }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            void Gork(string foo, string bar, string baz) {}
            void Bork() { Gork(foo: ""pupper"", bar: ""doggo"", baz: ""woofer""); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of static method is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            static void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(""Gizmo.cs"", 9000, 1); }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            static void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of private static method is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            private static void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(""Gizmo.cs"", 9000, 1); }
        "

        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            private static void Gork(string fileName, int line, int column) {}
            void Bork() { Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1); }
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of extension method is fixed to named arguments``() =
        let originalSource = @"
            class Character {}

            static class CharacterExtensions
            {
                [RequireNamedArgs]
                public static void SwitchPowerLevel(this Character character, Action onOver9000, Action onUnderOrAt9000) {}
            }

            class Test { static void Run() { new Character().SwitchPowerLevel(() => {}, () => {}); } }
        "

        let expectedFixedSource = @"
            class Character {}

            static class CharacterExtensions
            {
                [RequireNamedArgs]
                public static void SwitchPowerLevel(this Character character, Action onOver9000, Action onUnderOrAt9000) {}
            }

            class Test { static void Run() { new Character().SwitchPowerLevel(onOver9000: () => {}, onUnderOrAt9000: () => {}); } }
        "

        Assert.That(CSharpProgram.FixedFromClasses(originalSource))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expectedFixedSource))


    [<Fact>]
    member _.``Invocation of constructor is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            public Character(string name, int powerLevel) {}

            public static Character Create() => new Character(""Goku"", 5000);
        "
        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            public Character(string name, int powerLevel) {}

            public static Character Create() => new Character(name: ""Goku"", powerLevel: 5000);
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Implicit invocation of constructor is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            public Character(string name, int powerLevel) {}

            public static Character Create() => new (""Goku"", 5000);
        "
        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            public Character(string name, int powerLevel) {}

            public static Character Create() => new (name: ""Goku"", powerLevel: 5000);
        "

        Assert.That(CSharpProgram.FixedFromStatements(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithStatements(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of attribute constructor is fixed to named arguments``() =
        let originalSnippet = @"
            [PowerLevel(""Goku"", 9001)]
            class Goku { }

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class PowerLevelAttribute : Attribute
            {
                public string Name { get; }
                public int PowerLevel { get; }

                [RequireNamedArgs]
                public PowerLevelAttribute(string name, int powerLevel)
                {
                    Name = name;
                    PowerLevel = powerLevel;
                }
            }
        "
        let expectedFixedSnippet = @"
            [PowerLevel(name: ""Goku"", powerLevel: 9001)]
            class Goku { }

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class PowerLevelAttribute : Attribute
            {
                public string Name { get; }
                public int PowerLevel { get; }

                [RequireNamedArgs]
                public PowerLevelAttribute(string name, int powerLevel)
                {
                    Name = name;
                    PowerLevel = powerLevel;
                }
            }
        "

        Assert.That(CSharpProgram.FixedFromClasses(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expectedFixedSnippet))


    [<Fact>]
    member _.``Invocation of record primary constructor is fixed to named arguments``() =
        let originalSnippet = @"
            [RequireNamedArgs]
            record CharacterRecord(string Name, int PowerLevel)
            {
                public static CharacterRecord CreateGoku() => new CharacterRecord(""Goku"", 9001);
            }
        "
        let expectedFixedSnippet = @"
            [RequireNamedArgs]
            record CharacterRecord(string Name, int PowerLevel)
            {
                public static CharacterRecord CreateGoku() => new CharacterRecord(Name: ""Goku"", PowerLevel: 9001);
            }
        "

        Assert.That(CSharpProgram.FixedFromClasses(originalSnippet))
              .IsEqualIgnoringWhitespaceTo(CSharpProgram.WithClasses(expectedFixedSnippet))

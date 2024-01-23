namespace RequireNamedArgs.Tests


open Microsoft.CodeAnalysis
open RequireNamedArgs.Analyzer
open RequireNamedArgs.Tests.Support
open RequireNamedArgs.Tests.Analysis.Support


[<RequireQualifiedAccess>]
module private Diagnostics =


    let Of(program: string): Diagnostic[] =
        let analyzer = RequireNamedArgsAnalyzer()
        analyzer.Analyze(Document.Language.CSharp, [program])


open Xunit


type AnalyzerTests() =


    // TODO: Consider the following use-cases:
    // TODO:   Delegate. class C { void M(System.Action<int, int> f) => f(1, 2); }
    // TODO:   Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO:   `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO:   `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }


    [<Fact>]
    member _.``Empty code does not trigger diagnostics``() =
        let diagnostics = Diagnostics.Of(CSharpProgram.WithStatements(""))
        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/o params does not trigger diagnostics``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                void Practice() {}
                void Rest() { Practice(); }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/o params w/ [RequireNamedArgs] does not trigger diagnostics``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                [RequireNamedArgs]
                void Practice() {}
                void Rest() { Practice(); }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/o [RequireNamedArgs] does not trigger diagnostics``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                void TellPowerLevel(string name, int powerLevel) {}
                void IntroduceSelf() { TellPowerLevel(name: "Goku", powerLevel: 9001); }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ [RequireNamedArgs] invoked w/ named args does not trigger diagnostics``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                [RequireNamedArgs]
                void TellPowerLevel(string name, int powerLevel) {}
                void IntroduceSelf() { TellPowerLevel(name: "Goku", powerLevel: 9001); }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                [RequireNamedArgs]
                void TellPowerLevel(string name, int powerLevel) {}
                void IntroduceSelf() { TellPowerLevel("Goku", 9001); }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character.TellPowerLevel",
            fileName         = "Test0.cs",
            line             = 9,
            column           = 40)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Static method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                [RequireNamedArgs]
                static void TellPowerLevel(string name, int powerLevel) {}
                void IntroduceSelf() { TellPowerLevel("Goku", 9001); }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character.TellPowerLevel",
            fileName         = "Test0.cs",
            line             = 9,
            column           = 40)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Private static method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithStatements("""
                [RequireNamedArgs]
                private static void TellPowerLevel(string name, int powerLevel) {}
                void IntroduceSelf() { TellPowerLevel("Goku", 9001); }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character.TellPowerLevel",
            fileName         = "Test0.cs",
            line             = 9,
            column           = 40)

        Assert.That(diagnostics).Match([ expected ])


    // See https://github.com/mykolav/require-named-args-fs/issues/1
    [<Fact>]
    member _.``Extension method w/o [RequireNamedArgs] does not triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                static class PowerLevelExtensions
                {
                    public static void HandlePowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                }

                class Character
                {
                    void Bork() { "Goku".HandlePowerLevel(() => {}, () => {}); }
                }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Extension method  w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                static class PowerLevelExtensions
                {
                    [RequireNamedArgs]
                    public static void SwitchPowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                }

                class Character
                {
                    void Bork() { "Goku".SwitchPowerLevel(onOver9000: () => {}, onUnderOrAt9000: () => {}); }
                }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Extension method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                static class PowerLevelExtensions
                {
                    [RequireNamedArgs]
                    public static void SwitchPowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                }

                class Character
                {
                    void Bork() { "Goku".SwitchPowerLevel(() => {}, () => {}); }
                }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "PowerLevelExtensions.SwitchPowerLevel",
            fileName         = "Test0.cs",
            line             = 13,
            column           = 35)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                class Character
                {
                    public string Name { get; }
                    public int PowerLevel { get; }

                    [RequireNamedArgs]
                    public Character(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                    public static Character Create()
                    {
                        return new Character(name: "Goku", powerLevel: 5000);
                    }
                }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                class Character
                {
                    public string Name { get; }
                    public int PowerLevel { get; }

                    [RequireNamedArgs]
                    public Character(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                    public static Character Create()
                    {
                        return new Character("Goku", 5000);
                    }
                }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character..ctor",
            fileName         = "Test0.cs",
            line             = 15,
            column           = 32)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Constructor w/ [RequireNamedArgs] invoked implicitly w/ named args does not trigger the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                class Character
                {
                    public string Name { get; }
                    public int PowerLevel { get; }

                    [RequireNamedArgs]
                    public Character(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                    public static Character Create()
                    {
                        return new(name: "Goku", powerLevel: 5000);
                    }
                }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Constructor w/ [RequireNamedArgs] invoked implicitly w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                class Character
                {
                    public string Name { get; }
                    public int PowerLevel { get; }

                    [RequireNamedArgs]
                    public Character(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                    public static Character Create()
                    {
                        return new("Goku", 5000);
                    }
                }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character..ctor",
            fileName         = "Test0.cs",
            line             = 15,
            column           = 32)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Attribute constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                [PowerLevel(name: "Goku", powerLevel: 9001)]
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
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Attribute constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                [PowerLevel("Goku", 9001)]
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
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "PowerLevelAttribute..ctor",
            fileName         = "Test0.cs",
            line             = 5,
            column           = 18)

        Assert.That(diagnostics).Match([ expected ])


    [<Fact>]
    member _.``Record constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                [RequireNamedArgs]
                record Character(string Name, int PowerLevel)
                {
                    public static Character Create()
                    {
                        return new Character(Name: "Goku", PowerLevel: 5000);
                    }
                }
                """))

        Assert.That(diagnostics).AreEmpty()


    [<Fact>]
    member _.``Record constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic``() =
        let diagnostics =
            Diagnostics.Of(CSharpProgram.WithClasses("""
                [RequireNamedArgs]
                record Character(string Name, int PowerLevel)
                {
                    public static Character Create()
                    {
                        return new Character("Goku", 5000);
                    }
                }
                """))

        let expected = ExpectedDiagnostic.RequireNamedArgs(
            invokedMethod    = "Character..ctor",
            fileName         = "Test0.cs",
            line             = 10,
            column           = 32)

        Assert.That(diagnostics).Match([ expected ])

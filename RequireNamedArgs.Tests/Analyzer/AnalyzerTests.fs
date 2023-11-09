namespace RequireNamedArgs.Tests


module private ExpectDiags =
    
    
    open RequireNamedArgs.Analyzer
    open RequireNamedArgs.Tests.Support
    open RequireNamedArgs.Tests.Support.DiagnosticProvider
    open RequireNamedArgs.Tests.Support.DocumentFactory


    let toBeEmittedFrom snippet expectedDiags =
        let code = Format.program snippet
        let analyzer = RequireNamedArgsAnalyzer()
        
        expectedDiags |> Expect.diagsToMatch analyzer (analyzer.GetSortedDiagnostics(CSharp, [code]))

    
    let emptyDiagnostics snippet = [||] |> toBeEmittedFrom snippet


module AnalyzerTests =


    open Expecto
    open RequireNamedArgs.Tests.Support


    // TODO: Consider the following use-cases:
    // TODO:   Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
    // TODO:   Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO:   `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO:   `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }
    [<Tests>]
    let analyzerTests = 
        testList "The RequireNamedArgs analyzer tests" [
            test "Empty code does not trigger diagnostics" {
                ExpectDiags.emptyDiagnostics (Format.klass @"");
            }
            test "Method w/o params does not trigger diagnostics" {
                ExpectDiags.emptyDiagnostics (Format.klass @"
                    void Gork() {}
                    void Bork() { Gork(); } 
                ")
            }
            test "Method w/o params w/ [RequireNamedArgs] does not trigger diagnostics" {
                ExpectDiags.emptyDiagnostics (Format.klass @"
                    [RequireNamedArgs]
                    void Gork() {}
                    void Bork() { Gork(); } 
                ")
            }
            test "Method w/o [RequireNamedArgs] does not trigger diagnostics" {
                ExpectDiags.emptyDiagnostics (Format.klass @"
                    void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(name: ""Goku"", powerLevel: 9001); }
                ")
            }
            test "Method w/ [RequireNamedArgs] invoked w/ named args does not trigger diagnostics" {
                ExpectDiags.emptyDiagnostics (Format.klass @"
                    [RequireNamedArgs]
                    void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(name: ""Goku"", powerLevel: 9001); }
                ")
            }
            test "Method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = (Format.klass @"
                    [RequireNamedArgs]
                    void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(""Goku"", 9001); }
                ")

                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat.TellPowerLevel",
                                                                 paramNamesByType=[[ "line"; "column" ]],
                                                                 fileName="Test0.cs", line=9u, column=35u)

                [|expectedDiag|] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Method w/ [RequireNamedArgs] attribute invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = (Format.klass @"
                    [RequireNamedArgs]
                    void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(""Goku"", 9001); }
                ")

                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat.TellPowerLevel",
                                                                 paramNamesByType=[[ "line"; "column" ]],
                                                                 fileName="Test0.cs", line=9u, column=35u)

                [|expectedDiag|] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Static method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = (Format.klass @"
                    [RequireNamedArgs]
                    static void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(""Goku"", 9001); }
                ")

                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat.TellPowerLevel",
                                                                 paramNamesByType=[[ "line"; "column" ]],
                                                                 fileName="Test0.cs", line=9u, column=35u)

                [|expectedDiag|] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Private static method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = (Format.klass @"
                    [RequireNamedArgs]
                    private static void TellPowerLevel(string name, int powerLevel) {}
                    void Bork() { TellPowerLevel(""Goku"", 9001); }
                ")

                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat.TellPowerLevel",
                                                                 paramNamesByType=[[ "line"; "column" ]],
                                                                 fileName="Test0.cs", line=9u, column=35u)

                [|expectedDiag|] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            // See https://github.com/mykolav/require-named-args-fs/issues/1
            test "Extension method w/o [RequireNamedArgs] does not triggers the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
                    static class PowerLevelExtensions
                    {
                        public static void SwitchPowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                    }
                    
                    class Wombat
                    {
                        void Bork() { ""Goku"".SwitchPowerLevel(() => {}, () => {}); }
                    }
                "
            }
            test "Extension method  w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
                    static class PowerLevelExtensions
                    {
                        [RequireNamedArgs]
                        public static void SwitchPowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                    }
                    
                    class Wombat
                    {
                        void Bork() { ""Goku"".SwitchPowerLevel(onOver9000: () => {}, onUnderOrAt9000: () => {}); }
                    }
                "
            }
            test "Extension method w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = @"
                    static class PowerLevelExtensions
                    {
                        [RequireNamedArgs]
                        public static void SwitchPowerLevel(this string name, Action onOver9000, Action onUnderOrAt9000) {}
                    }
                    
                    class Wombat
                    {
                        void Bork() { ""Goku"".SwitchPowerLevel(() => {}, () => {}); }
                    }
                "

                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="PowerLevelExtensions.SwitchPowerLevel",
                                                                 paramNamesByType=[[ "line"; "column" ]],
                                                                 fileName="Test0.cs", line=13u, column=39u)

                [| expectedDiag |] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
                    class Wombat
                    {
                        public string Name { get; }
                        public int PowerLevel { get; }

                        [RequireNamedArgs]
                        public Wombat(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                        public static Wombat Create()
                        {
                            return new Wombat(name: ""Goku"", powerLevel: 5000);
                        }
                    }
                "
            }
            test "Constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = @"
                    class Wombat
                    {
                        public string Name { get; }
                        public int PowerLevel { get; }

                        [RequireNamedArgs]
                        public Wombat(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                        public static Wombat Create()
                        {
                            return new Wombat(""Goku"", 5000);
                        }
                    }
                "
                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat..ctor",
                                                                paramNamesByType=[[ "line"; "column" ]],
                                                                fileName="Test0.cs", line=15u, column=36u)

                [| expectedDiag |] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Constructor w/ [RequireNamedArgs] invoked implicitly w/ named args does not trigger the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
                    class Wombat
                    {
                        public string Name { get; }
                        public int PowerLevel { get; }

                        [RequireNamedArgs]
                        public Wombat(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                        public static Wombat Create()
                        {
                            return new(name: ""Goku"", powerLevel: 5000);
                        }
                    }
                "
            }
            test "Constructor w/ [RequireNamedArgs] invoked implicitly w/ positional args triggers the diagnostic" {
                let testCodeSnippet = @"
                    class Wombat
                    {
                        public string Name { get; }
                        public int PowerLevel { get; }

                        [RequireNamedArgs]
                        public Wombat(string name, int powerLevel) => (Name, PowerLevel) = (name, powerLevel);

                        public static Wombat Create()
                        {
                            return new(""Goku"", 5000);
                        }
                    }
                "
                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat..ctor",
                                                                paramNamesByType=[[ "line"; "column" ]],
                                                                fileName="Test0.cs", line=15u, column=36u)

                [| expectedDiag |] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Attribute constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
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
            }
            test "Attribute constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = @"
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
                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="PowerLevelAttribute..ctor",
                                                                paramNamesByType=[[ "line"; "column" ]],
                                                                fileName="Test0.cs", line=5u, column=22u)

                [| expectedDiag |] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
            test "Record constructor w/ [RequireNamedArgs] invoked w/ named args does not trigger the diagnostic" {
                ExpectDiags.emptyDiagnostics @"
                    [RequireNamedArgs]
                    record Wombat(string Name, int PowerLevel)
                    {
                        public static Wombat Create()
                        {
                            return new Wombat(Name: ""Goku"", PowerLevel: 5000);
                        }
                    }
                "
            }
            test "Record constructor w/ [RequireNamedArgs] invoked w/ positional args triggers the diagnostic" {
                let testCodeSnippet = @"
                    [RequireNamedArgs]
                    record Wombat(string Name, int PowerLevel)
                    {
                        public static Wombat Create()
                        {
                            return new Wombat(""Goku"", 5000);
                        }
                    }
                "
                let expectedDiag = RequireNamedArgsDiagResult.Create(invokedMethod="Wombat..ctor",
                                                                paramNamesByType=[[ "line"; "column" ]],
                                                                fileName="Test0.cs", line=10u, column=36u)

                [| expectedDiag |] |> ExpectDiags.toBeEmittedFrom testCodeSnippet
            }
        ]

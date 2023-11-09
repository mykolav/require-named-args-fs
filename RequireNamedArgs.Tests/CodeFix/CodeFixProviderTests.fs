namespace RequireNamedArgs.Tests


open Expecto
open RequireNamedArgs.Analyzer
open RequireNamedArgs.CodeFixProvider
open RequireNamedArgs.Tests.Support
open RequireNamedArgs.Tests.Support.DocumentFactory


[<RequireQualifiedAccess>]
module private ExpectCode =


    let toBeFixedAndMatch expectedFixedSource originalSource =
        CodeFixExpect.expectedFixedCodeToMatchActualFixedCode
            (RequireNamedArgsAnalyzer())
            (RequireNamedArgsCodeFixProvider())
            CSharp
            originalSource
            (*codeFixIndex=*) None
            (*allowNewCompilerDiags=*) false
            expectedFixedSource


    let snippetToBeFixedAndMatch expectedFixedSnippet originalSnippet =
        let expectedFixedSource = Format.program (Format.klass expectedFixedSnippet)
        let originalSource = Format.program (Format.klass originalSnippet)
        toBeFixedAndMatch expectedFixedSource originalSource        


module CodeFixProviderTests =
    // TODO: Delegate. class C { void M(System.Action<int, int> f) => f(1, 2);
    // TODO: Indexer. class C { int this[int arg1, int arg2] => this[1, 2]; }
    // TODO: `this` ctor initializer. class C { C(int arg1, int arg2) {} C() : this(1, 2) {} }
    // TODO: `base` ctor initializer. class C { public C(int arg1, int arg2) {} } class D : C { D() : base(1, 2) {} }
    // TODO: Attribute's parameters and properties?
    [<Tests>]
    let codeFixProviderTests = 
        testList "The RequireNamedCodeFixProvider code-fix provider tests" [
            testList "[RequireNamedArgs] method" [
                test "Invocation w/ positional args is fixed to named args" {
                    let originalSnippet = @"
                        [RequireNamedArgs]
                        void Gork(string fileName, int line, int column) {}
                        void Bork() { Gork(""Gizmo.cs"", 9000, 1); }
                    "

                    let expectedFixedSnippet = @"
                        [RequireNamedArgs]
                        void Gork(string fileName, int line, int column) {}
                        void Bork() { Gork(fileName: ""Gizmo.cs"", line: 9000, column: 1); }
                    "

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                } 
                test "Invocation w/ positional args is fixed to named args preserving trivia" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                } 
                test "Invocation w/ 1st arg named and 2 positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }
                test "Invocation w/ 2nd arg named and 2 positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }
                test "Invocation w/ 3rd arg named and 2 positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                } ]
            testList "static [RequireNamedArgs] method" [
                test "Invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                } ]
            testList "private static [RequireNamedArgs] method" [
                test "Invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }]
            testList "extension [RequireNamedArgs] method" [
                test "Invocation w/ positional args is fixed to named args" {
                    let originalSource = Format.program @"
                        class Character {}

                        static class CharacterExtensions
                        {
                            [RequireNamedArgs]
                            public static void SwitchPowerLevel(this Character character, Action onOver9000, Action onUnderOrAt9000) {}
                        }

                        class Test { static void Run() { new Character().SwitchPowerLevel(() => {}, () => {}); } }
                    "

                    let expectedFixedSource = Format.program @"
                        class Character {}

                        static class CharacterExtensions
                        {
                            [RequireNamedArgs]
                            public static void SwitchPowerLevel(this Character character, Action onOver9000, Action onUnderOrAt9000) {}
                        }

                        class Test { static void Run() { new Character().SwitchPowerLevel(onOver9000: () => {}, onUnderOrAt9000: () => {}); } }
                    "

                    originalSource |> ExpectCode.toBeFixedAndMatch expectedFixedSource
                }]
            testList "Constructor w/ [RequireNamedArgs]" [
                test "Invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }
                test "Implicit invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }]
            testList "Attribute constructor w/ [RequireNamedArgs]" [
                test "Invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }]
            testList "Record w/ [RequireNamedArgs]" [
                test "Primary constructor invocation w/ positional args is fixed to named args" {
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

                    originalSnippet |> ExpectCode.snippetToBeFixedAndMatch expectedFixedSnippet
                }]
        ]

# Require a method to be invoked with named arguments

[![Build status](https://ci.appveyor.com/api/projects/status/uucjd3ti7tjmmn9d?svg=true)](https://ci.appveyor.com/project/mykolav/require-named-args-fs)

This project contains a Roslyn code analyzer and an accompanying code-fix provider that let you [force named arguments in C#](https://stackoverflow.com/questions/11300645/forcing-named-arguments-in-c-sharp).

![The RequireNamedArgs analyzer in action](./require-named-args-demo.gif)

## How to use it?

Install the [nuget package](https://www.nuget.org/packages/RequireNamedArgs/).

Introduce a `RequireNamedArgsAttribute` attribute to your solution. In other words, place the following C# code in an appropriate spot in your solution.

```csharp
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
class RequireNamedArgsAttribute : Attribute {}
```

Apply the `[RequireNamedArgs]` attribute to the methods that should only be called with named arguments. For example:

```csharp
[RequireNamedArgs]
public static void TellPowerLevel(string name, int powerLevel) {}

// Elsewhere in your code:
// if `TellPowerLevel` method is called with positional arguments,
// the analyzer will emit an error.
TellPowerLevel(name: "Goku", powerLevel: 9001);
```

### Supported method kinds

The analyzer supports requiring named arguments for the following method kinds  
- Regular instance and static methods
- Extension methods
- Regular constructors
- Attribute constructors
- Primary constructors 

To mark a record's primary constructor, apply `[RequireNamedArgs]` to the record itself.

```csharp
[RequireNamedArgs]
record Character(string Name, int PowerLevel) {}

[RequireNamedArgs]
record struct CharacterStruct(string Name, int PowerLevel) {}

// Elsewhere in your code:
// if the primary constructor of `Character` or `CharacterStruct` is called with positional arguments,
// the analyzer will emit an error.
new Character(Name: "Goku", PowerLevel: 9001);
new CharacterStruct(Name: "Goku", PowerLevel: 9001);
```

Please note, in the code above the attribute only applies to the primary constructors of the records. If a record has additional constructors, you can mark them with this attribute individually in a usual way.

## Download and install

Install the [RequireNamedArgs](https://www.nuget.org/packages/RequireNamedArgs) nuget package.
For example, run the following command in the [NuGet Package Manager Console](https://docs.microsoft.com/en-us/nuget/tools/package-manager-console).

```powershell
Install-Package RequireNamedArgs
```

This will download all the binaries, and add necessary analyzer references to your project.

## Configuration

Starting in Visual Studio 2019 version 16.3, you can [configure the severity of analyzer rules, or diagnostics](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels), in an EditorConfig file, from the light bulb menu, and the error list.

You can add the following to the `[*.cs]` section of your .editorconfig.

```ini
[*.cs]
dotnet_diagnostic.RequireNamedArgs.severity = error
```

The possible severity values are:
- `error`
- `warning`
- `suggestion`
- `silent`
- `none`
- `default` (in case of this analyzer, it's equal to `error`)

Please take a look at [the documentation](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022#configure-severity-levels) for a detailed description.

## How does it work?

1. This analyzer looks at an invocation expression (e.g., a method call).
2. It then finds the method's definition.
3. If the definition is marked with a `[RequireNamedArgs]` attribute,  
   the analyzer requires every caller to provide names for the invocation's arguments.
4. If the last parameter is `params`, the analyzer  
   doesn't emit the diagnostic, as C# doesn't allow named arguments in this case.

## Technical details

The analyzer, code-fix provider, and tests are implemented in F#

# Thank you!

- [John Koerner](https://github.com/johnkoerner) for [Creating a Code Analyzer using F#](https://johnkoerner.com/code-analysis/creating-a-code-analyzer-using-f/)
- [Dustin Campbell](https://github.com/DustinCampbell) for [CSharpEssentials](https://github.com/DustinCampbell/CSharpEssentials)
- [Alireza Habibi](https://github.com/alrz) for [CSharpUseNamedArgumentsCodeRefactoringProvider](https://github.com/dotnet/roslyn/blob/master/src/Features/CSharp/Portable/UseNamedArguments/CSharpUseNamedArgumentsCodeRefactoringProvider.cs) which provided very useful code examples.
- [Steve Smith](https://ardalis.com/) for his article [Improve Tests with the Builder Pattern for Test Data](https://ardalis.com/improve-tests-with-the-builder-pattern-for-test-data).

# License

The [RequireNamedArgs](https://github.com/mykolav/require-named-args-fs) analyzer and code-fix provider are licensed under the MIT license.  
So they can be used freely in commercial applications.

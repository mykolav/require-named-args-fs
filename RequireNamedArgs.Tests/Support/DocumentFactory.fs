module RequireNamedArgs.Tests.Support.DocumentFactory

open System.Linq
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Text

let DefaultFilePathPrefix = "Test";
let CSharpDefaultFileExt = "cs";
let VisualBasicDefaultExt = "vb";
let TestProjectName = "TestProject";

type Langs =
    | CSharp
    | VisualBasic
    with override this.ToString() =
                match this with
                | CSharp -> LanguageNames.CSharp
                | VisualBasic -> LanguageNames.VisualBasic

/// <summary>
/// Create a project using the inputted strings as sources.
/// </summary>
/// <param name="sources">Classes in the form of strings</param>
/// <param name="language">The language the source code is in</param>
/// <returns>A Project created out of the Documents created from the source strings</returns>
let private mkProject(sources: seq<string>, lang: Langs) =
    let CorlibRef = MetadataReference.CreateFromFile(typedefof<obj>.Assembly.Location)
    let SystemCoreRef = MetadataReference.CreateFromFile(typedefof<Enumerable>.Assembly.Location)
    let CSharpSymbolsRef = MetadataReference.CreateFromFile(typedefof<CSharpCompilation>.Assembly.Location)
    let CodeAnalysisRef = MetadataReference.CreateFromFile(typedefof<Compilation>.Assembly.Location)

    let fileNamePrefix = DefaultFilePathPrefix
    let fileExt = if lang = Langs.CSharp then CSharpDefaultFileExt else VisualBasicDefaultExt

    let projId = ProjectId.CreateNewId(debugName=TestProjectName)

    let solution = (new AdhocWorkspace())
                        .CurrentSolution
                        .AddProject(projId, TestProjectName, TestProjectName, lang.ToString())
                        .AddMetadataReference(projId, CorlibRef)
                        .AddMetadataReference(projId, SystemCoreRef)
                        .AddMetadataReference(projId, CSharpSymbolsRef)
                        .AddMetadataReference(projId, CodeAnalysisRef)

    let addSource (solution: Solution, count: uint32) (source: string) =
        let fileName = fileNamePrefix + string count + "." + fileExt
        let docId = DocumentId.CreateNewId(projId, debugName=fileName)
        (solution.AddDocument(docId, fileName, SourceText.From(source)), 
         count + 1u)

    let solution, _ = sources |> Seq.fold addSource (solution, 0u)
    solution.GetProject(projId)

/// <summary>
/// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
/// </summary>
/// <param name="sources">Classes in the form of strings</param>
/// <param name="language">The language the source code is in</param>
/// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
let mkDocuments(sources: seq<string>, lang: Langs) =
    let project = mkProject(sources, lang)
    let documents = List.ofSeq project.Documents 

    if Seq.length sources = documents.Length 
    then documents
    else failwith "Amount of sources did not match amount of Documents created"

/// <summary>
/// Create a Document from a string through creating a project that contains it.
/// </summary>
/// <param name="source">Classes in the form of a string</param>
/// <param name="language">The language the source code is in</param>
/// <returns>A Document created from the source string</returns>
let mkDocument(source: string, lang: Langs) =
    mkDocuments([ source ], lang) |> Seq.exactlyOne

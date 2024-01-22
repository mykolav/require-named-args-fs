namespace RequireNamedArgs.Tests.Support


open System.Linq
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.VisualBasic


[<RequireQualifiedAccess>]
module Document =


    type Language =
        | CSharp
        | VisualBasic
        with
        override this.ToString(): string =
            match this with
            | CSharp -> LanguageNames.CSharp
            | VisualBasic -> LanguageNames.VisualBasic


    let private TestProjectName = "TestProject";


    let private ProjectWith(language: Language, sources: seq<string>): Project =
        let projectId = ProjectId.CreateNewId(debugName=TestProjectName)

        let parseOptions =
            if language = Language.CSharp
            then CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11) :> ParseOptions
            else VisualBasicParseOptions.Default :> ParseOptions

        let corlibRef = MetadataReference.CreateFromFile(typedefof<obj>.Assembly.Location)
        let systemCoreRef = MetadataReference.CreateFromFile(typedefof<Enumerable>.Assembly.Location)
        let csharpSymbolsRef = MetadataReference.CreateFromFile(typedefof<CSharpCompilation>.Assembly.Location)
        let codeAnalysisRef = MetadataReference.CreateFromFile(typedefof<Compilation>.Assembly.Location)

        let mutable solution =
            (new AdhocWorkspace())
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language.ToString())
                .WithProjectParseOptions(projectId, parseOptions)
                .AddMetadataReference(projectId, corlibRef)
                .AddMetadataReference(projectId, systemCoreRef)
                .AddMetadataReference(projectId, csharpSymbolsRef)
                .AddMetadataReference(projectId, codeAnalysisRef)

        let mutable i = 0
        for source in sources do
            let fileExtension = if language = Language.CSharp then "cs" else "vb"
            let fileName = $"Test{i}.{fileExtension}"
            solution <- solution.AddDocument(
                documentId=DocumentId.CreateNewId(projectId, debugName=fileName),
                name=fileName,
                text=SourceText.From(source))

            i <- i + 1

        solution.GetProject(projectId)


    let From(language: Language, sources: seq<string>): Document[] =
        let project = ProjectWith(language, sources)
        let documents = Array.ofSeq project.Documents

        let numberOfSources = Seq.length sources
        if numberOfSources <> documents.Length
        then
            failwith ($"The number of sources {numberOfSources} does not match " +
                      $"the number of created documents {documents.Length}")
        else

        documents


    let SingleFrom(language: Language, source: string): Document =
        let documents = From(language, [ source ])
        documents[0]

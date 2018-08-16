open Expecto

[<EntryPoint>]
let main args =
    let result = runTestsInAssembly defaultConfig args
    result
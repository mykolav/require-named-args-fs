open Expecto


[<EntryPoint>]
let main args =
    let result = runTestsInAssemblyWithCLIArgs [] args
    result

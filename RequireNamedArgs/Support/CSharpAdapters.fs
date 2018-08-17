module RequireNamedArgs.CSharpAdapters

type Option<'a> with
    static member ofType<'Derived when 'Derived : null> (baseObj: obj) = 
        match baseObj with 
        | :? 'Derived as derivedObj -> Some derivedObj
        | _                         -> None
    static member ofList list =
        match list with
        | [] -> None
        | _  -> Some list


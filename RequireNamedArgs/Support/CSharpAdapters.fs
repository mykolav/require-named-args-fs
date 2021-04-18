namespace RequireNamedArgs.Support


module CSharpAdapters =
    let asOptional<'Derived when 'Derived : null> (baseObj: obj) = 
        match baseObj with 
        | :? 'Derived as derivedObj -> Some derivedObj
        | _                         -> None


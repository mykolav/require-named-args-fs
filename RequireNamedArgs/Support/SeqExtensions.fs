module Seq

let count predicate source =
    source |> Seq.fold (fun acc it -> if predicate it then acc + 1 else acc) 0

let any source = source |> Seq.isEmpty |> not

let ofType<'a> source = 
    source |> Seq.choose (fun it -> match box it with
                                    | :? 'a as a -> Some a
                                    | _          -> None)

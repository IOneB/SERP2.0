module Repository

open Giraffe
open DbContext

let anyUsers : HttpHandler = 
    use context = new UserContext()
    if Seq.isEmpty context.Users then "none"
    else "not none"
    |> text

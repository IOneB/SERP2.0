module Repository

open DbContext
open SERP.Entities
open System
open Actions

let user = 
    use db = new UserContext()
    let tryFind = 
        db.Users
        |> Seq.tryFind (fun u -> u.Name = Environment.UserName)
    match tryFind with
        | Some user -> user
        | None ->
            let newUser = 
                { Default.defaultUser with 
                    UserName = Environment.UserName
                    Name = Environment.UserName }
            db.Users.Add(newUser) |> ignore
            db.SaveChanges true |> ignore
            newUser

let allResults() = 
    use db = new UserContext()
    db.Results 
    |> Seq.toList
    |> List.map (fun res -> {User = user; Id = res.ResultID; Date = res.Date.ToLongDateString(); Time = res.Date.ToLongTimeString(); ResultType = resultTypeTranslator res.ResultType; Count = res.Count; Result = res.ResultValues.Replace(";", "; ")})


let saveResult security =
    use db = new UserContext()
    let saveRes = db.Results.Add(security)
    db.SaveChanges true, saveRes.Entity.ResultID

let myResults () =
    let me = user
    {Me = user.Name; Results = allResults()}

module Repository

open DbContext
open SERP.Entities
open SERP.Entities.Default
open Actions

type User with
    member this.Results = 
        use db = new UserContext()
        Seq.filter (fun r -> r.UserID = this.UserID) db.Results
        |> Seq.toList

let getUserById id =
    use db = new UserContext()
    Seq.find (fun (u : User) -> u.UserID = id) db.Users

let allResults = 
    use db = new UserContext()
    db.Results 
    |> Seq.toList
    |> List.map (fun res -> {User = getUserById res.UserID; Id = res.ResultID; Date = res.Date.ToLongDateString(); Time = res.Date.ToLongTimeString(); ResultType = resultTypeTranslator res.ResultType; Count = res.Count; Result = res.ResultValues.Replace(";", "; ")})

let checkUserByName userName = 
    use db = new UserContext()
    Seq.exists (fun user -> user.UserName = userName) db.Users 

let getUserByName userName =
    use db = new UserContext()
    Seq.tryFind (fun user -> user.UserName = userName) db.Users

let createUser (model : RegisterModel) = 
    use db = new UserContext()
    let newUser = {defaultUser with UserName = model.UserName; Password = GetHash model.Password; Name = model.Name; Group = defaultArg model.Group ""; Role = setRole model.TeacherCode }
    db.Users.Add(newUser) |> ignore
    db.SaveChanges true

let saveResult security =
    use db = new UserContext()
    let saveRes = db.Results.Add(security)
    db.SaveChanges true, saveRes.Entity.ResultID

let myResults name =
    let user = getUserByName name

    let me = 
        match user with
        | Some user -> user.Name + " " + user.Group
        | _-> "Unknown"
    let results =
        match user with
        |Some user -> 
            user.Results
            |> List.map (fun res -> {Id = res.ResultID; User = user; Date = res.Date.ToLongDateString(); Time = res.Date.ToLongTimeString(); ResultType = resultTypeTranslator res.ResultType; Count = res.Count; Result = res.ResultValues.Replace(";", "; ")})
        | None -> []
    {Me = me; Results = results}

module Repository

open DbContext
open SERP.Entities
open SERP.Entities.Default
open Actions

let allUsers _ = 
    use db = new UserContext()
    db.Users |> Seq.toList

let checkUserByName userName = 
    use db = new UserContext()
    Seq.exists (fun user -> user.UserName = userName) db.Users 

let getUserByName userName =
    use db = new UserContext()
    Seq.tryFind (fun user -> user.UserName = userName) db.Users

let createUser (model : RegisterModel) = 
    use db = new UserContext()
    let newUser = {defaultUser with UserName = model.UserName; Password = GetHash model.Password; Name = model.Name; Group = getParam model.Group; Role = setRole model.TeacherCode }
    db.Users.Add(newUser) |> ignore
    db.SaveChanges true
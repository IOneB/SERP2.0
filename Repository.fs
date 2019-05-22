module Repository

open Giraffe
open DbContext
open SERP.Entities
open SERP.Entities.Default
open Actions
open Microsoft.AspNetCore.Http

let db = new UserContext()

let allUsers = (fun _ -> 
    [for i in db.Users -> i])

let checkUserByName userName = 
    Seq.exists (fun user -> user.UserName = userName) db.Users 

let getUserByName userName =
    Seq.tryFind (fun user -> user.UserName = userName) db.Users

let createUser (model : RegisterModel) = 
    let newUser = {defaultUser with UserName = model.UserName; Password = model.Password; Name = model.Name; Group = model.Group; Role = setRole model.TeacherCode }
    db.Users.Add(newUser) |> ignore
    db.SaveChanges true

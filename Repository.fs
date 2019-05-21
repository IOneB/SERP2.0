module Repository

open Giraffe
open DbContext
open SERP.Entities
open SERP.Entities.Default
open Actions
open Microsoft.AspNetCore.Http

let allUsers = (fun (next, context:HttpContext) -> 
    let db = context.GetService<UserContext>()
    [for i in db.Users -> i])
    

let checkUserByName (context:HttpContext) userName = 
    let db = context.GetService<UserContext>()
    Seq.exists (fun user -> user.UserName = userName) db.Users 

let createUser (context:HttpContext) (model : RegisterModel) = 
    let db = context.GetService<UserContext>()
    let newUser = {defaultUser with UserName = model.UserName; Password = model.Password; Name = model.Name; Group = model.Group; Role = setRole model.TeacherCode }
    db.Users.Add(newUser) |> ignore
    db.SaveChanges true

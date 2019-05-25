namespace SERP.Entities

open System.ComponentModel.DataAnnotations
open Giraffe

type [<CLIMutable>] RegisterModel = 
    {
        UserName : string
        Password : string
        ConfirmPassword : string
        Name : string
        Group : string
        TeacherCode : option<string>
    }

    member this.HasErrors() =
        if      this.UserName.Length < 5                then Some "UserName is too short."
        else if this.Password.Length  < 3               then Some "Password is too short."
        else if this.Password.Length  > 20              then Some "Password is too long."
        else if this.Password <> this.ConfirmPassword   then Some "Passwords do not match."
        else if this.Name.Length < 4                    then Some "Name is too short."
        else if this.Group.Length > 7                   then Some "Group is too short."
        else if this.Group.Length < 3                   then Some "Group is too long."
        else None

    interface IModelValidation<RegisterModel> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.badRequest (text msg))
            | None     -> Ok this

type [<CLIMutable>] LoginModel = 
    { 
        UserName : string
        Password : string 
    }

    member this.HasErrors() =
        if      this.UserName.Length = 0                then Some "Введите имя пользователя."
        else if this.Password.Length = 0                then Some "Введите пароль."
        else None

    interface IModelValidation<LoginModel> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.badRequest (text msg))
            | None     -> Ok this


type [<CLIMutable>] User = { UserID:int;Name:string; Password:string; UserName: string; Role : string; Group : string}
//and [<CLIMutable>] Product = { ProductId:int; CompanyId:int; Company: Company } 

module Default = 
    let defaultUser = {UserID = 0;Name="Joe"; Password=""; UserName=""; Role="User"; Group = ""}
    
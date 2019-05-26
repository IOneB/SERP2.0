namespace SERP.Entities

open System.ComponentModel.DataAnnotations
open Giraffe

type [<CLIMutable>] RegisterModel = 
    {
        UserName : string
        Password : string
        ConfirmPassword : string
        Name : string
        Group : option<string>
        TeacherCode : option<string>
    }

    member this.HasErrors() =
        if      this.UserName.Length < 5                then Some "Имя пользователя должно быть не менее 5 символов."
        else if this.Password.Length  < 3               then Some "Слишком короткий пароль."
        else if this.Password.Length  > 20              then Some "Слишком длинный пароль."
        else if this.Password <> this.ConfirmPassword   then Some "Пароли не совпадают."
        else if this.Name.Length < 3                    then Some "Слишком короткое имя."
        else 
            match this.Group with
                | Some value when value.Length > 7 ->        Some "Введите корректный номер группы." 
                | _ ->                                       None

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
    
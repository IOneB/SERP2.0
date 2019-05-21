namespace SERP.Entities

open System.ComponentModel.DataAnnotations
open Giraffe

type [<CLIMutable>] RegisterModel = 
    {
    [<Required(ErrorMessage="Укажите имя пользователя")>]
    [<MinLength(5)>]
    UserName : string
    [<Required(ErrorMessage="Не указан пароль")>]
    [<MinLength(8)>]
    [<DataType(DataType.Password)>]
    Password : string
    [<DataType(DataType.Password)>]
    [<Compare("Password", ErrorMessage="Пароли должны совпадать")>]
    ConfirmPassword : string
    [<MinLength(4)>]
    Name : string
    [<MaxLength(7)>]
    [<MinLength(3)>]
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

type [<CLIMutable>] LoginModel = {
    [<Required(ErrorMessage="Не указано имя пользователя")>]
    UserName : string
    [<Required(ErrorMessage="Не указан пароль")>]
    Password : string
}

type [<CLIMutable>] User = { UserID:int;Name:string; Password:string; UserName: string; Role : string; Group : string}
//and [<CLIMutable>] Product = { ProductId:int; CompanyId:int; Company: Company } 

module Default = 
    let defaultUser = {UserID = 0;Name="Joe"; Password=""; UserName=""; Role="User"; Group = ""}
    
namespace SERP.Entities

module Measure =
    [<Measure>]
    type MHz
    [<Measure>]
    type dB
    [<Measure>]
    type M = /MHz
    [<Measure>]
    type mkV
    [<Measure>]
    type V
    [<Measure>]
    type s = /MHz
    
    let convertDBtoMkV ( temp : float<dB> ) = 10.0 ** (0.05<dB^-1> * temp) * 1.0<mkV/M>
    let listToStr list = string list
    let strToList (str: string) = 
        str.Trim([| '[';']' |]).Split(';')
        |> Seq.toList
        |> List.map int

open System.ComponentModel.DataAnnotations
open Giraffe
open Measure

type ResultType = Security = 0 | Protection = 1| Effective = 2
type Zone = Near = 0 | Intermediate = 1 | Far = 2

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
        else                                                 None

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

type [<CLIMutable>] APIModel =
    {
        Freqs: float<MHz> list
        Tens: float<dB> list
        NoiseTens: float<dB> list
        ResultType: ResultType
        Count: int

        SecureResult: option<float<M> list>
        Ec: option<float<mkV/M> list>
        L1: option<float<M> list>
        L2: option<float<M> list>
        Zone: option<Zone list>

        ProtectionResult: option<float<M> list>
        Uc: option<float<dB> list>
        U1: option<float<mkV> list> 
        U2: option<float<mkV> list> 
        L: option<float<M> list> 
        Kp: option<float<dB/M> list> 
        Def: option<float<dB> list>

        EffectiveResult: option<float<mkV/M> list>
    }

    member this.HasErrors() = None

    interface IModelValidation<APIModel> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.badRequest (text msg))
            | None     -> Ok this

type [<CLIMutable>] User = { UserID:int; Name:string; Password:string; UserName: string; Role : string; Group: string}
and [<CLIMutable>] Result = { ResultID:int; Freqs: string; Tens: string; NoiseTens: string; Count: int; UserID: int; ResultType: ResultType; ResultValues: string; U1: string; U2: string; L: string }

module Default = 
    let defaultUser = { UserID = 0; Name="Joe"; Password=""; UserName=""; Role="User"; Group = ""}
    let defaultResult = { ResultID = 0; Freqs = ""; Tens = ""; NoiseTens = ""; Count = 0; UserID = -1; ResultType = ResultType.Security; ResultValues = ""; U1 = ""; U2 = ""; L = ""}
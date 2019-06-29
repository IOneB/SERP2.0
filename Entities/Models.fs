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
    let listToStr (list: float<_> list) = (List.fold (fun prev next -> prev + string next + ";") "" list ).TrimEnd(';')
    let strToList (measure : float<_>) (str: string)  = 
        str.Split(';')
        |> Seq.toList
        |> List.map float
        |> List.map ((*) measure)
    let listlistToStr (list: float<_> list list) = 
        List.map listToStr list
        |> List.reduce (fun x y -> x + "^" + y)
    
    let strToListList (measure: float<_>) (str: string) = 
        str.Replace("[ ", "").Replace(" ]", "").Split('^')
        |> Array.toList
        |> List.map (fun x -> strToList measure x)
        |> List.foldBack (fun prev next -> prev :: next) []


open Giraffe
open Measure
open System

type ResultType = Security = 0 | Protection = 1| Effective = 2
type Zone = Near = 0 | Intermediate = 1 | Far = 2

type GeneratorParameters = 
    {
        tau: float<s>
        R: float<M>
        Tension: float<dB>
        RemoteTens: float<dB>
        Frequency: float<MHz>
        Quality: float
        BandWith: float<MHz>
    }

type [<CLIMutable>] APIModel =
    {
        Id: option<int>
        Date: option<DateTime>
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
        U1: option<float<dB> list> 
        U2: option<float<dB> list> 
        L: option<float<M> list> 
        Kp: option<float<dB/M> list> 
        Def: option<float<dB> list>

        EffectiveResult: option<float list>
        RemoteTens: option<float<dB> list>
        DampingFactor: option<float list list>
        TensDamping: option<float list list>

        tau: float<s> option
        R: float<M> option
        Tension: float<dB> option
        RemoteTensGen: float<dB> option
        Frequency: float<MHz> option
        Quality: float option
        BandWith: float<MHz> option
    }

    member this.HasErrors() = None

    interface IModelValidation<APIModel> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.badRequest (text msg))
            | None     -> Ok this

type [<CLIMutable>] User = { UserID:int; Name:string; Password:string; UserName: string; Role : string; Group: string}
and [<CLIMutable>] Result = { ResultID:int; Freqs: string; Tens: string; NoiseTens: string; Count: int; UserID: int; ResultType: ResultType; ResultValues: string; U1: string; U2: string; L: string; Date: DateTime; RemoteTens: string; GeneratorParameters: string }

type ResultViewModel =
    {
        Id: int
        User: User
        Date: string
        Time: string
        ResultType: string
        Result: string
        Count: int
    }

type UsersResultModelView =
    {
       Me: string
       Results: ResultViewModel list 
    }

type Recommend = {Url: string; Name: string; Head: string}

module Default = 
    let defaultUser = { UserID = 0; Name="Joe"; Password=""; UserName=""; Role="User"; Group = ""}
    let defaultResult = { ResultID = 0; Freqs = ""; Tens = ""; NoiseTens = ""; Count = 0; UserID = -1; ResultType = ResultType.Security; ResultValues = ""; U1 = ""; U2 = ""; L = ""; Date = DateTime.Now; RemoteTens = ""; GeneratorParameters = "";}
    let defaultGenerator = { tau = 0.0<s>; R = 0.0<M>; Tension = 0.0<dB>; RemoteTens = 0.0<dB>; Frequency = 0.0<MHz>; Quality = 0.0; BandWith = 0.0<MHz>}
    let parseGen (gen: GeneratorParameters) = 
        string gen.tau + ";" + 
        string gen.R + ";" + 
        string gen.Tension + ";" + 
        string gen.RemoteTens + ";" + 
        string gen.Frequency + ";" + 
        string gen.Quality + ";" + 
        string gen.BandWith
        
    let strToGen (str: string) = 
        let pars = str.Split(';')
        {tau =  1.0<s> * float pars.[0] ; R = 1.0<M> * float pars.[1]; Tension = 1.0<dB> * float pars.[2]; RemoteTens = 1.0<dB> * float pars.[3]; Frequency = 1.0<MHz> * float pars.[4]; Quality = 1.0 * float pars.[5]; BandWith = 1.0<MHz> * float pars.[6]}
       
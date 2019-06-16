module Actions

open SERP.Entities.Measure
open SERP.Entities
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open System.IO
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Authentication.Cookies
open System.Globalization

let viewData =
    dict [
        "Who", "Foo Bar" :> obj
        "Foo", 89 :> obj
        "Bar", true :> obj
    ]

let getColumns f source = Some <| List.map (fun item -> f item) source

let first4 tupleList = getColumns (fun (x,_,_,_) -> x) tupleList
let second4 tupleList = getColumns (fun (_,x,_,_) -> x) tupleList
let third4 tupleList = getColumns  (fun (_,_,x,_) -> x) tupleList
let fourth4 tupleList = getColumns  (fun (_,_,_,x) -> x) tupleList

let first5 tupleList = getColumns  (fun (x,_,_,_,_) -> x) tupleList
let second5 tupleList = getColumns  (fun (_,x,_,_,_) -> x) tupleList
let third5 tupleList = getColumns (fun (_,_,x,_,_) -> x) tupleList
let fourth5 tupleList = getColumns  (fun (_,_,_,x,_) -> x) tupleList
let fivth tupleList = getColumns  (fun (_,_,_,_,x) -> x) tupleList

let first3 tupleList = getColumns  (fun (x,_,_) -> x) tupleList
let second3 tupleList = getColumns  (fun (_,x,_) -> x) tupleList
let third3 tupleList = getColumns  (fun (_,_,x) -> x) tupleList

let time() = System.DateTime.Now.ToString()

let currentDirectory = Directory.GetCurrentDirectory()
let webRoot = Path.Combine(currentDirectory, @"wwwroot")

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let mustBeLoggedIn : HttpFunc->HttpContext->HttpFuncResult = 
    let notLoggedIn =
        text "pls login"
    requiresAuthentication notLoggedIn

let mustBeAdmin : HttpFunc->HttpContext->HttpFuncResult = 
    let notAdmin =
        RequestErrors.FORBIDDEN
            "Permission denied. You must be an admin."
    requiresRole "Admin" notAdmin

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

let modelState = ModelStateDictionary()

let russian = CultureInfo.CreateSpecificCulture("ru-ru")
let parsingError (err : string) = 
    RequestErrors.BAD_REQUEST err


let setRole = function 
    |Some "f4df6a5d99" -> "Admin"
    |_ -> "User"

let GetHash (secret: string) =
    use md5 = System.Security.Cryptography.MD5.Create()
    secret
    |> System.Text.Encoding.ASCII.GetBytes
    |> md5.ComputeHash
    |> Seq.map (fun c -> c.ToString("X2"))
    |> Seq.reduce (+)

let calcTens tens noiseTens : float<mkV/M> = 
    let t = convertDBtoMkV tens
    let n = convertDBtoMkV noiseTens 
    (float (t * t + n * n) ** 0.5) * 1.0<mkV/M>

let calcRadius (tensNoise: float<mkV/M>) (tens: float<mkV/M>) l1 l2 = function
    | Zone.Near -> 1.0<M> / ((0.3 * tensNoise / tens) ** (1.0/3.0))
    | Zone.Intermediate -> 
        if l1 > 1.0<M> then 
            l1 / ((0.3 * tensNoise / (tens * (1.0<M> / l1) ** 3.0)) ** (1.0/2.0))
        else  
            1.0<M> / ((0.3 * tensNoise / tens) ** (1.0/2.0))
    | Zone.Far -> 
        if l2 > 1.0<M> then
            if l1 > 1.0<M> then
               l2 / (0.3 * tensNoise / ((tens * (1.0<M> / l1) ** 3.0) * (l1 / l2) ** 2.0))  
            else
               l2 / (0.3 * tensNoise / ((tens * (1.0<M> / l2) ** 2.0)))
        else
            1.0<M> / (0.3 * tensNoise / tens)
    | _ -> failwith "Uknown Zone Value"


let calcSecurity freqs tens noiseTens = 
    let input = List.zip3 freqs tens noiseTens
    let normalizeInput = List.map (fun (f, t, n) -> f, calcTens t n, convertDBtoMkV n) input //Нормализация входных данных для ближней зоны
    let r0 = 1

    //Расчет границ зон
    let withL = List.map (fun (f:float<MHz>, t, n ) -> f, 150.0 / (Math.PI * f), 1800.0 / f, t, n) normalizeInput
    let firstStep = List.mapi (fun i (r, l1, l2, t, n) -> calcRadius n t l1 l2 Zone.Near, l1, l2, Zone.Near, t, n) withL
    let secondStep = 
        List.mapi (fun i (r, l1, l2, z, t, n) ->
            if r > l1 then calcRadius n t l1 l2 Zone.Intermediate, l1, l2, Zone.Intermediate, t, n
            else r, l1, l2, z, t, n) firstStep
    let thirdStep = 
        List.mapi (fun i (r, l1, l2, z, t, n) ->
            if r > l2 then calcRadius n t l1 l2 Zone.Far, l1, l2, Zone.Far, t
            else r, l1, l2, z, t) secondStep
    thirdStep //радиус, границы зон, тип зоны и рассчитанное значение напряженности

let calcProtection freqs tens noiseTens (U1 : float<dB> list) (U2 : float<dB> list) (l : float<M> list) = 
    let input = List.zip3 freqs tens noiseTens 
    let normalize = List.map (fun (f, t, n) -> f, 20.0<dB> * log10 ((10.0 ** (t / 10.0<dB>) - 10.0 ** (n / 10.0<dB>)) ** (1.0/2.0)), n) input
    let withDef = List.mapi (fun i (f, t, n) -> U1.[i], U2.[i], t - n, t, l.[i]) normalize
    let withKp = List.map (fun (u1, u2, def, t, l) -> 20.0<dB> * (log10 (u1 / u2)) / l, def, t) withDef
    let result = List.map (fun (k, d, t) -> ((d + 10.0<dB>)/ k), k, d, t) withKp
    result //радиус, затухание, показатель защищенности и расчитанное значение напряжения 

let calcEffective freqs tens noiseTens = 
    
    [(0.0<M>, 0.0<M>, 0.0<M>)]

let test = 
    calcSecurity [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.5<dB>(*; 4.0<dB>; 5.0<dB>*)] [8.0<dB>(*; 6.0<dB>; 7.0<dB>*)]
    calcProtection [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.0<dB>(*; 4.0<dB>; 5.0<dB>*)] [10.0<dB>(*; 6.0<dB>; 7.0<dB>*)] [32.0<dB>] [25.0<dB>] [20.0<M>]
    calcEffective [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.5<dB>(*; 4.0<dB>; 5.0<dB>*)] [8.0<dB>(*; 6.0<dB>; 7.0<dB>*)]
    
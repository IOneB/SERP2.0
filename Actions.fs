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
let reportsRoot = Path.Combine(webRoot, "Reports")

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

let calcTens tens noiseTens = 
    (float (tens * tens - noiseTens * noiseTens) ** 0.5) * 1.0<dB>
    |> convertDBtoMkV

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

let calcK (freq: float<MHz>) r =
    let l1 = 150.0 / (Math.PI * freq)
    let l2 = 1800.0 / freq
    if r < l1 then float r ** 3.0
    elif r > l1 && r < l2 then 300_000.0 / (float freq * 2.0 * Math.PI) * (float r ** 2.0)
    else 
        6.0 * 
        (
            (300_000.0 / (float freq * 2.0 * Math.PI)) ** 2.0
        ) * 
        (float r)

let effectiveNumerator interval (Fm : float<MHz>) (tau: float<s>)=
    let numSum = List.fold (fun prev (_, (_, t, _, k, _))-> prev + (float t / k) ** 2.0) 0.0 interval
    let radicalExpression = 0.001 / (2.0 * Fm * tau)
    (numSum * radicalExpression) ** 0.5

let effectiveDenominator (kj : float list) quality tau bandwith tens =
    let N = 0.001 / (tau * bandwith)
    let get j = 
        try kj.[j]
        with | _ -> 1.0
    let radicalExpression = 
        let mutable acc = 0.0
        for i in [1.0..N] do
            acc <- acc + ((float tens / (get (int i))) ** 2.0)
        acc
    quality * (radicalExpression ** 0.5)



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
    let normalize = List.map (fun (f, t, n) -> 
                        f, 20.0<dB> * log10 ((10.0 ** (t / 10.0<dB>) - 10.0 ** (n / 10.0<dB>)) ** (1.0/2.0)), n) input
    let withDef = List.mapi (fun i (f, t, n) -> 
                        U1.[i], U2.[i], t - n, t, l.[i]) normalize
    let withKp = List.map (fun (u1, u2, def, t, l) -> 
                        20.0<dB> * (log10 (u1 / u2)) / l, def, t) withDef
    let result = List.map (fun (k, d, t) -> 
                        ((d + 10.0<dB>)/ k), k, d, t) withKp
    result //радиус, затухание, показатель защищенности и расчитанное значение напряжения 

let calcEffective freqs tens noiseTens remoteTens (noiseGen: GeneratorParameters) = 
    let input = List.zip (List.zip3 freqs tens noiseTens) remoteTens
    //частота, расчит напр, напр помех, К,К, удаленное напр
    let normalizeInput = 
        List.map (fun ((f,t,n), rt) -> 
            f, 
            calcTens t n, 
            convertDBtoMkV n, 
            calcK f noiseGen.R, 
            calcK f 1.0<M>,
            calcK noiseGen.Frequency noiseGen.R, 
            calcK noiseGen.Frequency 1.0<M>,
            convertDBtoMkV rt)
            input
    let withK = 
        List.map (fun (f,t,n,kr,k, knr, kn, rt) -> 
            f, t, n,
            t * k / (rt * kr),
            noiseGen.Tension * kn / (noiseGen.RemoteTens * knr)
            ) 
            normalizeInput
    let Ks = List.map (fun (_,_,_,_,k) -> k) withK
    let indexedFreqs = List.mapi (fun i f -> f, i) freqs
    let maxFreq = List.max freqs
    let intervals = 
        Seq.initInfinite (fun index -> 0.001 * float index / noiseGen.tau)
        |> Seq.pairwise
        |> Seq.takeWhile (fun (s,e) -> s < maxFreq)
        |> Seq.map (fun (s, e) -> List.filter (fun (freq, i) -> freq >= s && freq < e) indexedFreqs)
        |> Seq.filter (fun x -> not <| List.isEmpty x)
        |> Seq.toList
    let intervalWithParams = 
        List.map 
            (fun x -> 
                List.map 
                    (fun (f, i) -> 
                        f, 
                        withK.[i]
                    )
                    x 
            ) intervals
    let deltas =
        List.map 
            (fun x -> 
                effectiveNumerator x noiseGen.Frequency noiseGen.tau 
                /
                effectiveDenominator Ks noiseGen.Quality noiseGen.tau noiseGen.BandWith noiseGen.Tension
            )
            intervalWithParams

    List.zip intervalWithParams deltas 
    |> List.map (fun (x, d) -> d, List.map (fun (_, (_, _, _, k, _)) -> k) x)


let test = 
    calcSecurity [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.5<dB>(*; 4.0<dB>; 5.0<dB>*)] [8.0<dB>(*; 6.0<dB>; 7.0<dB>*)]
    calcProtection [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.0<dB>(*; 4.0<dB>; 5.0<dB>*)] [10.0<dB>(*; 6.0<dB>; 7.0<dB>*)] [32.0<dB>] [25.0<dB>] [20.0<M>]
    //calcEffective [30.0<MHz>(*; 2.0<MHz>; 3.0<MHz>*)] [40.5<dB>(*; 4.0<dB>; 5.0<dB>*)] [8.0<dB>(*; 6.0<dB>; 7.0<dB>*)] None
    
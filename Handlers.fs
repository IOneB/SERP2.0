module Handlers

open System
open System.IO
open Actions
open Giraffe
open SERP.Entities
open Repository
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open System.Security.Claims
open Giraffe.Razor
open SERP.Entities
open SERP.Entities.Default
open SERP.Entities.Measure

let textAndLog txt log : HttpHandler =
    handleContext(
        fun ctx ->
            task {
                let logger = ctx.GetLogger("textAndLog")
                logger.LogInformation(log)
                return! ctx.WriteTextAsync txt
            })

let profileHandler : HttpHandler =
    fun next ctx ->
        razorHtmlView "profile" (Some Repository.user) None None next ctx

let getResultHandler _ =
    text "Ok"

let getUserResultHandler id next (ctx : HttpContext) =
    if Path.Combine(reportsRoot, string id + ".docx") |> File.Exists  then 
        ctx.SetContentType ("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        (streamFile true (Path.Combine(reportsRoot, string id + ".docx")) None None) next ctx
    else text "404" next ctx

let securityHandler (model : APIModel) : HttpHandler = 
    fun next ctx ->
        task {
            let result = calcSecurity model.Freqs model.Tens model.NoiseTens
            let response =
                {
                    model with
                        SecureResult = first5 result
                        L1 = second5 result
                        L2 = third5 result
                        Zone = fourth5 result
                        Ec = fivth result
                        Date = Some DateTime.Now
                }
            let security = 
                {
                    defaultResult with
                        Freqs = listToStr model.Freqs
                        Tens = listToStr model.Tens
                        NoiseTens = listToStr model.NoiseTens
                        Count = model.Count
                        ResultValues = listToStr response.SecureResult.Value
                        UserID = user.UserID
                        Date = response.Date.Value
                }    
            return! (match saveResult security with
                        | (-1, _) -> 
                            parsingError "Не удалось сохранить результат, попробуйте позже"
                        | (_, id) -> 
                            generateSecurityReport model result id user.Name
                            json {response with Id = Some id}) next ctx
        }

let protectionHandler (model:APIModel) : HttpHandler =
    fun next ctx ->
        task {
            let result = calcProtection model.Freqs model.Tens model.NoiseTens model.U1.Value model.U2.Value model.L.Value
            let response =
                {
                    model with
                        ProtectionResult = first4 result
                        Kp = second4 result
                        Def = third4 result
                        Uc = fourth4 result
                        Date = Some DateTime.Now
                }
            let protection = 
                {
                    defaultResult with
                        ResultType = ResultType.Protection
                        Freqs = listToStr model.Freqs
                        Tens = listToStr model.Tens
                        NoiseTens = listToStr model.NoiseTens
                        U1 = listToStr response.U1.Value
                        U2 = listToStr response.U2.Value
                        L = listToStr response.L.Value
                        Count = model.Count
                        ResultValues = listToStr response.ProtectionResult.Value
                        UserID = user.UserID
                        Date = response.Date.Value
                }    
            return! (match saveResult protection with
                        | (-1, _) -> 
                            parsingError "Не удалось сохранить результат, попробуйте позже"
                        | (_, id) -> 
                            generateProtectionReport model result id user.Name
                            json {response with Id = Some id})  next ctx
        }

let effectiveHandler (model:APIModel) : HttpHandler =
    fun next ctx ->
        task {
            let generator = { tau = model.tau.Value; RemoteTens = model.RemoteTensGen.Value; Tension = model.Tension.Value; Frequency = model.Frequency.Value; R = model.R.Value; Quality = model.Quality.Value; BandWith = model.BandWith.Value}
            let result = calcEffective model.Freqs model.Tens model.NoiseTens (defaultArg model.RemoteTens []) generator
            let response =
                {
                    model with
                        EffectiveResult = first4 result
                        DampingFactor = second4 result
                        TensDamping = third4 result
                        Date = Some DateTime.Now
                }
            let effective = 
                {
                    defaultResult with
                        ResultType = ResultType.Effective
                        Freqs = listToStr model.Freqs
                        Tens = listToStr model.Tens
                        NoiseTens = listToStr model.NoiseTens
                        RemoteTens = listToStr model.RemoteTens.Value
                        GeneratorParameters = parseGen generator
                        Count = model.Count
                        ResultValues = listToStr response.EffectiveResult.Value
                        UserID = user.UserID
                        Date = response.Date.Value
                }    
            return! (match saveResult effective with
                        | (-1, _) -> parsingError "Не удалось сохранить результат, попробуйте позже"
                        | (_, id) -> 
                            generateEffectiveReport model result id user.Name
                            json {response with Id = Some id}) next ctx
        }


let recommendHandler path =
    let recommend = match path with
        | "active" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vSV7IgnnqpbUAp6mmuqQTKi4dNMspGKmzaL-7V5yDhZhUQdJH9LgNqG2q3X-U4A0A/pub?embedded=true"; Name = path; Head = "Средства активной защиты"}
        | "effective" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vRfJt6GAHHL3FEr12taNK4BB2tXwUG1Le8HeAgIqLrFA52BHBINJctgC6GVYjxpuQ/pub?embedded=true"; Name = path; Head = "Оценка эффективности"}
        | "organization" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vRKoeySkmUoxE47kwCjKB5zvkFgoFLcI_SIorR7ImAaZinksPFrbhkrcycw8cZW1Q/pub?embedded=true"; Name = path; Head = "Организационные меры"}
        | "passive" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vR8ZX716gQv0agpSO9-NbhHF9tvqUHbDwiACaC7Y-DEhB1jdkuWqq0hTf3Eqnk_HQ/pub?embedded=true"; Name = path; Head = "Пассивные меры"}
        | "protection" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vS_7wSKGyMMZvaekkmpz_Y5JXPUZ55Hvl20nzznGPgfjlP27f5I_1wKbQMVBoxFWQ/pub?embedded=true"; Name = path; Head = "Оценка защищенности от наводок"}
        | "protocols" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vT-HhYp2fkaP9TTk-sk3CdceMcHsPX3EM1dDUK3QAVVz8CkFSDshbNejUM-5b5NWQ/pub?embedded=true"; Name = path; Head = "Протоколы"}
        | "secure_hardware" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vSLch1aRRajiok_IJcHuVUFnHDKktfE9eCqcpUFi40FLdAMdCVigg4KcaWdeQjYkw/pub?embedded=true"; Name = path; Head = "Технические средства в защищенном исполнении"}
        | "security" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vQ6fZFM1gsnrBHdTj5l2Q7rykNNAzUwJ4N6hCC8NcWDQb10DabsgfKzEO7BxK85hQ/pub?embedded=true"; Name = path; Head = "Оценка защищенности от ПЭМИ"}
        | "summary" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vTljyVgQ5_wXxdNlAoTARwRxp3EQfJ-dfpRRJCMFFLZlYtJpRx8uZ_1eKvvFahxIA/pub?embedded=true"; Name = path; Head = "Методические рекомендации"}
        | "test" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vSpfkGa-SiIv-VKEr4HumGmaHmFtX5sv9rklfilz504clhJowxW3qcogqovZ8ClcQ/pub?embedded=true"; Name = path; Head = "Тестовые программы"}
        | "theory" -> {Url = "https://docs.google.com/document/d/e/2PACX-1vQrI4IyA3BYAlR0BpKl1HOfR2U8Am-Ci7cHWjGeTbZ4CAKu9AMrnAkKtOUvIYa1Cg/pub?embedded=true"; Name = path; Head = "Теория"}
        | _ -> {Url = "https://docs.google.com/document/d/e/2PACX-1vRKoeySkmUoxE47kwCjKB5zvkFgoFLcI_SIorR7ImAaZinksPFrbhkrcycw8cZW1Q/pub?embedded=true"; Name = "organization" ; Head = "Организационные меры"}
    razorHtmlView "theory" (Some recommend) None None

let downloadRecommendHandler name : HttpHandler =
    fun next ctx ->
        if Path.Combine(theoryRoot, name + ".docx") |> File.Exists  then 
               ctx.SetContentType ("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
               (streamFile true (Path.Combine(theoryRoot, name + ".docx")) None None) next ctx
        else text "404" next ctx
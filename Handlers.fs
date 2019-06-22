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

let userRoute routequery = route routequery >=> mustBeLoggedIn
let adminRoute routequery = userRoute routequery >=> mustBeAdmin

let textAndLog txt log : HttpHandler =
    handleContext(
        fun ctx ->
            task {
                let logger = ctx.GetLogger("textAndLog")
                logger.LogInformation(log)
                return! ctx.WriteTextAsync txt
            })

let loginHandler (model : LoginModel) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task{
            let user = getUserByName model.UserName
            let claimCookie value = 
                let issuer = "SERP"
                let claims =
                    [
                        Claim(ClaimTypes.Name, value.UserName,  ClaimValueTypes.String, issuer)
                        Claim(ClaimTypes.Role, value.Role, ClaimValueTypes.String, issuer)
                    ]
                let identity = ClaimsIdentity(claims, authScheme)
                let user     = ClaimsPrincipal(identity)
                ctx.SignInAsync(authScheme, user).Start |> ignore 
                text "Successfully logged in"
            return! 
                (match user with
                    |Some value when value.Password = GetHash model.Password -> claimCookie value
                    | _ -> parsingError "Неверный логин или пароль") next ctx
        }
    
let registerHandler (model : RegisterModel ) =  
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task{
            if checkUserByName model.UserName then 
                return! parsingError "Пользователь с таким именем уже существует" next ctx
            else 
                let checkSave = function 
                    | -1 -> parsingError "Some shit happens" 
                    | _ -> text "All good"
                let claimCookie value = 
                    let issuer = "SERP"
                    let claims =
                        [
                            Claim(ClaimTypes.Name, value.UserName,  ClaimValueTypes.String, issuer)
                            Claim(ClaimTypes.Role, value.Role, ClaimValueTypes.String, issuer)
                         ]
                    let identity = ClaimsIdentity(claims, authScheme)
                    let user     = ClaimsPrincipal(identity)
                    ctx.SignInAsync(authScheme, user).Start |> ignore 
                    text "Successfully logged in"
                let creationResult = createUser model
                match getUserByName model.UserName with
                    | Some value ->  claimCookie(value) |> ignore
                    | _ -> ignore |> ignore
                return! checkSave creationResult next ctx
        }

let userHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task { return! text ctx.User.Identity.Name next ctx}

let logoutHandler = 
    fun (next : HttpFunc) (ctx: HttpContext) ->
        ctx.SignOutAsync(authScheme) |> ignore
        setStatusCode 200 next ctx

let profileHandler : HttpHandler =
    fun next ctx ->
        let safeUser = function
            |Some value -> value
            |None -> SERP.Entities.Default.defaultUser
        razorHtmlView "profile" (Some (safeUser (getUserByName ctx.User.Identity.Name))) None None next ctx

let getResultHandler _ =
    text "Ok"

let getUserResultHandler id next (ctx : HttpContext)=
    if Path.Combine(reportsRoot, string id + ".docx") |> File.Exists  then 
        ctx.SetContentType ("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        (streamFile true "Reports/1.docx" None None) next ctx
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
                        UserID = (getUserByName ctx.User.Identity.Name).Value.UserID
                }    
            return! json {response with Id = Some 1} next ctx
                (*match saveResult security with
                    | (-1, _) -> parsingError "Не удалось сохранить результат, попробуйте позже"
                    | (_, id) -> json {response with Id = Some id}*) 
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
                        U1 = listToStr response.U1
                        U2 = listToStr response.U2
                        L = listToStr response.L
                        Count = model.Count
                        ResultValues = listToStr response.ProtectionResult.Value
                        UserID = (getUserByName ctx.User.Identity.Name).Value.UserID
                }    
            return! json {response with Id = Some 1} next ctx
                (*match saveResult protection with
                    | (-1, _) -> parsingError "Не удалось сохранить результат, попробуйте позже"
                    | (_, id) -> json {response with Id = Some id}*) 
        }

let effectiveHandler (model:APIModel) : HttpHandler =
    fun next ctx ->
    (*    task {
            let result = calcSecurity model.Freqs model.Tens model.NoiseTens
            let response =
                {
                    model with
                        SecureResult = first4 result
                        L1 = second4 result
                        L2 = third4 result
                }
            let effective = 
                {
                    defaultResult with
                        Freqs = listToStr model.Freqs
                        Tens = listToStr model.Tens
                        NoiseTens = listToStr model.NoiseTens
                        Count = model.Count
                        ResultValues = listToStr response.SecureResult
                }    
            return! 
                (match saveResult effective with
                    | -1 -> parsingError "Не удалось сохранить результат, попробуйте позже"
                    | _ -> json response) next ctx
        } *)
        text "ура" next ctx
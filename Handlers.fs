module Handlers

open Actions
open Giraffe
open SERP.Entities
open Repository
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open System.Security.Claims

let userRoute routequery = route routequery >=> mustBeLoggedIn
let adminRoute routequery = userRoute routequery >=> mustBeAdmin

let razorView page model : HttpHandler =
    razor (page, model)
    |> htmlString

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
            match user with
                |Some value when value.Password = model.Password -> 
                    let issuer = "SERP"
                    let claims =
                        [
                            Claim(ClaimTypes.Name, value.UserName,  ClaimValueTypes.String, issuer)
                            Claim(ClaimTypes.Role, value.Role, ClaimValueTypes.String, issuer)
                        ]
                    let identity = ClaimsIdentity(claims, authScheme)
                    let user     = ClaimsPrincipal(identity)

                    do! ctx.SignInAsync(authScheme, user)
                    return! text "Successfully logged in" next ctx 
                | _ -> return! parsingError "Неверный логин или пароль" next ctx
        }
    

let registerHandler (model : RegisterModel ) =  
    if checkUserByName model.UserName then 
        (parsingError "Пользователь с таким именем уже существует")
    else 
        let checkSave = function 
            | -1 -> parsingError "Some shit happens" 
            | _ -> text "All good"
        (checkSave (createUser model))

let userHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        text ctx.User.Identity.Name next ctx

let logoutHandler = 
    fun (next : HttpFunc) (ctx: HttpContext) ->
        ctx.SignOutAsync(authScheme) |> ignore
        text "Вы вышли" next ctx


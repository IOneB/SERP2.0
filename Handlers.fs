module Handlers

open Actions
open Giraffe
open SERP.Entities
open Repository
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
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

let loginHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let issuer = "SERP"
            let claims =
                [
                    Claim(ClaimTypes.Name, "John",  ClaimValueTypes.String, issuer)
                    Claim(ClaimTypes.Role, "User", ClaimValueTypes.String, issuer)
                ]
            let identity = ClaimsIdentity(claims, authScheme)
            let user     = ClaimsPrincipal(identity)

            do! ctx.SignInAsync(authScheme, user)

            return! text "Successfully logged in" next ctx
        }

let registerHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = ctx.TryBindFormAsync<RegisterModel>()

            return! 
                (match result with
                    | Ok model when (model.HasErrors()) = None -> 
                        if checkUserByName ctx model.UserName then 
                            text "такой юзер уже есть"
                        else 
                            let checkSave = function 
                                | -1 -> text "-1"
                                | _ -> loginHandler
                            checkSave (createUser ctx model)
                    | Error err -> RequestErrors.BAD_REQUEST err
                    | Ok model -> RequestErrors.BAD_REQUEST (model.HasErrors())) next ctx
        }

let userHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        text ctx.User.Identity.Name next ctx

let logoutHandler = 
    fun (next : HttpFunc) (ctx: HttpContext) ->
        ctx.SignOutAsync(authScheme) |> ignore
        text "Вы вышли" next ctx


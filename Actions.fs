module Actions

open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Security.Cryptography
open System.Text
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

let getParam = function
     | Some value -> value
     | None -> ""
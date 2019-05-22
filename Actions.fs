module Actions

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open UI
open System.IO
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Authentication.Cookies
open System.Globalization

let time() = System.DateTime.Now.ToString()

let razor = RazorGenerator.GenerateView

let currentDirectory = Directory.GetCurrentDirectory()
let webRoot = Path.Combine(currentDirectory, @"UI\wwwroot")
let viewsDirectory = Path.Combine(webRoot, @"Views")

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
let parsingError (err : string) = RequestErrors.BAD_REQUEST err

let setRole = function 
    |Some "f4df6a5d99" -> "Admin"
    |_ -> "User"

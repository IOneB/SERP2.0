module Actions

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open DbContext
open UI
open System.IO
open Microsoft.AspNetCore.Mvc.ModelBinding

let time() = System.DateTime.Now.ToString()

let razor = RazorGenerator.GenerateView

let currentDirectory = Directory.GetCurrentDirectory()
let webRoot = Path.Combine(currentDirectory, @"UI\wwwroot")
let viewsDirectory = Path.Combine(webRoot, @"Views")

let mustBeLoggedIn : HttpFunc->HttpContext->HttpFuncResult = 
    let notLoggedIn =
        RequestErrors.UNAUTHORIZED
            "Basic"
            "Some Realm"
            "You must be logged in."
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
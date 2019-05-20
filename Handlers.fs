module Handlers

open Actions
open Giraffe
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging

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
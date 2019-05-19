open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open System
open Microsoft.AspNetCore.Builder
open ElectronNET.API
open SERP.Entities
open System.IO
open UI

let current = Directory.GetCurrentDirectory()
let webRoot = Path.Combine(current, @"UI\wwwroot")

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

let time() = System.DateTime.Now.ToString()
let razor = RazorGenerator.GenerateView
let razorView page model : HttpHandler =
    razor (page, model)
    |> htmlString

let webApp : HttpHandler = choose[
    route "/test"      >=> warbler (fun _-> (razorView "index.cshtml" (time())))
]

let configureServices(services: IServiceCollection) =
    services.AddMvc() |> ignore
    services.AddGiraffe() |> ignore

let configureApp(app: IApplicationBuilder) =
    app.UseMvc(fun routes -> routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}") |> ignore) |> ignore

    Electron.WindowManager.CreateWindowAsync()|> Async.AwaitTask |> ignore
    app.UseGiraffeErrorHandler(errorHandler)
        .UseStaticFiles()
        .UseGiraffe webApp

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    WebHostBuilder()
        .UseKestrel()
        .UseWebRoot(webRoot)
        .UseContentRoot(webRoot)
        .UseElectron(args)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0

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
open Giraffe.Razor
open Microsoft.AspNetCore.Mvc.ModelBinding
open Actions


let configureServices(services: IServiceCollection) =
    services.AddRazorEngine (viewsDirectory) |> ignore
    services.AddMvc() |> ignore
    services.AddGiraffe() |> ignore

let configureApp(app: IApplicationBuilder) =
    app.UseMvc(fun routes -> routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}") |> ignore) |> ignore

    Electron.WindowManager.CreateWindowAsync()|> Async.AwaitTask |> ignore
    app.UseGiraffeErrorHandler(errorHandler)
        .UseStaticFiles()
        .UseGiraffe Routers.webApp

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

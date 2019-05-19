open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open System
open Microsoft.AspNetCore.Builder
open ElectronNET.API
open SERP.Entities

let user = {UserID=1; Name=""}

let webApp : HttpHandler = choose[
    route "/" >=> json user
    ]

let configureServices(services: IServiceCollection) =
    services.AddMvc() |> ignore
    services.AddGiraffe() |> ignore

let configureApp(app: IApplicationBuilder) =
    app.UseStaticFiles() |> ignore

    app.UseMvc(fun routes -> routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}") |> ignore) |> ignore
    Electron.WindowManager.CreateWindowAsync()|> Async.AwaitTask |> ignore
    app.UseStaticFiles()
        .UseGiraffe webApp


[<EntryPoint>]
let main args =
    WebHostBuilder()
        .UseKestrel()
        .UseElectron(args)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0

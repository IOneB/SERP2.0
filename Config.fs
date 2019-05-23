open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.EntityFrameworkCore;
open ElectronNET.API
open Giraffe.Razor
open Actions
open DbContext

let cookieOptions = fun (options:CookieAuthenticationOptions) -> 
    options.LoginPath <- new Microsoft.AspNetCore.Http.PathString "/login"

let configureServices (services: IServiceCollection) =
    services.AddRazorEngine (viewsDirectory) |> ignore
    services.AddMvc() |> ignore
    services.AddAuthentication(authScheme)
        .AddCookie(cookieOptions) |> ignore
    services.AddDbContext<UserContext>() |> ignore
    services.AddGiraffe() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.UseMvc(fun routes -> routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}") |> ignore) |> ignore

    Electron.WindowManager.CreateWindowAsync() |> Async.AwaitTask |> ignore
    app.UseGiraffeErrorHandler(errorHandler)
        .UseStaticFiles()
        .UseAuthentication()
        .UseGiraffe Routers.webApp
    app.ApplicationServices.GetService<UserContext>().Database.Migrate() |> ignore


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

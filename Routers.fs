module Routers

open Giraffe
open Giraffe.Razor
open Handlers
open Actions
open SERP.Entities
open Repository

let adminRouter = subRoute "/admin" (choose [
    adminRoute "/manage"           >=> setStatusCode 200  >=> text "aaa"
    adminRoute "/time"             >=> warbler (fun _-> textAndLog (time()) "aaa")
    adminRoute "/download_result"  >=> text "manage"
    ])

let userRouter = subRoute "/user" (choose [
    userRoute "/home"              >=> warbler (fun (next, ctx) -> (razorHtmlView "list" (Some (allUsers())) None None ))
    userRoute "/logout"            >=> logoutHandler
    userRoute "/time"              >=> warbler (fun _-> textAndLog (time()) "aaa")
    userRoute "/download_result"   >=> text "manage"
    ])

let postRouter = choose[
        route "/login"        >=> tryBindForm<LoginModel> parsingError None (validateModel loginHandler) //TODO: в следующем слое проверить статус и сайнин
        route "/register"     >=> tryBindForm<RegisterModel> parsingError (Some russian) (validateModel registerHandler) >=> redirectTo false "/user/login" 
    ]

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/view"       >=> razorHtmlView "View" None None None
            route "/"           >=> redirectTo false "/user/home"
            route "/login"      >=> warbler (fun (next, ctx)-> (razorHtmlView "login" (None) None None))
            adminRouter 
            userRouter
            route "/tutorial"   >=> text "tutorial"
            route "/recommend"  >=> text "recommend"
        ]

        POST >=> choose [
            postRouter
        ]

        routex ".*" >=> setStatusCode 404 >=> text "Not Found"
    ]
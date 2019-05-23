module Routers

open Giraffe
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
    userRoute "/home"              >=> warbler (fun (next, ctx) -> allUsers() |> razorView "list.cshtml" )
    userRoute "/logout"            >=> logoutHandler
    userRoute "/time"              >=> warbler (fun _-> textAndLog (time()) "aaa")
    userRoute "/download_result"   >=> text "manage"
    ])

let postRouter = choose[
        route "/user/login"        >=> tryBindForm<LoginModel> parsingError (Some russian) (validateModel loginHandler) //TODO: в следующем слое проверить статус и сайнин
        route "/user/register"     >=> tryBindForm<RegisterModel> parsingError (Some russian) (validateModel registerHandler) >=> redirectTo false "/user/login" 
    ]

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/"           >=> redirectTo false "/user/home"
            route "/login"      >=> warbler (fun _-> (None |> razorView "login.cshtml"))
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
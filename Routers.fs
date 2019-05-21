module Routers

open Giraffe
open Handlers
open Actions
open SERP.Entities
open Repository

let adminRouter = subRoute "/admin" (choose [
    adminRoute "/manage"               >=> setStatusCode 200  >=> text "aaa"
    adminRoute "/home"                 >=> htmlFile "/pages/index.html" 
    adminRoute "/time"                 >=> warbler (fun _-> textAndLog (time()) "aaa")
    adminRoute "/download_result"      >=> text "manage"
    ])

let userRouter = subRoute "/user" (choose [
    route "/login"                 >=> warbler (fun _-> (None |> razorView "login.cshtml"))
    userRoute "/home"              >=> warbler (fun (next, ctx) -> (allUsers(next, ctx)) |> razorView "list.cshtml" )
    userRoute "/logout"            >=> logoutHandler
    userRoute "/time"              >=> warbler (fun _-> textAndLog (time()) "aaa")
    userRoute "/download_result"   >=> text "manage"
    ])

let postRouter = choose[
        route "/user/login"                 >=> loginHandler 
        route "/user/register"              >=> registerHandler
    ]

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/"           >=> redirectTo false "/user/home"
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
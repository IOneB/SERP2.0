module Routers

open Giraffe
open Handlers
open Actions

let adminRouter = subRoute "/admin" (choose [
    adminRoute "/manage"           >=> setStatusCode 200  >=> text "aaa"
    adminRoute "/home"                 >=> htmlFile "/pages/index.html" 
    adminRoute "/time"                 >=> warbler (fun _-> textAndLog (time()) "aaa")
    adminRoute "/download_result"      >=> text "manage"
    ])

let userRouter = subRoute "/user" (choose [
    userRoute "/login"             >=> setStatusCode 200  >=> text "aaa"
    userRoute "/home"              >=> htmlFile "/pages/index.html" 
    userRoute "/time"              >=> warbler (fun _-> textAndLog (time()) "aaa")
    userRoute "/download_result"   >=> text "manage"
    ])

let postRouter = subRoute "/" (choose[

    ])

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            warbler (fun _-> (razorView "index.cshtml" (time())))
            adminRouter
            userRouter
            route "/tutorial"   >=> text "tutorial"
            route "/recommend"  >=> text "recommend"
            route "/test"      >=> warbler (fun _-> (razorView "index.cshtml" (time())))
        ]

        POST >=> choose [
            postRouter
        ]

        routex ".*" >=> setStatusCode 404 >=> text "Not Found"
    ]
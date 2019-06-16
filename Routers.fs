module Routers

open Giraffe
open Giraffe.Razor
open Handlers
open Actions
open SERP.Entities
open Repository

let adminRouter = subRoute "/admin" (choose [
    adminRoute "/manage"           >=> warbler (fun _ -> razorHtmlView "list" (Some (allUsers())) (Some viewData) None)
    routef "/result/%i"  getResultHandler
    ])

let userRouter = subRoute "/user" (choose [
    userRoute "/home"              >=> profileHandler
    userRoute "/profile"           >=> redirectTo false "/user/home" 

    userRoute "/security"          >=> razorHtmlView "security" None None None
    userRoute "/protection"        >=> razorHtmlView "protection" None None None
    userRoute "/effective"         >=> razorHtmlView "effective" None None None

    userRoute "/result"            >=> getUserResultHandler
    ])

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/"           >=> redirectTo false "/test"
            route "/test"       >=> razorHtmlView "test" None None None
            route "/login"      >=> warbler (fun (next, ctx)-> (razorHtmlView "login" (None) (Some viewData) None))
            route "/logout"     >=> logoutHandler >=> redirectTo false "/login"

            adminRouter 
            userRouter

            subRoute "/recommend" (choose [
                userRoute "/theory"     >=> razorHtmlView "theory" None None None
                userRoute "/method"     >=> razorHtmlView "theory" None None None
                userRoute "/recommend"     >=> razorHtmlView "theory" None None None
                userRoute "/literature" >=> razorHtmlView "literature" None None None
                ])
        ]

        POST >=> choose [
            route "/login"        >=> tryBindForm<LoginModel> parsingError None (validateModel loginHandler)
            route "/register"     >=> tryBindForm<RegisterModel> parsingError (Some russian) (validateModel registerHandler)
            route "/result"       >=> tryBindForm<RegisterModel>   parsingError None (registerHandler) //TODO:

            route "/security"     >=> tryBindForm<APIModel> parsingError None (validateModel securityHandler)
            route "/protection"   >=> tryBindForm<APIModel> parsingError None (validateModel protectionHandler)
            route "/effective"    >=> tryBindForm<APIModel> parsingError None (validateModel effectiveHandler)
        ]

        routex ".*" >=> setStatusCode 404 >=> text "Not Found"
    ]
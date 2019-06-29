module Routers

open Giraffe
open Giraffe.Razor
open Handlers
open Actions
open SERP.Entities
open Repository

let adminRouter = subRoute "/admin" (choose [
    adminRoute "/manage"           >=> warbler (fun _ -> razorHtmlView "list" (Some (allResults)) None None)
    routef "/result/%i"  getResultHandler
    ])

let userRouter = subRoute "/user" (choose [
    userRoute "/home"              >=> warbler (fun (_, ctx) -> razorHtmlView "profile" (Some (myResults ctx.User.Identity.Name)) None None)
    userRoute "/profile"           >=> redirectTo false "/user/home" 

    userRoute "/security"          >=> razorHtmlView "security" None None None
    userRoute "/protection"        >=> razorHtmlView "protection" None None None
    userRoute "/effective"         >=> razorHtmlView "effective" None None None

    routef "/result/%i" getUserResultHandler
    ])

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/"           >=> redirectTo false "/user/home"
            route "/login"      >=> warbler (fun (next, ctx)-> (razorHtmlView "login" (None) (Some viewData) None))
            route "/logout"     >=> logoutHandler >=> redirectTo false "/login"
            route "/about"      >=> razorHtmlView "about" None None None
            route "/literature" >=> razorHtmlView "literature" None None None

            adminRouter 
            userRouter

            routef "/recommend/download/%s" downloadRecommendHandler
            routef "/recommend/%s"  recommendHandler
        ]

        POST >=> choose [
            route "/login"        >=> tryBindForm<LoginModel> parsingError None (validateModel loginHandler)
            route "/register"     >=> tryBindForm<RegisterModel> parsingError (Some russian) (validateModel registerHandler)
            route "/result"       >=> tryBindForm<RegisterModel>  parsingError None (registerHandler)

            route "/security"     >=> tryBindForm<APIModel> parsingError None (validateModel securityHandler)
            route "/protection"   >=> tryBindForm<APIModel> parsingError None (validateModel protectionHandler)
            route "/effective"    >=> tryBindForm<APIModel> parsingError None (validateModel effectiveHandler)
        ]

        routex ".*" >=> setStatusCode 404 >=> text "Not Found"
    ]
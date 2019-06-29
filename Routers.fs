module Routers

open Giraffe
open Giraffe.Razor
open Handlers
open Actions
open SERP.Entities
open Repository

let adminRouter = subRoute "/admin" (choose [
    route "/manage"           >=> warbler (fun _ -> razorHtmlView "list" (Some (allResults())) None None)
    routef "/result/%i"  getResultHandler
    ])

let userRouter = subRoute "/user" (choose [
    route "/home"              >=> warbler (fun (_, ctx) -> razorHtmlView "profile" (Some (myResults ())) None None)
    route "/profile"           >=> redirectTo false "/user/home" 

    route "/security"          >=> razorHtmlView "security" None None None
    route "/protection"        >=> razorHtmlView "protection" None None None
    route "/effective"         >=> razorHtmlView "effective" None None None

    routef "/result/%i" getUserResultHandler
    ])

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/"           >=> redirectTo false "/user/home"
            route "/about"      >=> razorHtmlView "about" None None None
            route "/literature" >=> razorHtmlView "literature" None None None

            adminRouter 
            userRouter

            routef "/recommend/download/%s" downloadRecommendHandler
            routef "/recommend/%s"  recommendHandler
        ]

        POST >=> choose [
            route "/security"     >=> tryBindForm<APIModel> parsingError None (validateModel securityHandler)
            route "/protection"   >=> tryBindForm<APIModel> parsingError None (validateModel protectionHandler)
            route "/effective"    >=> tryBindForm<APIModel> parsingError None (validateModel effectiveHandler)
        ]

        routex ".*" >=> setStatusCode 404 >=> text "Not Found"
    ]
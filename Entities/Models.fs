namespace SERP.Entities

open System.ComponentModel.DataAnnotations

type [<CLIMutable>] User = { UserID:int;Name:string}
//and [<CLIMutable>] Product = { ProductId:int; CompanyId:int; Company: Company } 
//and [<CLIMutable>] Company = { CompanyId:int; Products:Product list}
    
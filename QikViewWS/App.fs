open System
open Suave
open Suave.Filters
open Suave.Operators
//open Suave.Successful
open Suave.RequestErrors
//open System.Runtime.InteropServices
open System.Threading
//open Suave.Utilsopen System.Security.Principal
open Authenticationopen System.Configurationopen System.Collections 
open System.Collections

let logPath s (ctx : HttpContext) : Async<HttpContext option> =
    Console.WriteLine(DateTime.Now.ToString() + " " + user None + " " +  ctx.request.url.AbsolutePath.ToString() + " " + s)
    async.Return None
    
let app =
    choose [
        GET >=> choose [
            request(fun r -> authenticateWindows r.["user"])
            logPath ""
            // NOTE: paths are case sensitive
            path ("/QikViewWS/departments") >=> warbler(fun _ -> Departments.get ())
            pathScan "/QikViewWS/transactions/grouped/%s,%s,%s" (fun (id,start,finish) -> Transactions.getGrouped id start finish)
            pathScan "/QikViewWS/transactions/detail/%s,%s,%s,%s" (fun (id,start,finish,code) -> Transactions.getDetail id start finish code)
            isSuperUser
            path ("/QikViewWS/summary") >=> warbler(fun _ -> Summary.get ())
            ]
        logPath ("NOT FOUND")
        NOT_FOUND "Found no handlers"
        ]
        
    [<EntryPoint>]
    let main argv =
        Console.WriteLine("Starting")
        let config = defaultConfig
        let config =
            match Int32.TryParse(Environment.GetEnvironmentVariable("HTTP_PLATFORM_PORT")) with
            | (true, port) ->
                Console.WriteLine("Port found " + port.ToString())
                { config with bindings = [ HttpBinding.mkSimple HTTP "127.0.0.1" port ] }
            | _ -> config
            
        startWebServer config app
        0
    

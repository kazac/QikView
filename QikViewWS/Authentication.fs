module Authentication
open System
open Suave
open Suave.RequestErrors
open System.Runtime.InteropServices
open System.Security.Principal
open FSharp.Data.SqlClient
open FSharp.Datatype 

MaxPrincipal (wi : WindowsIdentity, l : int, payroll: bool, impersonating : string option) =
    inherit System.Security.Principal.WindowsPrincipal (wi)
    member this.Payroll = payroll
    member this.Level = l
    member this.Impersonating = impersonating

let user (username : string option)  =
    let username =
        match username with
        | Some s -> s
        | None ->
            let p = Threading.Thread.CurrentPrincipal :?> MaxPrincipal;
            match p.Impersonating with
            | Some name -> name
            | None -> p.Identity.Name
    let parts = username.Split('\\')
    parts.[parts.Length-1]
        
let getSecurity username =
    if username = "brduffy"
    then Some (9, true)
    else
        use cmd =
            new SqlCommandProvider<
                @"SELECT SecurityLevel, UserType
                FROM Employee e
                JOIN Users u ON e.PayId = u.PayId
                WHERE e.UserName = @username", Max.designConnectionString, SingleRow=true, ResultType=ResultType.Tuples>(Max.runtimeConnectionString)
                
        let result = cmd.Execute(username)
        match result with
        | Some (None, _) -> None
        | Some (l, Some("P")) -> Some(l.Value, true)
        | Some (l, _) -> Some(l.Value, false)
        | _ -> None
        
[<DllImport("kernel32.dll", SetLastError=true)>]
extern bool CloseHandle(IntPtr tokenHandle);

let authenticateWindows (impersonate:string option) (ctx : HttpContext) : Async<HttpContext option> =
    match ctx.request.header "X-IIS-WindowsAuthToken" with
    | Choice1Of2 stoken ->
        let token = IntPtr(Int32.Parse(stoken, System.Globalization.NumberStyles.HexNumber))
        let wi = new WindowsIdentity(token)
        CloseHandle(token) |> ignore
        if wi.IsAuthenticated then
            // check if we are impersonating another user, and set the security level accordingly
            let username =
                match wi.Name, impersonate with
                | "brduffy", Some _ -> impersonate
                | _ -> Some wi.Name
            
            match username |> user |> getSecurity with
            | Some (l, t) ->
                Threading.Thread.CurrentPrincipal <- new MaxPrincipal(wi, l, t, impersonate)
                async.Return None
            | None -> FORBIDDEN "User unknown" ctx
        else FORBIDDEN "Authentication failed" ctx
    | _ -> FORBIDDEN "Authentication token absent failed" ctx
    
let isPayroll (ctx : HttpContext) : Async<HttpContext option> =
    match Threading.Thread.CurrentPrincipal with
    | :? MaxPrincipal as p when p.Payroll = true  -> async.Return None
    | _ -> FORBIDDEN "Payroll only" ctx
    
let minLevel level (ctx : HttpContext) : Async<HttpContext option> =
    match Threading.Thread.CurrentPrincipal with
    | :? MaxPrincipal as p when p.Level >= level  -> async.Return None
    | _ -> FORBIDDEN "Insufficient privilege" ctx
    
let launchDebugger (ctx : HttpContext) : Async<HttpContext option> =
    System.Diagnostics.Debugger.Launch() |> ignore
    fail

module Departments
open System
open System.Configuration
open FSharp.Data.SqlClient
open FSharp.Data
open System.Runtime.Serialization
open Suave
open Suave.Successful
open Suave.Filters
open Suave.Operators
open Suave.Utils

let ofChoice = function
    | Choice1Of2 x -> Some x
    | Choice2Of2 _ -> None

let get () =
    let username = Authentication.user None
    use cmd =
        new SqlCommandProvider<
            @";WITH Departments (ParentId, Id, DeptId, Name, Level)
            AS
            (
                SELECT NULL, p.PointId, p.Data, p.Description, 0 as Level
                FROM PointUser pu
                JOIN Employee e on pu.PayId = e.PayId
                JOIN Users u ON e.PayId = u.PayId
                JOIN Point p ON pu.PointId = p.PointId
                WHERE e.UserName = @username
            UNION ALL
                SELECT p.PointParentId, p.PointId, p.Data, p.Description, Level+1
                FROM Departments d
                INNER JOIN dbo.Point p ON d.Id = p.PointParentId
            )
            SELECT * FROM Departments OPTION (maxrecursion 20)", Max.designConnectionString>(Max.runtimeConnectionString)
    cmd.Execute(username)
    |> Seq.toList
    |> Json.Write 

module Json
open System
open Suave
open Suave.Successful
open Suave.Operators
open Newtonsoft.Json

// The FSharpLu wrapper id used as it trims the "Some/None" noise from the output
// the default FSharpLu serializer omits nulls, so we lift some library code up here to set this th include
type Settings =
    static member settings =
        let s = JsonSerializerSettings(
                    NullValueHandling = NullValueHandling.Include,
                    MissingMemberHandling = MissingMemberHandling.Error)
        s.Converters.Add(Microsoft.FSharpLu.Json.CompactUnionJsonConverter())
        s
        
    static member formatting = Formatting.None
type MySerializer = Microsoft.FSharpLu.Json.With<Settings>
let inline serialize< ^T> x = MySerializer.serialize x

let Write v =
    serialize v
    |> OK >=> Writers.setMimeType "application/json; charset=utf-8"

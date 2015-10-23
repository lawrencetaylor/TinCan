open System.ServiceModel
open System
open Server
open ServerExtensibility

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

[<ServiceContract>]
type IMyService = 
    [<OperationContract>]
    abstract member SayHi : name: string -> string

type GreetingService() = 
    interface IMyService with
        member x.SayHi name  = sprintf "Hello %s" name

[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    let startHostAsync = 
                async {
                    let host = new Host(typeof<GreetingService>, ServiceConfig(), Uri("http://localhost:1234"))
                    host.Open()
                }
    Async.StartImmediate(startHostAsync)
 
    Console.ReadLine() |> ignore
    0 // return an integer exit code

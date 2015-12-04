open System.ServiceModel
open System
open Server
open ServerExtensibility
open System.ServiceModel.Channels

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

[<ServiceContract>]
type IMyService = 
    [<OperationContract>]
    abstract member SayHi : name: string -> string

type MyType() = 
    inherit ServiceInstanceAttribute()
    override x.Binding()  =  new BasicHttpBinding() :> Binding
    override x.Route() = "Hello"

[<MyType>]
type GreetingService() = 
    interface IMyService with
        member x.SayHi name  = sprintf "Hello %s" name

[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    let startHostAsync = 
                async {
                    let host = new Host<GreetingService>("http://localhost:1234")
                    host.Open()
                }
    Async.StartImmediate(startHostAsync)
 
    Console.ReadLine() |> ignore
    0 // return an integer exit code

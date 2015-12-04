module ServerExtensibility

open System
open System.ServiceModel
open System.ServiceModel.Channels

type IServiceStaticConfig = 
    abstract member Binding : Binding
    abstract member Route : string

type IServiceRuntimeConfig = 
    abstract member Factory: (unit -> obj) option
    abstract member IsDebug: bool
    

type ServiceConfig(serviceType: Type) = 

    static member RuntimeDefault = { new IServiceRuntimeConfig with
                                        member x.Factory = None
                                        member x.IsDebug = false}

    static member HttpDefault = { new IServiceStaticConfig with
                                            member x.Route = String.Empty
                                            member x.Binding = BasicHttpBinding() :> Binding}

[<AbstractClass>]
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)>]
type ServiceInstanceAttribute() =
    inherit Attribute()

    abstract member Binding : unit -> Binding
    abstract member Route: unit -> string

    
    interface IServiceStaticConfig with
        member x.Binding = x.Binding()
        member x.Route = x.Route()



module ServerExtensibility

open System.ServiceModel

type ServiceConfig() = 

    member x.ServiceConstructor : (unit -> obj) option = None
    member x.IsDebug = true

    member x.GetBinding() = BasicHttpBinding()


﻿module ServiceConstruction

    open System
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher

    type ConstructingBehaviour(serviceFactory: unit -> obj) =
        member x.Add z y = z + y 
        interface IInstanceProvider with
            member x.ReleaseInstance(_,_) = ()
            member x.GetInstance(_,_) = serviceFactory()
            member x.GetInstance(_) = serviceFactory()

        interface IEndpointBehavior with
            member x.AddBindingParameters(_, _) = ()
            member x.ApplyClientBehavior(_, _) = ()
            member x.Validate(_) = ()
            member x.ApplyDispatchBehavior(endpoint, endpointDispatcher) = endpointDispatcher.DispatchRuntime.InstanceProvider <- x
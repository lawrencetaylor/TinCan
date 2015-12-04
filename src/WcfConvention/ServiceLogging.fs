namespace TinCan

/// <summary>
/// Concerns application/service level logging 
/// </summary>
module ServiceLogging = 

    open System
    open System.Collections
    open System.ServiceModel
    open System.ServiceModel.Description
    open System.ServiceModel.Dispatcher
    open System.Threading.Tasks
    open TinCan.Logging

    let (|Force|) (l:Lazy<_>) =
        l.Force()

    type internal OperationInvoker(invoker: IOperationInvoker, logger : IWcfConventionLogger) = 
        interface IOperationInvoker with

            member x.InvokeBegin(instance, inputs, callback, state) = invoker.InvokeBegin(instance, inputs, callback, state)
            member x.IsSynchronous = invoker.IsSynchronous
            member x.AllocateInputs() = invoker.AllocateInputs()
            member x.Invoke(instance, inputs, outputs) = try invoker.Invoke(instance, inputs, &outputs)
                                                         with | ex ->   logger.ApplicationFault "Unhandled exception in service" (Some ex)
                                                                        raise (FaultException(FaultReason(ex.Message)))
            member x.InvokeEnd(instance, outputs, result) = match result with
                                                                | :? Task as t -> match (box t, lazy t.IsFaulted, lazy box t.Exception) with
                                                                                    | (null, _, _) -> ()
                                                                                    | (_,  Force(true), null) -> logger.ApplicationFault "Unhandled exception in service" None
                                                                                    | (_,  Force(true), ex) -> Some(ex.Force() :?> AggregateException).Value.InnerExceptions
                                                                                                                    |> Seq.map(fun e -> logger.ApplicationFault "Unhandled exception in service" (Some e))
                                                                                                                    |> ignore
                                                                                    | _ -> ()

                                                                | _ -> ()
                                                            invoker.InvokeEnd(instance, &outputs, result)

    /// <summary>
    /// Implementation of IErrorHandler responsible for handling faults in the service layer
    /// </summary>
    type LogErrorsBehaviour(logger : IWcfConventionLogger) = 
    
        let logger = logger

        let hasLogErrorsBehaviour (o : OperationDescription) = o.Behaviors |> Seq.exists(fun b -> b :? LogErrorsBehaviour)

        interface IErrorHandler with
            member x.HandleError(ex) = (logger.ApplicationFault "Unhandled exception in service" (Some ex)) |> ignore
                                       false
            member x.ProvideFault(error, version, fault) = let faultEx = FaultException(FaultReason(error.Message))
                                                           fault <- System.ServiceModel.Channels.Message.CreateMessage(version, faultEx.CreateMessageFault(), faultEx.Action)
                

        interface IOperationBehavior with
            member x.AddBindingParameters(_,_) = ()
            member x.ApplyClientBehavior(_,_) = ()
            member x.Validate(_) = ()
            member x.ApplyDispatchBehavior(_, dispatchOperation) = (dispatchOperation.Invoker = (OperationInvoker(dispatchOperation.Invoker, logger) :> IOperationInvoker)) |> ignore
        

        interface IServiceBehavior with
            member x.AddBindingParameters(_,_,_,_) = ()
            member x.Validate(_,_) = ()
            member x.ApplyDispatchBehavior(_, serviceHostBase) = serviceHostBase.Description.Endpoints 
                                                                    |> Seq.map(fun e -> e.Contract.Operations) 
                                                                    |> Seq.collect(fun o -> o)
                                                                    |> Seq.filter(hasLogErrorsBehaviour >> not)
                                                                    |> Seq.map(fun o -> do o.Behaviors.Add(x))
                                                                    |> ignore
                                                                 serviceHostBase.ChannelDispatchers
                                                                    |> Seq.cast<ChannelDispatcher>
                                                                    |> Seq.map(fun cd -> do cd.ErrorHandlers.Add(x))
                                                                    |> ignore
                                                                 ()
     
                                                             



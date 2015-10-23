module Server

open System.ServiceModel
open System.ServiceModel.Description
open System
open ContractHelpers
open WcfConvention.Logging
open ServiceLogging
open ServerExtensibility
open ServiceConstruction

type ServiceEndpoint with
    member x.AddBehaviour (behaviour : 'a) = let matchingBehaviour = x.Behaviors.Find<'a>()
                                             match box matchingBehaviour with
                                                        | null -> 
                                                                x.Behaviors.Add(behaviour)
                                                                behaviour
                                                        | _ -> matchingBehaviour

    member x.AddConstructingBehavior(config: ServiceConfig) = match config.ServiceConstructor with
                                                                | Some serviceConstructor -> x.AddBehaviour(ConstructingBehaviour(serviceConstructor)) |> ignore
                                                                                             x
                                                                | None -> x

type ServiceHost with
    member x.AddBehaviour (behaviour : 'a) = let matchingBehaviour = x.Description.Behaviors.Find<'a>()
                                             match box matchingBehaviour with
                                                        | null -> 
                                                                x.Description.Behaviors.Add(behaviour)
                                                                behaviour
                                                        | _ -> matchingBehaviour


type Host(serviceType: Type, config : ServiceConfig,  [<ParamArray>] baseAddresses : Uri[]) as server =
    inherit ServiceHost(serviceType, baseAddresses) 

    let segmentDelimiter = '/'
    let logger = ConsoleLogger.Instance :> IWcfConventionLogger
    let serviceType = serviceType
    let binding = config.GetBinding()

    let relativeAddressByConvention (serviceContract : Type) = let segments = (ServiceContract(serviceContract) :> IServiceContract).Name.Split('.') 
                                                                                |> Seq.map(fun segment -> segment.TrimStart(segmentDelimiter).TrimEnd(segmentDelimiter))
                                                               String.Join(segmentDelimiter.ToString(), segments)
    


    let addEndpoint (serviceContract : Type) = 
                                                let relativeAddress =  serviceContract |> relativeAddressByConvention
                                                logger.LibraryDiagnostic (sprintf "Service contract %s has relative endpoint %s" (serviceContract.ToString()) relativeAddress )
                                                server.AddServiceEndpoint(serviceContract, binding, relativeAddress)

    let addConstructingBehavior (ep : ServiceEndpoint) = ep.AddConstructingBehavior(config)

    member val Logger : IWcfConventionLogger = logger with get, set

    override x.InitializeRuntime() = 

        LogErrorsBehaviour(x.Logger) |> server.AddBehaviour  |> ignore
        ServiceMetadataBehavior() |> server.AddBehaviour |> ignore
        (ServiceDebugBehavior() |> server.AddBehaviour).IncludeExceptionDetailInFaults <- config.IsDebug

        serviceType |> ContractHelpers.getServiceContracts
                    |> Seq.map(addEndpoint >> addConstructingBehavior)
                    |> Seq.iter ignore

        base.InitializeRuntime()


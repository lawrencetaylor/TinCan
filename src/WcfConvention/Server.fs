module Server

open System.ServiceModel
open System.ServiceModel.Channels
open System.ServiceModel.Description
open System
open ContractHelpers
open TinCan.Logging
open TinCan.ServiceLogging
open ServerExtensibility
open TinCan.ServiceConstruction

type ServiceEndpoint with
    member x.AddBehaviour (behaviour : 'a) = let matchingBehaviour = x.Behaviors.Find<'a>()
                                             match box matchingBehaviour with
                                                        | null -> 
                                                                x.Behaviors.Add(behaviour)
                                                                behaviour
                                                        | _ -> matchingBehaviour

    member x.AddConstructingBehavior(config: IServiceRuntimeConfig) = 
        match config.Factory with
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


type Host(serviceType: Type, staticConfig : IServiceStaticConfig, runtimeConfig: IServiceRuntimeConfig , [<ParamArray>] baseAddresses : string[]) as server =
    inherit ServiceHost(serviceType, baseAddresses |> Seq.map(fun a -> Uri(a)) |> Array.ofSeq) 

    let segmentDelimiter = '/'
    let logger = ConsoleLogger.Instance :> IWcfConventionLogger
    let serviceType = serviceType
    let binding = staticConfig.Binding

    let relativeAddressByConvention (serviceContract : Type) = let segments = (ServiceContract(serviceContract) :> IServiceContract).Name.Split('.') 
                                                                                |> Seq.map(fun segment -> segment.TrimStart(segmentDelimiter).TrimEnd(segmentDelimiter))
                                                               String.Join(segmentDelimiter.ToString(), segments)
    


    let addEndpoint (serviceContractType : Type) =  let routeSegment = match staticConfig.Route with
                                                                        | "" -> ""
                                                                        | x -> x + "/"
                                                    let serviceContract = ServiceContract(serviceContractType) :> IServiceContract
                                                    let relativeAddress : string = routeSegment + serviceContract.Name
                                                    let binding : System.ServiceModel.Channels.Binding = staticConfig.Binding
                                                    server.AddServiceEndpoint(serviceContractType, binding, relativeAddress)

    let addConstructingBehavior (ep : ServiceEndpoint) = ep.AddConstructingBehavior(runtimeConfig)

    member val Logger : IWcfConventionLogger = logger with get, set

    override x.InitializeRuntime() = 

        LogErrorsBehaviour(x.Logger) |> server.AddBehaviour  |> ignore
        (ServiceMetadataBehavior() |> server.AddBehaviour).HttpGetEnabled <- true
        (ServiceDebugBehavior() |> server.AddBehaviour).IncludeExceptionDetailInFaults <- runtimeConfig.IsDebug

        serviceType |> ContractHelpers.getServiceContracts
                    |> Seq.map(addEndpoint >> addConstructingBehavior)
                    |> Seq.iter ignore

        base.InitializeRuntime()

    //Constructors
    new(serviceType, staticConfig, baseAddresses) = new Host(serviceType, staticConfig, ServiceConfig.RuntimeDefault, baseAddresses)
    new(serviceType, baseAddresses) = new Host(serviceType, ServiceConfig.HttpDefault, ServiceConfig.RuntimeDefault, baseAddresses)

type Host<'a>(staticConfig, runtimeConfig, baseAddresses) = 
    inherit Host(typeof<'a>, staticConfig, runtimeConfig, baseAddresses)

    static let getStaticConfigFromAttribute =
        let serviceInstanceAttributes = typeof<'a>.GetCustomAttributes(typeof<ServiceInstanceAttribute>, false)
                                            |> Seq.cast<ServiceInstanceAttribute>
        match serviceInstanceAttributes |> Seq.isEmpty with
            | false -> serviceInstanceAttributes |> Seq.exactlyOne :> IServiceStaticConfig
            | true -> failwith (sprintf "Expected type %A to have the the ServiceInstance Attribute" typeof<'a>)
             
    //Constructors
    new(runtimeConfig, [<ParamArray>] baseAddresses) =  new Host<'a>(getStaticConfigFromAttribute, runtimeConfig, baseAddresses)
    new([<ParamArray>] baseAddresses) = new Host<'a>(getStaticConfigFromAttribute, ServiceConfig.RuntimeDefault ,baseAddresses)





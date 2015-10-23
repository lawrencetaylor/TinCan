module Server

open System.ServiceModel
open System.ServiceModel.Description
open System
open ContractHelpers
open WcfConvention.Logging
open ServiceLogging
open ServerExtensibility
open ServiceConstruction


type Server2(serviceType: Type, config : ServiceConfig,  [<ParamArray>] baseAddresses : Uri[]) as server =
    inherit ServiceHost(serviceType, baseAddresses) 

    let segmentDelimiter = '/'
    let serviceType = serviceType
    let binding = config.GetBinding()

    let segmentsByConvention (serviceContract : Type) = (ServiceContract(serviceContract) :> IServiceContract).Name.Split('.') 
                                                            |> Seq.map(fun segment -> segment.TrimStart(segmentDelimiter).TrimEnd(segmentDelimiter))
    let joinSegments (segments : seq<string>) = String.Join(segmentDelimiter.ToString(), segments)
    
    let addBehaviour (behvaviourFactory : unit -> 'a) = match box (server.Description.Behaviors.Find<'a>()) with
                                                        | null -> server.Description.Behaviors.Add(behvaviourFactory())
                                                        | _ -> ()

    let addEndpoint (serviceContract : Type) (relativeAddress : string) = server.AddServiceEndpoint(serviceContract, binding, relativeAddress)  |> ignore


    member val Logger = NullLogger.Instance with get, set

    override x.InitializeRuntime() = 

        addBehaviour |> (fun unit -> LogErrorsBehaviour(x.Logger)) |> ignore
        addBehaviour |> (fun unit -> ServiceMetadataBehavior()) |> ignore

        match config.ServiceConstructor with
            | Some serviceConstructor -> addBehaviour |> (fun unit -> ConstructingBehaviour(serviceConstructor)) |> ignore
            | _ -> ()

        serviceType |> ContractHelpers.getServiceContracts
                            |> Seq.map(fun t -> let relativeAddress = t |> (segmentsByConvention >> joinSegments)
                                                addEndpoint t relativeAddress )
                            |> ignore

        let serviceDebugBehaviour = addBehaviour |> (fun unit -> ServiceDebugBehavior())
        serviceDebugBehaviour.IncludeExceptionDetailInFaults <- config.IsDebug

                

        
        
        base.InitializeRuntime()


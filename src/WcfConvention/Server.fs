module Server

open System.ServiceModel
open System.ServiceModel.Description
open System
open Test
open ContractHelpers

type Server2(serviceType: Type, [<ParamArray>] baseAddresses : Uri[]) =
    inherit ServiceHost(serviceType, baseAddresses) 

    let segmentDelimiter = '/'

    let serviceType = serviceType

    let segmentsByConvention (serviceContract : Type) = (ServiceContract(serviceContract) :> IServiceContract).Name.Split('.')
                                                            |> Seq.map(fun segment -> segment.TrimStart(segmentDelimiter).TrimEnd(segmentDelimiter))
    let joinSegments (segments : seq<string>) = String.Join(segmentDelimiter.ToString(), segments)

    override x.InitializeRuntime() = 

        let relativeAddresses = serviceType |> ContractHelpers.getServiceContracts
                                            |> Seq.map(segmentsByConvention >> joinSegments)
        
        base.InitializeRuntime()


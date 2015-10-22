module internal ContractHelpers

    open System
    open System.Runtime.Serialization
    open System.ServiceModel

    let isDataContract (``type``: Type) = Attribute.IsDefined(``type``, typeof<DataContractAttribute>)
    let isServiceContract (``type``: Type) = Attribute.IsDefined(``type``, typeof<ServiceContractAttribute>)
    let getServiceContracts (``type``: Type) = ``type``.GetInterfaces() |> Seq.filter(isServiceContract)
    let getServiceContract (``type``: Type) = let attributes = Attribute.GetCustomAttributes(``type``, typeof<ServiceContractAttribute>)
                                              match (attributes |> Seq.length) with
                                                    | 0 -> None
                                                    | 1 -> Some (attributes |> Seq.exactlyOne)
                                                    | _ -> failwithf "More than one ServiceContract Attribute found on %s" (``type``.ToString())
                                                

    type IServiceContract =
        abstract member Name : string with get

    type ServiceContract(serviceContractInterface : Type) = 

        let serviceContractAttribute = getServiceContract serviceContractInterface
        let name = match serviceContractAttribute with
                                        | Some x -> 
                                                    let attribute = (x :?> ServiceContractAttribute)
                                                    attribute.Namespace + "." + attribute.Name
                                        | None -> serviceContractInterface.FullName.ToString()
        
        interface IServiceContract with
            member x.Name with get() = name

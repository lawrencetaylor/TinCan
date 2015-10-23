namespace WcfConvention

module Logging = 

    open System

    type LogLevel = Trace | Error

    type IWcfConventionLogger = 
        abstract member ApplicationFault : string -> Exception option -> unit
        abstract member LibraryDiagnostic : string -> unit

    type NullLogger private() = 

        static let instance = new NullLogger()
        static member Instance = instance
        
        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = ()
            member x.LibraryDiagnostic(message) = ()

    type ConsoleLogger private() = 

        //Determine colour of console output based on Log Level
        let getConsoleColour logLevel = match logLevel with
                                            | Trace -> ConsoleColor.Gray
                                            | Error  -> ConsoleColor.Red

        //Determine the message to output
        let getMessage (message : string) ( ex : Exception option) = message

        //Log to Console
        let log level message (ex : Exception option) = 
            do (Console.ForegroundColor = getConsoleColour level) |> ignore
            do Console.WriteLine (getMessage message ex)
            do (Console.ResetColor) |> ignore

        static let instance = new ConsoleLogger()
        static member Instance = instance

        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = log Error message ex
            member x.LibraryDiagnostic(message) = log Trace message None



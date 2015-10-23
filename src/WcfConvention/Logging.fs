namespace WcfConvention

module Logging = 

    open System

    type LogLevel = Trace | Error

    type IWcfConventionLogger = 
        abstract member Log : LogLevel -> string -> obj[] -> unit
        abstract member LogException : LogLevel -> Exception -> string -> obj[] -> unit
        abstract member ApplicationFault : string -> Exception option -> unit

    type NullLogger private() = 

        static let instance = new NullLogger()
        static member Instance = instance
        
        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = ()
            member x.Log (level)(formatString)(formatParameters) = ()
            member x.LogException (level)(ex)(formatString)(formatParameters) = ()

    type ConsoleLogger() = 

        //Determine colour of console output based on Log Level
        let getConsoleColour logLevel = match logLevel with
                                            | Trace -> ConsoleColor.Gray
                                            | Error  -> ConsoleColor.Red

        //Determine the message to output
        let getMessage formatString formatParameters ex = 
            String.Format(formatString, formatParameters)

        //Log to Console
        let log level formatString formatParameters (ex : Exception option) = 
            do (Console.ForegroundColor = getConsoleColour level) |> ignore
            do Console.WriteLine (getMessage formatString formatParameters ex)
            do (Console.ResetColor) |> ignore

        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = log Error message null ex
            member x.Log (level)(formatString)(formatParameters) = log level formatString formatParameters None
            member x.LogException (level)(ex)(formatString)(formatParameters) = log level formatString formatParameters (Some(ex))



namespace TinCan

/// <summary>
/// Concerns internal library logging
/// </summary>
module Logging = 

    open System

    type LogLevel = Trace | Error

    type IWcfConventionLogger = 
        abstract member ApplicationFault : string -> Exception option -> unit
        abstract member LibraryDiagnostic : string -> unit

    /// <summary>
    /// Null implementation of a logger
    /// </summary>
    type NullLogger private() = 

        static let instance = new NullLogger()
        static member Instance = instance
        
        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = ()
            member x.LibraryDiagnostic(message) = ()

    /// <summary>
    /// Logs to the console output
    /// </summary>
    type ConsoleLogger private() = 

        let getConsoleColour logLevel = match logLevel with
                                            | Trace -> ConsoleColor.Gray
                                            | Error  -> ConsoleColor.Red

        let getMessage (message : string) ( ex : Exception option) = message

        let log level message (ex : Exception option) = 
            do (Console.ForegroundColor = getConsoleColour level) |> ignore
            do Console.WriteLine (getMessage message ex)
            do (Console.ResetColor) |> ignore

        static let instance = new ConsoleLogger()
        static member Instance = instance

        interface IWcfConventionLogger with
            member x.ApplicationFault(message)(ex) = log Error message ex
            member x.LibraryDiagnostic(message) = log Trace message None



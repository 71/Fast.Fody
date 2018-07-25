module Program

open System
open System.IO
open System.Reflection
open System.Xml.Linq

open Fast.Fody
open Fody

open Mono.Cecil

let inline logWithColor prefix color = Action<string>(fun str ->
    Console.ForegroundColor <- color
    Console.Write("{0} ", (prefix : string))
    Console.ResetColor()
    Console.WriteLine str
)

/// Entry point used when debugging the modification process.
[<EntryPoint>]
let main _ =
    let projectPath = Path.Combine(__SOURCE_DIRECTORY__, "..", "Fast.TestAssembly")
    let assemblyPath = Path.Combine(projectPath, "obj", "Debug", "netcoreapp2.0",
                                    "Fast.TestAssembly.dll")
    let configPath = Path.Combine(projectPath, "FodyWeavers.xml")

    let config = XElement.Load(configPath)
    
    let assembly = AssemblyDefinition.ReadAssembly(assemblyPath)
    let weaver   = ModuleWeaver()

    weaver.AssemblyFilePath      <- assemblyPath
    weaver.AddinDirectoryPath    <- Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName
    weaver.Config                <- config.Descendants(XName.Get "ModuleWeaver") |> Seq.head
    weaver.ModuleDefinition      <- assembly.MainModule
    weaver.SolutionDirectoryPath <- Path.GetDirectoryName(assemblyPath)
    weaver.ProjectDirectoryPath  <- projectPath

    weaver.LogMessage <- Action<_, _>(fun msg -> function 
        | MessageImportance.Normal -> weaver.LogWarning.Invoke msg
        | MessageImportance.High   -> weaver.LogError.Invoke msg
        | _                        -> weaver.LogDebug.Invoke msg
    )

    weaver.LogInfo    <- logWithColor "[i]" ConsoleColor.Green
    weaver.LogDebug   <- logWithColor "[<]" ConsoleColor.Blue
    weaver.LogError   <- logWithColor "[!]" ConsoleColor.Red
    weaver.LogWarning <- logWithColor "[-]" ConsoleColor.Yellow

    weaver.Execute()
    weaver.AfterWeaving()

    0

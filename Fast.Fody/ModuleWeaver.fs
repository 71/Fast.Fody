namespace Fast.Fody

open System
open System.IO
open System.Text
open System.Xml.Linq

open Fody

open Microsoft.FSharp.Compiler.Interactive.Shell
open Microsoft.FSharp.Compiler.SourceCodeServices


/// Weaver that will purge all FSharp.Core-related members from the visited module.
type ModuleWeaver() =
    inherit BaseModuleWeaver()


    override __.GetAssembliesForScanning() = Seq.empty
    override __.ShouldCleanReference = true

    override this.Execute() =

        // Helpers
        let inline debugf fmt = Printf.ksprintf this.LogDebug.Invoke fmt
        let inline warnf  fmt = Printf.ksprintf this.LogWarning.Invoke fmt
        let inline errorf fmt = Printf.ksprintf this.LogError.Invoke fmt


        // Initialize FSI
        let outString = StringBuilder()
        let errString = StringBuilder()
        use out = new StringWriter(outString)
        use err = new StringWriter(errString)
        use in' = new StringReader("")

        let args = [| "fsi"; "--noninteractive"; "--nologo" |]

        let fsiConfig  = FsiEvaluationSession.GetDefaultConfiguration()
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, args, in', out, err)

        let printErrors (errors: FSharpErrorInfo[]) =
            if errors.Length = 0 then
                errorf "Error: %s" (err.ToString())
            else
                for i, error in Array.indexed errors do
                    let message = sprintf "Error %d/%d: (%d:%d) %s" (i + 1) errors.Length
                                                                         <| error.StartLineAlternate
                                                                         <| error.StartColumn
                                                                         <| error.Message

                    if error.Severity = FSharpErrorSeverity.Error then
                        errorf "%s" message
                    elif error.Severity = FSharpErrorSeverity.Warning then
                        warnf "%s" message


        // Add loaded directories to FSI search paths
        let assemblyDirectories = AppDomain.CurrentDomain.GetAssemblies()
                                  |> Seq.filter (fun x -> not x.IsDynamic && not <| String.IsNullOrWhiteSpace x.Location)
                                  |> Seq.map (fun x -> Path.GetDirectoryName x.Location)
                                  |> Seq.distinct
                                  |> Seq.append (Seq.singleton this.AddinDirectoryPath)

        for assemblyDirectory in assemblyDirectories do
            let result, errors = fsiSession.EvalInteractionNonThrowing(sprintf "#I @\"%s\"" assemblyDirectory)

            match result with
            | Choice1Of2 ()  -> debugf "Added '%s' to output." assemblyDirectory
            | Choice2Of2 exn ->
                errorf "Cannot import directory '%s': %A." assemblyDirectory exn 
                printErrors errors
        
        // Load Fast.Fody directly
        match fsiSession.EvalInteractionNonThrowing(sprintf "#r \"Weavers.dll\"") with
        | Choice2Of2 exn, errors ->
            errorf "Cannot load Fast.Fody in interactive: %A." exn
            printErrors errors
        | _ -> ()
        
        match fsiSession.EvalExpressionNonThrowing("Fast.Fody.Context.__weaver : obj ref") with
        | Choice2Of2 exn, errors ->
            errorf "Cannot initialize context: %A." exn
            printErrors errors

        | Choice1Of2  None, _ ->
            assert false
        
        | Choice1Of2 (Some value), _ ->
            (value.ReflectionValue :?> obj ref) := box this

            fsiSession.EvalInteraction("Fast.Fody.Context.__initialize() ;;")

            // Execute scripts (lets them add various handlers)
            let baseDirectory = this.ProjectDirectoryPath
            let scriptName = XName.Get("Script")

            let mutable success = true

            for script in this.Config.Elements(scriptName) do
                let scriptPath = script.Value

                try
                    let fullScriptPath = if Path.IsPathRooted(scriptPath) then
                                            scriptPath
                                         else
                                            Path.Combine(baseDirectory, scriptPath)

                    let result, errors = fsiSession.EvalScriptNonThrowing(fullScriptPath)

                    match result with
                    | Choice1Of2 ()  -> debugf "Script '%s' loaded successfully." scriptPath
                    | Choice2Of2 exn ->
                        errorf "Script '%s' threw an exception: %A." scriptPath exn 
                        printErrors errors

                        success <- false

                with
                | e -> errorf "Unable to load script '%s': %A." scriptPath e
                       success <- false


            // Process assembly
            if success then
                fsiSession.EvalInteraction("Fast.Fody.Context.__process() ;;")

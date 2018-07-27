namespace Fast.Fody

open System
open System.IO
open System.Xml.Linq

open Fody

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Scripting
open Microsoft.CodeAnalysis.CSharp.Scripting

open Mono.Cecil
open Mono.Cecil.Cil
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Scripting


/// Weaver that runs C# scripts given in the Fast.CSharp weaver config.
type ModuleWeaver() =
    inherit BaseModuleWeaver()


    override __.GetAssembliesForScanning() = Seq.empty
    override __.ShouldCleanReference = true

    override this.Execute() =

        // Helpers
        let inline debugf fmt = Printf.ksprintf this.LogDebug.Invoke fmt
        let inline warnf  fmt = Printf.ksprintf this.LogWarning.Invoke fmt
        let inline errorf fmt = Printf.ksprintf this.LogError.Invoke fmt


        // Initialize C# scripting env
        // let context = Context(this)
        // let contextType = context.GetType()
        let blacklistedAssemblies = [| "Fast.CSharp.Fody" |]

        let knownAssemblies =
            [|
                AppDomain.CurrentDomain.GetAssemblies()
                |> Array.where (fun x -> not x.IsDynamic && not <| String.IsNullOrWhiteSpace x.Location)
                |> Array.map   (fun x -> x.Location)

                Array.singleton this.AssemblyFilePath

                this.References.Split(';')
            |]
            |> Array.concat
            |> Array.distinct
            |> Array.filter (fun x ->
                                let filename = Path.GetFileNameWithoutExtension(x)

                                Array.contains filename blacklistedAssemblies
                                |> not
                            )
        
        let scriptMetadataResolver =
            ScriptMetadataResolver.Default
                .WithSearchPaths(knownAssemblies
                                 |> Seq.map Path.GetDirectoryName
                                 |> Seq.distinct
                                 |> Seq.toArray)
    
        let metadataResolver = { new MetadataReferenceResolver() with
            override this.Equals(other) =
                LanguagePrimitives.PhysicalEquality (box this) other

            override this.GetHashCode() =
                LanguagePrimitives.PhysicalHash this

            override __.ResolveMissingAssemblies = true
            override __.ResolveMissingAssembly(definition, referenceIdentity) =
                knownAssemblies
                |> Seq.tryFind (fun x -> Path.GetFileNameWithoutExtension(x) = referenceIdentity.Name)
                |> function
                   | Some reference -> PortableExecutableReference.CreateFromFile reference
                   | None -> scriptMetadataResolver.ResolveMissingAssembly(definition, referenceIdentity)

            override __.ResolveReference(reference, baseFilePath, properties) =
                knownAssemblies
                |> Seq.where (fun x -> Path.GetFileName(x).Contains(reference))
                |> Seq.map   (PortableExecutableReference.CreateFromFile)
                |> scriptMetadataResolver.ResolveReference(reference, baseFilePath, properties)
                                .AddRange
        }

        let scriptSourceResolver =
            ScriptSourceResolver.Default
                .WithBaseDirectory(this.ProjectDirectoryPath)
        
        use emptyStream = new MemoryStream(0)

        let sourceResolver = { new SourceReferenceResolver() with
            override __.Equals(other) =
                LanguagePrimitives.PhysicalEquality (box this) other

            override this.GetHashCode() =
                LanguagePrimitives.PhysicalHash this

            override __.NormalizePath(path, baseFilePath) =
                scriptSourceResolver.NormalizePath(path, baseFilePath)

            override __.ResolveReference(path, baseFilePath) =
                scriptSourceResolver.ResolveReference(path, baseFilePath)

            override __.OpenRead(resolvedPath) =
                if Path.GetFileName(resolvedPath) = "Context.csx" then
                    // Do not provide a source to Context.csx, since we have our own shim below
                    emptyStream :> _
                else
                    scriptSourceResolver.OpenRead(resolvedPath)
        }

        let runtime = typeof<obj>.Assembly.Location
                      |> Path.GetDirectoryName
                      |> fun root -> Path.Combine(root, "System.Runtime.dll")

        let baseScriptOpts = ScriptOptions.Default
                                .WithMetadataResolver(metadataResolver)
                                .WithSourceResolver(sourceResolver)
                                .AddReferences(knownAssemblies)
                                .AddReferences(typeof<obj>.Assembly,
                                               typeof<ModuleDefinition>.Assembly,
                                               typeof<Instruction>.Assembly)
                                .AddReferences(runtime)
                                .AddImports("System",
                                            "System.Collections.Generic",
                                            "System.Linq",
                                            "System.Reactive.Linq",
                                            "System.Reactive.Subjects",
                                            "System.Threading.Tasks",
                                            "Mono.Cecil",
                                            "Mono.Cecil.Cil")


        // Compute script state
        // Since we can't send an object defined in this assembly to the script, we instead
        // store the weaver in a tuple, and send the tuple to an "initialization" script that
        // sets up the global state.
        let setupState = """
            public class __ContextType
            {
                private readonly Fody.BaseModuleWeaver weaver;

                private readonly Subject<ModuleDefinition>   moduleSubject;
                private readonly Subject<AssemblyDefinition> assemblySubject;
                private readonly Subject<TypeDefinition>     typeSubject;
                private readonly Subject<MethodDefinition>   methodSubject;
                private readonly Subject<FieldDefinition>    fieldSubject;
                private readonly Subject<PropertyDefinition> propertySubject;
                private readonly Subject<EventDefinition>    eventSubject;

                private readonly Subject<Exception> disposeSubject;

                private __ContextType(Fody.BaseModuleWeaver w)
                {
                    weaver = w;

                    moduleSubject   = new Subject<ModuleDefinition>();
                    assemblySubject = new Subject<AssemblyDefinition>();
                    typeSubject     = new Subject<TypeDefinition>();
                    methodSubject   = new Subject<MethodDefinition>();
                    fieldSubject    = new Subject<FieldDefinition>();
                    propertySubject = new Subject<PropertyDefinition>();
                    eventSubject    = new Subject<EventDefinition>();

                    disposeSubject  = new Subject<Exception>();
                }

                public static __ContextType __create(Fody.BaseModuleWeaver w) => new __ContextType(w);

                public void Warn(string format, params object[] args)  => weaver.LogWarning(string.Format(format, args));
                public void Error(string format, params object[] args) => weaver.LogError(string.Format(format, args));
                public void Debug(string format, params object[] args) => weaver.LogDebug(string.Format(format, args));
                public void Info(string format, params object[] args)  => weaver.LogInfo(string.Format(format, args));

                public IObservable<ModuleDefinition>   Modules    => moduleSubject;
                public IObservable<AssemblyDefinition> Assemblies => assemblySubject;
                public IObservable<TypeDefinition>     Types      => typeSubject;
                public IObservable<MethodDefinition>   Methods    => methodSubject;
                public IObservable<FieldDefinition>    Fields     => fieldSubject;
                public IObservable<PropertyDefinition> Properties => propertySubject;
                public IObservable<EventDefinition>    Events     => eventSubject;

                public IObservable<Exception> Dispose  => disposeSubject;

                public void __process()
                {
                    var moduleDefinition = weaver.ModuleDefinition;

                    try
                    {
                        assemblySubject.OnNext(moduleDefinition.Assembly);
                        moduleSubject.OnNext(moduleDefinition);

                        foreach (var type in moduleDefinition.Types)
                        {
                            typeSubject.OnNext(type);

                            foreach (var p in type.Properties) propertySubject.OnNext(p);
                            foreach (var m in type.Methods)    methodSubject.OnNext(m);
                            foreach (var f in type.Fields)     fieldSubject.OnNext(f);
                            foreach (var e in type.Events)     eventSubject.OnNext(e);
                        }
                    }
                    catch (Exception e)
                    {
                        disposeSubject.OnNext(e);

                        throw;
                    }
                    
                    disposeSubject.OnNext(null);
                }
            }

            var Context = __ContextType.__create(Item1);
        """
        let globals = Tuple.Create<BaseModuleWeaver>(this)
        let state = CSharpScript.RunAsync(setupState, baseScriptOpts, globals)
        state.Wait()
        let state = state.Result


        // Execute scripts (lets them add various handlers)
        let baseDirectory = this.ProjectDirectoryPath
        let scriptName = XName.Get("Script")

        let mutable success = true

        for scriptElement in this.Config.Elements(scriptName) do
            let scriptPath = scriptElement.Value
            let fullScriptPath = if Path.IsPathRooted(scriptPath) then
                                    scriptPath
                                 else
                                    Path.Combine(baseDirectory, scriptPath)

            let scriptOpts    = baseScriptOpts.WithFilePath(fullScriptPath)
            use scriptContent = File.OpenRead(fullScriptPath)

            let script = state.Script.ContinueWith(scriptContent, scriptOpts)

            let diagnostics = script.Compile()
            let mutable error = false

            for diagnostic in diagnostics do
                let location = diagnostic.Location.GetMappedLineSpan()

                match diagnostic.Severity with
                | DiagnosticSeverity.Error ->
                    error <- true
                    errorf "%s (%d:%d): %s" scriptPath
                                         <| location.StartLinePosition.Line
                                         <| location.StartLinePosition.Character
                                         <| diagnostic.GetMessage()
                
                | DiagnosticSeverity.Warning ->
                    if diagnostic.IsWarningAsError then
                        error <- true
                    warnf "%s (%d:%d): %s" scriptPath
                                        <| location.StartLinePosition.Line
                                        <| location.StartLinePosition.Character
                                        <| diagnostic.GetMessage()
                | _ -> ()

            if error then
                success <- false
            else
                try
                    script.RunFromAsync(state).Wait()
                with exn ->
                    errorf "Exception while executing script: %A." exn
                    success <- false


        // Process assembly
        if success then
            try
                state.ContinueWithAsync("Context.__process();").Wait()
            with exn ->
                errorf "Exception while processing assembly: %A." exn

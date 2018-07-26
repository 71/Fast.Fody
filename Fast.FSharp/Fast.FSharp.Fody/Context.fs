namespace Fast.Fody

open System
open System.ComponentModel

open Fody

open Mono.Cecil

module Observe =
    let internal processAssembly = new Event<AssemblyDefinition>()
    let internal processModule   = new Event<ModuleDefinition>()
    let internal processType     = new Event<TypeDefinition>()
    let internal processMethod   = new Event<MethodDefinition>()
    let internal processField    = new Event<FieldDefinition>()
    let internal processEvent    = new Event<EventDefinition>()
    let internal processProperty = new Event<PropertyDefinition>()

    let Assemblies = processAssembly.Publish :> IObservable<_>
    let Modules    = processModule.Publish   :> IObservable<_>
    let Types      = processType.Publish     :> IObservable<_>
    let Methods    = processMethod.Publish   :> IObservable<_>
    let Fields     = processField.Publish    :> IObservable<_>
    let Properties = processProperty.Publish :> IObservable<_>
    let Events     = processEvent.Publish    :> IObservable<_>


[<AutoOpen>]
module Context =

    let mutable internal logDebug   : Action<string> = null
    let mutable internal logWarning : Action<string> = null
    let mutable internal logError   : Action<string> = null
    let mutable internal logInfo    : Action<string> = null

    let mutable __weaver = ref Unchecked.defaultof<obj>

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let __initialize() =
        let weaver = !__weaver :?> BaseModuleWeaver

        logDebug   <- weaver.LogDebug
        logWarning <- weaver.LogWarning
        logError   <- weaver.LogError
        logInfo    <- weaver.LogInfo

    let debugf fmt = Printf.ksprintf logDebug.Invoke fmt
    let warnf fmt  = Printf.ksprintf logWarning.Invoke fmt
    let errorf fmt = Printf.ksprintf logError.Invoke fmt
    let infof fmt  = Printf.ksprintf logInfo.Invoke fmt

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let __process() =
        let weaver = !__weaver :?> BaseModuleWeaver
        let moduleDefinition = weaver.ModuleDefinition

        Observe.processAssembly.Trigger(moduleDefinition.Assembly)
        Observe.processModule.Trigger(moduleDefinition)

        for typ in moduleDefinition.Types do
            Observe.processType.Trigger(typ)

            typ.Properties |> Seq.iter Observe.processProperty.Trigger
            typ.Methods    |> Seq.iter Observe.processMethod.Trigger
            typ.Fields     |> Seq.iter Observe.processField.Trigger
            typ.Events     |> Seq.iter Observe.processEvent.Trigger

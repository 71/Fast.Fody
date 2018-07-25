Fast.Fody
=========

Weave an assembly using F# scripts, for extra productivity.


## Getting started
> The NuGet package has not been published yet. However, as soon as it will be online, the
> following will apply to the installation process of Fast.

#### Add a dependency to your project:
```xml
<ItemGroup>
  <PackageReference Include="Fast.Fody" Version="0.1.0" />
</ItemGroup>
```

#### Install the Fast weaver by creating a `FodyWeavers.xml` file and setting its content to:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Fast>
    <Script>ReplaceAnswer.fsx</Script>
  </Fast>
</Weavers>
```

#### Create the `ReplaceAnswer.fsx` script that modifies your assembly:
```fs
#if INTERACTIVE
// Necessary to get completions:
#r @"..\Path\To\Mono.Cecil.dll"
#r @"..\Path\To\Fast.Fody.dll"
#else
// Necessary for the script to load correctly:
#r "Mono.Cecil.dll"
#r "Fast.Fody.dll"
#endif

open Mono.Cecil.Cil

open Fast.Fody

//      Find all properties...
Observe.Property
//                             ... whose name is 'answer'
|> Observable.filter (fun x -> x.Name = "answer")
|> Observable.add(fun prop ->

    // Create a new method body for the getter.
    let newBody = MethodBody(prop.GetMethod)

    // Set its content to:
    //   ldc.i4 42
    //   ret
    newBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 42))
    newBody.Instructions.Add(Instruction.Create(OpCodes.Ret))

    // Override the previous body with the new one.
    prop.GetMethod.Body <- newBody

    // Log an information message to notify the user of a success.
    infof "Successfully modified %A." prop
)
```

#### That's it.
Yup, just let Fody do the rest.

## API
The following API is provided from the scripts.

```fs
module Context =
  // This module is open by default.
  val debugf : StringFormat<'args, unit> -> 'args
  val warnf  : StringFormat<'args, unit> -> 'args
  val errorf : StringFormat<'args, unit> -> 'args
  val infof  : StringFormat<'args, unit> -> 'args

module Observe =
  val Assembly : IObservable<AssemblyDefinition>
  val Module   : IObservable<ModuleDefinition>
  val Type     : IObservable<TypeDefinition>
  val Method   : IObservable<MethodDefinition>
  val Property : IObservable<PropertyDefinition>
  val Field    : IObservable<FieldDefinition>
  val Event    : IObservable<EventDefinition>
```

Fast.FSharp.Fody
================

F# version of [Fast.Fody](..). Uses [fsi](https://www.nuget.org/packages/FSharp.Compiler.Service)
behind the scenes.

## Getting started
> The NuGet package has not been published yet. However, as soon as it will be online, the
> following will apply to the installation process of Fast.

Here, we'll create a script that replaces the body of all properties named `answer` to `42`.

#### Add a dependency to your project:
```xml
<ItemGroup>
  <PackageReference Include="Fast.FSharp.Fody" Version="0.1.0" />
</ItemGroup>
```

#### Install the Fast weaver by creating a `FodyWeavers.xml` file and setting its content to:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Fast.FSharp>
    <Script>ReplaceAnswer.fsx</Script>
  </Fast.FSharp>
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
Observe.Properties
//                            ... named 'answer'
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
  val Assemblies : IObservable<AssemblyDefinition>
  val Modules    : IObservable<ModuleDefinition>
  val Types      : IObservable<TypeDefinition>
  val Methods    : IObservable<MethodDefinition>
  val Properties : IObservable<PropertyDefinition>
  val Fields     : IObservable<FieldDefinition>
  val Events     : IObservable<EventDefinition>
```

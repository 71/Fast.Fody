Fast.CSharp.Fody
================

C# version of [Fast.Fody](..). Uses [Roslyn](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting)
behind the scenes.

## Getting started
> The NuGet package has not been published yet. However, as soon as it will be online, the
> following will apply to the installation process of Fast.

Here, we'll create a script that replaces the body of all properties named `answer` to `return 42;`.

#### Add a dependency to your project:
```xml
<ItemGroup>
  <PackageReference Include="Fast.CSharp.Fody" Version="0.1.0" />
</ItemGroup>
```

#### Install the Fast weaver by creating a `FodyWeavers.xml` file and setting its content to:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Fast.CSharp>
    <Script>ReplaceAnswer.csx</Script>
  </Fast.CSharp>
</Weavers>
```

#### Create the `ReplaceAnswer.csx` script that modifies your assembly:
```cs
// The following namespaces are already imported by default:
//  - System
//  - System.Collections.Generic
//  - System.Linq
//  - System.Reactive.Linq
//  - System.Threading.Tasks
//  - Mono.Cecil
//  - Mono.Cecil.Cil

// Find all properties...
Context.Properties
//              ... named 'answer'
    .Where(x => x.Name == "answer")
    .Subscribe(prop => {
        // Create a new method body for the getter.
        MethodBody newBody = new MethodBody(prop.GetMethod);

        // Set its content to:
        //   ldc.i4 42
        //   ret
        newBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 42));
        newBody.Instructions.Add(Instruction.Create(OpCodes.Ret));

        // Override the previous body with the new one.
        prop.GetMethod.Body = newBody;

        // Log an information message to notify the user of a success.
        Context.Info("Successfully modified {0}.", prop);
    });
```

#### That's it.
Yup, just let Fody do the rest.


## API
The following API is provided from the scripts.

```cs
class ContextType
{
    public void Warn (string format, params object[] args);
    public void Error(string format, params object[] args);
    public void Debug(string format, params object[] args);
    public void Info (string format, params object[] args);

    public IObservable<ModuleDefinition>   Modules    { get; }
    public IObservable<AssemblyDefinition> Assemblies { get; }
    public IObservable<TypeDefinition>     Types      { get; }
    public IObservable<MethodDefinition>   Methods    { get; }
    public IObservable<FieldDefinition>    Fields     { get; }
    public IObservable<PropertyDefinition> Properties { get; }
    public IObservable<EventDefinition>    Events     { get; }
}
```

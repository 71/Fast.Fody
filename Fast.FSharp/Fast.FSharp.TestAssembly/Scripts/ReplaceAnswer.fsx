#if FAST
// Necessary for the script to load correctly:
#r "Mono.Cecil.dll"
#r "Fast.FSharp.Fody.dll"
#else
// Necessary to get completions:
#r @"..\..\Fast.FSharp.Fody\bin\Debug\netstandard2.0\Mono.Cecil.dll"
#r @"..\..\Fast.FSharp.Fody\bin\Debug\netstandard2.0\Fast.FSharp.Fody.dll"
#endif

open Mono.Cecil.Cil

// Warn to ensure we can see the message
warnf "Starting replacement..."

Observe.Properties
|> Observable.filter (fun x -> x.Name = "answer")
|> Observable.add(fun prop ->
    debugf "Found property to edit in %s, processing..." prop.DeclaringType.FullName

    let newBody = MethodBody(prop.GetMethod)

    newBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 42))
    newBody.Instructions.Add(Instruction.Create(OpCodes.Ret))

    prop.GetMethod.Body <- newBody
)

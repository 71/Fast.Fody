#if INTERACTIVE
// Necessary to get completions:
#r @"..\..\Fast.Fody\bin\Debug\netstandard2.0\Mono.Cecil.dll"
#r @"..\..\Fast.Fody\bin\Debug\netstandard2.0\Fast.Fody.dll"
#else
// Necessary for the script to load correctly:
#r "Mono.Cecil.dll"
#r "Fast.Fody.dll"
#endif

open Mono.Cecil.Cil

open Fast.Fody

debugf "Starting replacement..."

Observe.Property
|> Observable.filter (fun x -> x.Name = "answer")
|> Observable.add(fun prop ->
    debugf "Found property to edit in %s, processing..." prop.DeclaringType.FullName

    let newBody = MethodBody(prop.GetMethod)

    newBody.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Forty-two."))
    newBody.Instructions.Add(Instruction.Create(OpCodes.Ret))

    prop.GetMethod.Body <- newBody
)

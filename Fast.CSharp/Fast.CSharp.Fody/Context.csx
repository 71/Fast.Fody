using Mono.Cecil;
using Mono.Cecil.Cil;

public static class Context
{
    private static Exception NotImplemented
        => new NotImplementedException("The Context.csx file contains no implementation of the code, and should only be used as a shim.");

    public static void Warn (string format, params object[] args) => throw NotImplemented;
    public static void Error(string format, params object[] args) => throw NotImplemented;
    public static void Debug(string format, params object[] args) => throw NotImplemented;
    public static void Info (string format, params object[] args) => throw NotImplemented;

    public static IObservable<ModuleDefinition>   Modules    => throw NotImplemented;
    public static IObservable<AssemblyDefinition> Assemblies => throw NotImplemented;
    public static IObservable<TypeDefinition>     Types      => throw NotImplemented;
    public static IObservable<MethodDefinition>   Methods    => throw NotImplemented;
    public static IObservable<FieldDefinition>    Fields     => throw NotImplemented;
    public static IObservable<PropertyDefinition> Properties => throw NotImplemented;
    public static IObservable<EventDefinition>    Events     => throw NotImplemented;
}

Fast.[Fody](https://github.com/Fody/Fody)
=========================================

Weave an assembly using .NET scripts, for extra productivity.

## Documentation

The add-in is available for both C# and F# scripts.
- For C# scripts, documentation is available in [Fast.CSharp].
- For F# scripts, documentation is available in [Fast.FSharp].

## Project structure
- [`.vscode`](./.vscode): [Visual Studio Code](https://github.com/Microsoft/vscode) config files
  that improve the debugging experience.
- [`Common`](./Common): F# sources and MSBuild properties used by both [Fast.CSharp] and [Fast.FSharp].
- [`Fast.CSharp`](./Fast.CSharp): Sources for the Fody addin that invokes C# scripts, as well as tests.
- [`Fast.FSharp`](./Fast.FSharp): Sources for the Fody addin that invokes F# scripts, as well as tests.
- [`Tools`](./Tools): Various tools written in PowerShell for a better building / testing experience.

[Fast.CSharp]: ./Fast.CSharp
[Fast.FSharp]: ./Fast.FSharp

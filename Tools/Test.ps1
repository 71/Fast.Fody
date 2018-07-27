<#
.SYNOPSIS
Performs all tests in this solution with the given configuration.
#>
param([string] $Configuration = 'Debug')

. $PSScriptRoot\Common.ps1

'CSharp', 'FSharp' | % {
  dotnet test "$SolutionDirectory\Fast.$_\Fast.$_.Tests\Fast.$_.Tests.fsproj" -c $Configuration
}

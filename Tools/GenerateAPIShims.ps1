<#
.SYNOPSIS
Generates a C# file that redefines all the exported types in the given
packages.

The packages will be fetched in ~/.nuget/packages, and C# code will be emitted
to the standard output. The C# code does not have any implementation, but defines types
in a way that they can be used in a .csx script.
#>
param(
  [string] $NugetPackagesDirectory = "$env:USERPROFILE\.nuget\packages",
  [string[]] $Packages = @('System.Reactive', 'Mono.Cecil')
)

. $PSScriptRoot\Common.ps1

function Get-TypeName {
  param([type] $Type)

  $Name = $Type.Name

  if (-not $Name.Contains('`')) {
    return $Name
  }

  $Name = $Name.Substring(0, $Name.IndexOf('`'))
  $Name += '<'

  $Type.GetGenericArguments() | select -SkipLast 1 | % {
    $Name += "$(Get-TypeName $_), "
  }
  $Type.GetGenericArguments() | select -Last 1 | % {
    $Name += Get-TypeName $_
  }

  return $Name + '>'
}

function Write-Output {
  Write-Host $Args -NoNewline
}


$Packages | % {
  $PackageDirBase = "$NugetPackagesDirectory\$($_.ToLower())"
  $PackageDir = Get-ChildItem $PackageDirBase -Directory `
              | sort -Property DirectoryName -Descending `
              | select -First 1

  $PackageDll = Get-ChildItem $PackageDir.FullName -Recurse -File -Filter "$_.dll" `
              | select -Last 1

  Write-Output "[i] Loading $PackageDll..."  -ForegroundColor Blue

  $Assembly = [Reflection.Assembly]::LoadFrom($PackageDll.FullName)

  Write-Output "[+] Loaded $Assembly."  -ForegroundColor Green

  $Assembly.ExportedTypes | ? { $_.IsPublic } | % {
    if ($_.IsStatic) { Write-Output 'public static ' }
    else { Write-Output 'public ' }

    Write-Output "class $(Get-TypeName $_)`n{`n"

    $_.DeclaredMethods | ? { $_.IsPublic -and $_.Name -notlike '*_*' } | % {
      Write-Output '    '

      if ($_.IsStatic) { Write-Output 'public static ' }
      else { Write-Output 'public ' }

      Write-Output "$(Get-TypeName $_.ReturnType) $($_.Name)"
      $Generics = $_.GetGenericArguments()

      if ($Generics) {
        Write-Output '<'

        $Generics | select -SkipLast 1 | % {
          Write-Output "$($_.Name), "
        }
        $Generics | select -Last 1 | % {
          Write-Output $_.Name
        }

        Write-Output '>'
      }

      Write-Output '('

      if ($_.CustomAttributes | ? { $_.AttributeType.Name -eq 'ExtensionAttribute' }) {
        Write-Output 'this '
      }

      $_.GetParameters() | select -SkipLast 1 | % {
        Write-Output "$(Get-TypeName $_.ParameterType) $($_.Name), "
      }
      $_.GetParameters() | select -Last 1 | % {
        Write-Output "$(Get-TypeName $_.ParameterType) $($_.Name)"
      }

      Write-Output ") => throw new NotImplementedException();`n"
    }

    $_.DeclaredProperties | % {
      Write-Output '    '

      if ($_.IsStatic) { Write-Output 'public static ' }
      else { Write-Output 'public ' }

      Write-Output "$(Get-TypeName $_.PropertyType) $($_.Name) `n    {`n"

      if ($_.GetMethod) {
        Write-Output "         get => throw new NotImplementedException();`n"
      }
      if ($_.SetMethod) {
        Write-Output "         set => throw new NotImplementedException();`n"
      }

      Write-Output "    }`n"
    }

    Write-Output "}`n`n"
  }

  Write-Output
}

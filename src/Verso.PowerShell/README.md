# Verso.PowerShell

PowerShell language kernel extension for [Verso](https://github.com/DataficationSDK/Verso) notebooks.

## Overview

Hosts a persistent PowerShell runspace powered by Microsoft.PowerShell.SDK. State persists across cells, pipelines render through `Out-String` for proper formatting, and variables are automatically shared with other kernels.

### Features

- **PowerShell execution** with a persistent runspace, pipeline output, and full error/warning/information stream handling
- **IntelliSense** via `CommandCompletion` (the PowerShell SDK's native tab-completion engine)
- **Parse diagnostics** via the PowerShell AST parser
- **Hover information** with AST-aware context (commands, variables, members)
- **Bidirectional variable sharing** between PowerShell and other kernels (C#, F#, SQL, Python)
- **Format-aware output** that detects PowerShell format objects and pipes them through `Out-String`
- **`Display` function** for explicit output rendering from PowerShell cells
- **`$VersoVariables`** direct access to the shared variable store from PowerShell
- **Best-effort module-management discovery** for `PowerShellGet` / `PSResourceGet` when a normal `pwsh` installation is available on the machine

## Installation

```shell
dotnet add package Verso.PowerShell
```

This package depends on [Verso.Abstractions](https://www.nuget.org/packages/Verso.Abstractions) and `Microsoft.PowerShell.SDK`.

## PowerShell Gallery Modules

The PowerShell SDK runtime includes the core PowerShell modules, but it does not always include module-management modules such as `PowerShellGet`, `PackageManagement`, or `Microsoft.PowerShell.PSResourceGet`.

On startup, Verso tries to discover an external `pwsh` executable and adds its `$PSHOME/Modules` directory to `PSModulePath` when that directory contains module-management modules. This lets cells use commands such as `Find-Module`, `Install-Module`, `Find-PSResource`, and `Install-PSResource` on machines with a normal PowerShell installation.

If those commands are still unavailable, install PowerShell normally or add the directory containing `PowerShellGet` / `PSResourceGet` to `PSModulePath` before using Gallery install commands.

## Quick Start

```powershell
Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 Name, CPU, WorkingSet
```

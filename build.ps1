param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$VietnamWarDirectory = 'C:\Program Files (x86)\Steam\steamapps\common\VietnamWar'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'IL2CPP.Navigator.csproj'

dotnet build $project -c $Configuration "-p:VietnamWarDirectory=$VietnamWarDirectory"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$output = Join-Path $PSScriptRoot "bin\$Configuration\net6.0\IL2CPP.Navigator.dll"
Write-Host "Plugin output: $output"
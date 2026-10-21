param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Hex of Steel',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $dotnet = Join-Path $root '.tools/dotnet/dotnet.exe'
    if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
    $managed = Join-Path $GamePath 'Hex of Steel_Data/Managed'
    if (-not (Test-Path -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll'))) { throw 'Game assemblies not found. Pass -GamePath pointing to your installed Hex of Steel directory.' }
    & $dotnet restore HexOfSteelStory.sln --locked-mode "-p:GameManagedPath=$managed"
    if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
    & $dotnet build HexOfSteelStory.sln -c $Configuration --no-restore "-p:GameManagedPath=$managed"
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if (-not $SkipTests) {
        & $dotnet test HexOfSteelStory.sln -c $Configuration --no-build --no-restore "-p:GameManagedPath=$managed"
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
    & (Join-Path $PSScriptRoot 'package.ps1') -Configuration $Configuration
} finally { Pop-Location }

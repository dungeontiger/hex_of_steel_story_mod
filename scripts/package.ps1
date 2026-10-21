param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$compiled = Join-Path $root "src/Story.Mod/bin/$Configuration/net48"
if (-not (Test-Path -LiteralPath (Join-Path $compiled 'KingTiger.StoryMod.dll'))) { throw 'Build the mod first.' }
# A fresh staging directory avoids accidentally shipping stale binaries or local configuration.
$staging = Join-Path $root ('artifacts/staging-' + [Guid]::NewGuid().ToString('N'))
$mod = Join-Path $staging 'King Tiger Story'
New-Item -ItemType Directory -Force (Join-Path $mod 'Libraries'),(Join-Path $mod 'Graphics') | Out-Null
foreach ($dll in Get-ChildItem -LiteralPath $compiled -Filter '*.dll') {
    if ($dll.Name -eq 'Newtonsoft.Json.dll') { continue } # Supplied by the game, version 13.
    Copy-Item -LiteralPath $dll.FullName -Destination (Join-Path $mod 'Libraries')
}
if ($Configuration -eq 'Debug') { Get-ChildItem -LiteralPath $compiled -Filter '*.pdb' | Copy-Item -Destination (Join-Path $mod 'Libraries') }
Copy-Item -LiteralPath (Join-Path $root 'branding_images/logo_idea.png') -Destination (Join-Path $mod 'Graphics/logo.png')
foreach ($name in @('LICENSE','README.md','THIRD_PARTY_NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $mod }
Copy-Item -LiteralPath (Join-Path $root 'package/Manifest.json'),(Join-Path $root 'package/config.example.json') -Destination $mod
Copy-Item -LiteralPath (Join-Path $root 'docs/INSTALL.md') -Destination $mod
Copy-Item -LiteralPath (Join-Path $root 'third-party-licenses') -Destination $mod -Recurse
$archive = Join-Path $root "artifacts/KingTigerStory-0.1.0-$Configuration.zip"
Compress-Archive -LiteralPath $mod -DestinationPath $archive -Force
(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash + '  ' + (Split-Path $archive -Leaf) | Set-Content -LiteralPath ($archive + '.sha256')
& (Join-Path $PSScriptRoot 'verify-package.ps1') -Archive $archive
Write-Output "Package: $archive"

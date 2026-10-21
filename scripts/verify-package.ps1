param([Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Archive))
try {
    $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($required in @('King Tiger Story/Manifest.json','King Tiger Story/Libraries/KingTiger.StoryMod.dll','King Tiger Story/Libraries/Story.Core.dll','King Tiger Story/Libraries/0Harmony.dll','King Tiger Story/Graphics/logo.png','King Tiger Story/LICENSE','King Tiger Story/THIRD_PARTY_NOTICES.md','King Tiger Story/config.example.json')) {
        if ($names -notcontains $required) { throw "Package missing: $required" }
    }
    foreach ($name in $names) {
        if ($name -match '(Assembly-CSharp|UnityEngine|Newtonsoft\.Json)\.dll$|config\.local\.json$|\.sav$|\.journal\.jsonl$|\.story\.json$|\.cs$') { throw "Forbidden package content: $name" }
    }
    $manifest = $zip.GetEntry('King Tiger Story/Manifest.json')
    $reader = [IO.StreamReader]::new($manifest.Open())
    try { $data = $reader.ReadToEnd() | ConvertFrom-Json; if ($data.modName -ne 'King Tiger Story') { throw 'Incorrect manifest name' } } finally { $reader.Dispose() }
    Write-Output "Package verified: $($names.Count) entries; game binaries, source, saves, and secrets excluded."
} finally { $zip.Dispose() }

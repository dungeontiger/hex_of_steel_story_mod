# Third-party notices

The project's viewing-only license applies to its original material. It does not restrict rights granted by the following third-party licenses.

## Runtime libraries

- Harmony 2.3.3: MIT. Copyright Andreas Pardeike; bundled dependency notices are included in its license file.
- PDFsharp 6.2.0: MIT. Copyright empira Software GmbH.
- Newtonsoft.Json 13.0.3: MIT. Copyright James Newton-King. Referenced for builds; the package uses the game's existing Newtonsoft.Json runtime rather than shipping a duplicate.
- Microsoft .NET support libraries: MIT. Copyright .NET Foundation and Contributors / Microsoft Corporation. Exact resolved versions are in `src/Story.Mod/packages.lock.json`; original notices are in `third-party-licenses/`.

Full license texts and available upstream notices are copied into `third-party-licenses/` and included in each mod package. PDFsharp's license was retrieved from its upstream repository: https://github.com/empira/PDFsharp/blob/master/LICENSE.

## Development-only tools

The .NET SDK and reference assemblies, xUnit, Microsoft.NET.Test.Sdk, and ILSpy are development tools, not redistributed as part of the mod. NuGet dependency versions are locked. The SDK and locally extracted API inspection files are ignored by Git.

## Game and fonts

Hex of Steel, Unity, and their assemblies belong to their respective owners. No game assemblies, decompiled game source, saves, player data, or credentials are published or included in the mod package. A licensed local installation supplies build and integration-test references.

The PDF renderer reads Times New Roman from the Windows fonts directory and embeds the subset needed for a generated report. Font files are not included in this repository or mod package. This initial release targets Windows; other platforms need an appropriate font resolver and confirmation UI.

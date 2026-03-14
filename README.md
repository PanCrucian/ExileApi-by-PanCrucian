# ExileApi by PanCrucian

Public-source fork of the Poe HUD / ExileApi engine assembled to give full control over the codebase without relying on decompilation of compiled releases.

## Public source baseline

This repository is built from the public source tree published by:

* Engine: `Qvin0000/ExileApi`
* Matching plugin pack: `Qvin0000/ExileApiPlugins`
* Compiled distribution reference: `exApiTools/ExileApi-Compiled`

`exApiTools` publicly exposes a compiled distribution, while the closest public engine source line is the `Qvin0000` fork family. This fork packages the engine source and matching plugin source together in one repository.

## PanCrucian changes

* Rebranded loader and assembly metadata to `ExileApi by PanCrucian`
* Kept technical assembly names like `ExileCore` for plugin compatibility
* Vendored matching plugin sources into `Plugins/Source`
* Preserved the DX11-based public source line as the editable baseline

## Requirements

* .NET Framework 4.8 runtime
* Visual Studio 2019
* .NET Framework 4.8 Developer Pack

## Build

1. Create a working folder such as `HUD`.
2. Place a compatible compiled runtime in `HUD\PoeHelper`.
3. Clone this repository into `HUD\ExileApi-by-PanCrucian` or another sibling folder.
4. Open `ExileApi.sln`.
5. Build the solution in Visual Studio 2019.

The build copies output into the runtime folder expected by the original public source layout.

## Notes

* This is the closest publicly available editable baseline found for the ExileApi engine family.
* If you want this fork to track a newer compiled branch, expect offset updates and API drift work.

## Troubleshooting

* If Windows blocks extracted release files, open the archive properties and use `Unlock` before unpacking.
* If rendering offsets look wrong, set Windows display scaling to `100%`.

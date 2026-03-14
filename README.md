# ExileApi by PanCrucian

Public-source fork of the Poe HUD / ExileApi engine assembled to give full control over the codebase without relying on decompilation of compiled releases.

## Public source baseline

This repository started from the public source tree published by:

* Engine: `Qvin0000/ExileApi`
* Matching plugin pack: `Qvin0000/ExileApiPlugins`
* Compiled distribution reference: `exApiTools/ExileApi-Compiled`

`exApiTools` publicly exposes a compiled distribution, while the closest public engine source line is the `Qvin0000` fork family. This fork packages the editable engine source and matching plugin source together in one repository.

## PanCrucian changes

* Main engine migrated to SDK-style `net10.0-windows` projects
* Root solution simplified to `ExileApi.sln` with only the engine projects
* Legacy CodeDom/source-hot-compile path disabled in the engine
* Rebranded loader and assembly metadata to `ExileApi by PanCrucian`
* Kept technical assembly names like `ExileCore` for plugin compatibility

## Requirements

* Windows 10/11 x64
* .NET SDK 10.0.x

## Build

```powershell
dotnet build ExileApi.sln
```

Build output is written to:

```text
artifacts\build\<Configuration>\
```

Default example:

```text
artifacts\build\Debug\
```

## Publish and release

Create a runnable release package with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Release.ps1
```

That produces three distinct layers:

* `ExileApi\` - source workspace only
* `artifacts\publish\<Configuration>\<RID>\` - raw `dotnet publish` output
* `artifacts\release\ExileApi-by-PanCrucian-<Configuration>-<RID>\` - clean runnable package
* `artifacts\release\ExileApi-by-PanCrucian-<Configuration>-<RID>.zip` - distributable archive

Main runtime artifacts inside the release package:

* `Loader.exe`
* `Loader.dll`
* `ExileCore.dll`
* `GameOffsets.dll`

Runtime folders created in the release package:

* `config`
* `Logs`
* `Sounds`
* `fonts`
* `Plugins\Compiled`
* `Plugins\Source`

## Workspace layout

Use the folders this way to avoid mixing roles:

* `ExileApi\` is the only source-of-truth workspace.
* `ExileApi-Compiled-328.5\` is just an old compiled snapshot/reference folder.
* New runnable packages should be taken from `artifacts\release\...`, not from the source root and not from old compiled snapshots.
* If you still have the old `artifacts\PanCrucian.Net10\` folder, it is legacy and can be removed with `.\scripts\Clear-LegacyArtifacts.ps1`.

## Notes

* This repository is now maintained as a parallel PanCrucian branch rather than a compatibility mirror of the original legacy build chain.
* Source plugin hot-compilation is intentionally disabled on the `net10` branch. Build plugins explicitly and place the binaries into `Plugins\Compiled` if needed.
* The renderer still uses the public SharpDX-based DX11 line for now, but the old `SharpDX.Desktop` wrapper dependency has been removed.
* A future step can still migrate the rendering stack itself to a newer API such as `Vortice`, but that is no longer required just to build and run this branch on .NET 10.

## Troubleshooting

* If Windows blocks extracted release files, open the archive properties and use `Unlock` before unpacking.
* If rendering offsets look wrong, set Windows display scaling to `100%`.

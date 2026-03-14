# ExileApi by PanCrucian Runtime

This folder is a runnable release package, not the source workspace.

## What this folder is for

* Run the HUD from `Loader.exe`
* Drop compiled plugins into `Plugins\Compiled\<PluginName>\`
* Keep local runtime state in `config\`, `Logs\`, `fonts\`, `Sounds\`

## What this folder is not for

* Do not develop engine source code here
* Do not treat this as the canonical repository
* Do not expect source-plugin hot-compilation on the `.NET 10` PanCrucian branch

## Source of truth

Source repository:

* `https://github.com/PanCrucian/ExileApi-by-PanCrucian`

Recommended workflow:

1. Edit source in the `ExileApi` repository.
2. Build or publish from source.
3. Use the generated package from `artifacts\release\...` as the runnable release.

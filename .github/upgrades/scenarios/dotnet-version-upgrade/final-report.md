# .NET 10 Upgrade — Report

**Scenario:** Finish upgrading the FiberDEM2D solution to .NET 10 (net10.0), including fixing a runtime error in PlotFDEM introduced by the earlier (partial) migration.
**Outcome:** ✅ Fully completed
**Projects affected:** 4 (`Animation`, `PlotFDEM`, `FDEMWindows`, `RandomRVEGenerator`)
**Branch:** `upgrade-to-NET10` merged into `main` and deleted (local + remote)

---

## Summary

The FiberDEM2D solution had already been mostly migrated to .NET 10 in a prior session, but `PlotFDEM` compiled while throwing a `System.NotSupportedException` (BinaryFormatter disabled) at runtime when opening a plot. Root cause was the `Animation` project — a WinForms library still on the legacy csproj format targeting .NET Framework 4.8 — whose `.resx` files embed `Bitmap` image resources that require the modern "preserialized resources" format to load without `BinaryFormatter` on .NET 9+/10. The `Animation` project was converted to SDK-style and retargeted to `net10.0-windows`, and the preserialized-resources fix was applied to every WinForms project in the solution with the same resx pattern. The fix was verified by the user re-running PlotFDEM successfully, then merged to `main`.

---

## What Changed

### Packages

| Project | Package | Change | From → To |
|---------|---------|--------|-----------|
| Animation | System.Drawing.Common | Removed | 4.7.2 → *(built into net10.0-windows via `UseWindowsForms`)* |
| Animation | System.Resources.Extensions | Added | — → 9.0.0 |
| PlotFDEM | System.Resources.Extensions | Added | — → 9.0.0 |
| FDEMWindows | System.Resources.Extensions | Added | — → 9.0.0 |
| RandomRVEGenerator | System.Resources.Extensions | Added | — → 9.0.0 |

### Code Modifications

- **Project file changes**
  - `animation/animation/Animation.csproj` — converted from legacy `packages.config`-style project to SDK-style; retargeted `net48` → `net10.0-windows`; removed stale `HintPath` references superseded by `PackageReference`; deleted `packages.config`.
  - `PlotFDEM/PlotFDEM.csproj`, `FDEMWindows/FDEMWindows.csproj`, `RandomRVEGenerator/RandomRVEGenerator.csproj` — added `<GenerateResourceUsePreserializedResources>true</GenerateResourceUsePreserializedResources>` so `.resx` image/icon resources compile to the modern preserialized `.resources` format instead of relying on `BinaryFormatter`.
- **Build/tooling fixes**
  - `animation/animation/Properties/AssemblyInfo.cs` — changed `[assembly: AssemblyVersion("1.0.*")]` to a fixed `"1.0.0.0"`; wildcard versions are incompatible with deterministic builds under the SDK-style project (`CS8357`).

### Git Commits

| SHA | Message |
|-----|---------|
| `93dbe64` | Fix PlotFDEM BinaryFormatter resx error: convert Animation to SDK-style/net10.0-windows, add System.Resources.Extensions preserialized resources |
| `0502823` | Merge upgrade-to-NET10: upgrade FiberDEM2D solution to .NET 10 (merge commit into `main`) |

---

## Decisions Made

- **Fix approach:** Use the supported "preserialized resources" mechanism (`System.Resources.Extensions` + `GenerateResourceUsePreserializedResources`) rather than re-enabling `BinaryFormatter` via `EnableUnsafeBinaryFormatterSerialization`, since that switch is deprecated/unsupported guidance.
- **Package version:** Pinned `System.Resources.Extensions` to the last stable GA release (9.0.0) instead of the 11.0.0 preview build surfaced by version lookup.
- **Branch strategy:** Merged `upgrade-to-NET10` into `main` with `--no-ff` (explicit merge commit) since `origin/main` had not diverged beyond its initial commit, then deleted the feature branch locally and on `origin` per user request.

---

## Build & Test Results

| Project | Build | Warnings |
|---------|-------|----------|
| Full solution (`FiberDEM.sln`) | ✅ 0 errors | 261 (pre-existing, unrelated to this fix — mostly nullable-reference warnings in `PlotFDEM`) |

Runtime verification: user confirmed PlotFDEM now opens the previously-failing plot (AnimatedPlot) without the `BinaryFormatter` exception.

---

## Known Gaps & Follow-up Items

- **Pre-existing build warnings (261)** — mostly `CS8602`/`CS8622` nullable-reference warnings and a few unused-field/CA2200 warnings in `PlotFDEM`. Not introduced by this fix; left as-is since they were out of scope for the reported runtime issue. Consider a follow-up nullable-reference cleanup pass.
- **`RandomRVEGeneratorConsole`** was not checked for the resx/BinaryFormatter pattern since it's a console app; no image resources were found referencing it, so no action was taken.

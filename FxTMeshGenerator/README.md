
# FiberMeshGen (net5.0)

A small, readable mesh generator for **fiber-center Delaunay triangulations** inside a rectangular cell boundary, with:

- **Periodic tiling** (no “quadrants” implementation) when the *pair* of opposite walls are periodic.
- **Solid boundary walls**: disables tiling in that direction **and** inserts a row of `boundaryPoints` along those edges (plus corners) that participate in triangulation.
- Legacy ASCII **VTK `.vtk` output** for quick visual inspection in ParaView.

This project is intentionally conservative on complexity (first step = “plot it and look at it”).
It focuses on maintainability and clear extension points for your later FE-element logic.

## Project layout

- `src/FiberMeshGen.Core` – core library
- `src/FiberMeshGen.Cli` – demo CLI that writes a `.vtk`
- `tests/FiberMeshGen.Tests` – xUnit tests

## Build

```bash
dotnet --version
# expects SDK that can build net5.0 projects

dotnet build
dotnet test
```

## Run the demo CLI

```bash
dotnet run --project src/FiberMeshGen.Cli --   --width 10 --height 5 --fibers 200   --solid top,right   --out mesh.vtk
```

Open `mesh.vtk` in ParaView.

Point scalar `node_type` is:
- `0` = fiber center
- `1` = boundary point

## How it maps to your MATLAB pipeline

MATLAB `FE_Mesh.GenerateMesh` does:
1. Load fibers + boundary from pack file
2. Create periodic quadrants (tiling)
3. Delaunay triangulation
4. Delete unwanted fibers/triads, build FE elements, boundary node pairs, etc.

This C# version **replaces** steps (1–3) with:

- Start from `CellBoundary2D` and `IReadOnlyList<Fiber2D>`
- Add boundary points on SOLID walls (and corners always)
- Tile only in directions where opposite walls are **both periodic**
- Delaunay triangulate via `DelaunatorSharp`
- Keep triangles with wrapped centroids inside the base cell and de-duplicate

The later FE-element construction steps are intentionally not implemented yet. The next step is to port your MATLAB “element building” logic on top of the `Mesh2D` output.

## Next extensions (planned hooks)

- Replace/augment `Mesh2D` triangles with:
  - fiber “patch” elements
  - matrix quad elements
  - boundary-specific element templates
- Build explicit periodic node-pair arrays (for FE constraints)
- Read your existing `FDEMCore.CellBoundary` and `FDEMCore.Fiber` types via adapter methods

See `MeshGenerator` for the main control flow.

## Notes

- NuGet reference uses `DelaunatorSharp`. If your environment uses a different Delaunay package, swap it behind the same API in `MeshGenerator`.
- Boundary point spacing uses `sqrt(area / nFibers)` (configurable via `MeshOptions.BoundaryPointSpacingMultiplier`).


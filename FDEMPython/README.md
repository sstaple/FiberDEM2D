# FDEMPython — Python Interoperability Layer

## Purpose

`FDEMPython` exposes FDEM's existing random RVE (representative volume element) generation
algorithm to external (non-.NET) callers — in particular, the NASA Python RVE-generation GUI —
without exposing FDEM's internal domain objects (`Fiber`, `Packing`, `RandomPack`,
`CellBoundary`, `Node`, `Element`, etc.).

It does **not** re-implement or alter the random-packing algorithm. It is a thin wrapper around
the common generation API in `FDEMCore` (`RandomRVEGenerationService`), which is the same code
path used by the existing textual random-RVE input-file workflow
(`RandomRVEGeneratorInputFile`) and `RandomPack`.

```
NASA Python application
		|
		v
FDEMPython (RveApi)                      <- thin pass-through, primitives/DTOs only
		|
		v
FDEMCore (RandomRVEGenerationService)    <- common RVE-generation API
		|
		v
FDEMCore.RandomPack                      <- existing, unmodified generation algorithm
```

## Public API

### `FDEMCore.RandomRVEGenerationOptions`

Strongly-typed options DTO covering the same generation parameters available through the
random-RVE input file (required parameters: fiber radius, fiber volume fraction, number of
rows/fibers, number of repetitions; plus all optional `RandomPack` options such as
`MinSpacingBetweenFibers`, `RVEHOverW`, `RVEThickness`, damping coefficients, relaxation step
counts, `MultipleRadii`/`MultipleRadiiPercentages`, boundary types, etc.).

### `FDEMCore.RandomRVEGenerationResult`

- `FiberLocations`: `double[N, 2]` — fiber center coordinates, `[i, 0]` = Y, `[i, 1]` = Z.
- `FiberRadii`: `double[N]` — fiber radius, aligned by index with `FiberLocations`.
- `BoundaryDimensions`: `double[2]` — `[0]` = RVE width (Y extent), `[1]` = RVE height (Z extent).

Coordinate/semantics notes (verified against `RandomPack`/`CellBoundary`):

- This is a 2-D cross-section (Y/Z); the fiber axis (X/length direction) is not included.
- Origin `(0, 0)` is the bottom-left corner of the RVE boundary, matching FDEM's
  `CellBoundary` convention.
- Units are whatever consistent length units were used for the input radius/volume fraction —
  FDEM does not enforce a specific unit system.
- Fibers that are periodically projected across the RVE boundary (used internally for
  relaxation/meshing of periodic wrap-around) are **not** included in the result; only the one
  "true" center per generated fiber is returned.

### `FDEMPython.RveApi.GenerateRandomRVE(RandomRVEGenerationOptions) -> RandomRVEGenerationResult`

Single entry point. Constructs the equivalent `RandomPack` configuration, runs the existing
generation/relaxation algorithm, and converts the result to the DTOs above.

## Python interop mechanism

For this first implementation, FDEMPython is a plain .NET class library exposing only
primitive-valued public types and static methods. The smallest, least invasive mechanism to
call it from Python is **[pythonnet](https://pythonnet.github.io/)** (`pip install pythonnet`),
which loads a .NET assembly directly into the Python process and lets Python call its public
types/methods with ordinary Python syntax — no additional native interop layer, IPC, or file
based hand-off is required.

Example (illustrative):

```python
import clr
clr.AddReference(r"C:\path\to\FDEMPython.dll")
clr.AddReference(r"C:\path\to\FDEMCore.dll")

from FDEMCore import RandomRVEGenerationOptions
from FDEMPython import RveApi

options = RandomRVEGenerationOptions()
options.FiberRadius = 1.0
options.FiberVolumeFraction = 0.4
options.NRows = 5

result = RveApi.GenerateRandomRVE(options)

# result.FiberLocations is a 2-D System.Double[,]; result.FiberRadii and
# result.BoundaryDimensions are System.Double[]. Convert to numpy as needed, e.g.:
import numpy as np
locations = np.array([[result.FiberLocations[i, 0], result.FiberLocations[i, 1]]
					   for i in range(result.FiberRadii.Length)])
radii = np.array(list(result.FiberRadii))
boundary = np.array(list(result.BoundaryDimensions))
```

If a fully decoupled, out-of-process interop mechanism becomes necessary later (e.g. for a
different Python runtime/version constraint), the same `RveApi`/DTO boundary can be re-exposed
behind that mechanism without further changes to FDEMCore's generation logic.

## Known limitation: determinism

`RandomPack`'s fiber seeding uses an unseeded `System.Random`, so two generation runs are not
guaranteed to produce identical fiber positions. Tests therefore verify structural correctness
(fiber/radius counts, valid radii, valid boundary dimensions, coordinate ranges) and that the
FDEMPython and FDEMCore pathways are consistent with each other, rather than asserting exact
position equality between runs.

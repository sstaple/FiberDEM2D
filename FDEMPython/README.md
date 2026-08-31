# FDEMPython — Python Interoperability Layer

## Purpose

`FDEMPython` exposes FDEM's existing random RVE (representative volume element) generation
algorithm to external (non-.NET) callers — in particular, the NASA Python RVE-generation GUI —
without exposing FDEM's internal domain objects (`Fiber`, `Packing`, `RandomPack`,
`CellBoundary`, `Node`, `Element`, etc.), and without requiring any knowledge of .NET, Python.NET,
or `clr.AddReference`.

It does **not** re-implement or alter the random-packing algorithm. Every layer below is a thin
pass-through onto the existing, unmodified `RandomPack` algorithm:

```
NASA Python application
		|
		v
import FDEMPython as fdem            <- Python package (this is what NASA uses)
  fdem.RandomRVEOptions               <- snake_case options dataclass
  fdem.generate_random_rve(options)   <- returns NumPy arrays
		|
		v  (Python.NET, hidden inside FDEMPython/_interop.py)
FDEMPython.dll : RveApi.GenerateRandomRVE(...)   <- thin .NET pass-through
		|
		v
FDEMCore.dll : RandomRVEGenerationService.Generate(...)  <- common RVE-generation API
		|
		v
FDEMCore.RandomPack                  <- existing, unmodified generation algorithm
```

## Preferred Python API

```python
import FDEMPython as fdem

options = fdem.RandomRVEOptions(
	fiber_radius=1.0,
	fiber_volume_fraction=0.30,
	n_rows=10,
)

result = fdem.generate_random_rve(options)

locations = result.locations   # numpy.ndarray, shape (N, 2): columns are (Y, Z)
radii = result.radii           # numpy.ndarray, shape (N,)
boundary = result.boundary     # numpy.ndarray, shape (2,): [width, height]
```

No manual conversion from `System.Double[,]` / `System.Double[]` is required — `generate_random_rve`
always returns plain NumPy arrays.

### `fdem.RandomRVEOptions` (snake_case)

Exposes the complete set of parameters supported by `FDEMCore.RandomRVEGenerationOptions`, with
identical defaults:

| Python (snake_case) | Maps to C# property | Default |
|---|---|---|
| `fiber_radius` | `FiberRadius` | `1.0` |
| `fiber_volume_fraction` | `FiberVolumeFraction` | `0.5` |
| `n_rows` | `NRows` | `5` |
| `n_repetitions` | `NRepetitions` | `1` |
| `fiber_linear_density` | `FiberLinearDensity` | `1.0` |
| `fiber_length` | `FiberLength` | `1.0` |
| `fiber_axial_modulus` | `FiberAxialModulus` | `1.0` |
| `fiber_transverse_modulus` | `FiberTransverseModulus` | `1.0` |
| `fiber_poissons_ratio` | `FiberPoissonsRatio` | `0.3` |
| `fiber_global_damping` | `FiberGlobalDamping` | `0.0` |
| `multiple_radii` / `multiple_radii_percentages` | `MultipleRadii` / `MultipleRadiiPercentages` | `None` |
| `min_spacing_between_fibers` | `MinSpacingBetweenFibers` | `0.0` |
| `n_fibers_per_square` | `NFibersPerSquare` | `1` |
| `square_margin` | `SquareMargin` | `0.75` |
| `rve_h_over_w` | `RVEHOverW` | `1.0` |
| `rve_thickness` | `RVEThickness` | `-1.0` |
| `contact_damping_coeff` | `ContactDampingCoeff` | `0.1` |
| `global_damping_coeff` | `GlobalDampingCoeff` | `1.0` |
| `increasing_damping_coeff` | `IncreasingDampingCoeff` | `0.001` |
| `per_ke_tol` | `PerKETol` | `0.01` |
| `n_max_steps` | `NMaxSteps` | `3000` |
| `n_undamped_steps` | `NUndampedSteps` | `500` |
| `is_n_rows_actually_n_fibers` | `IsNRowsActuallyNFibers` | `False` |
| `do_not_allow_overlaps` | `DoNotAllowOverlaps` | `False` |
| `min_spacing_between_fiber_and_solid_boundary` | `MinSpacingBetweenFiberAndSolidBoundary` | `0.0` |
| `solid_boundary_y` / `solid_boundary_z` | `SolidBoundaryY` / `SolidBoundaryZ` | `False` |

### `fdem.generate_random_rve(options) -> fdem.RandomRVEResult`

`RandomRVEResult` has three fields, all NumPy arrays: `locations` (N×2), `radii` (N,), and
`boundary` (2,). Coordinate convention (unchanged from `RandomRVEGenerationResult`):

- 2-D cross-section (Y/Z); the fiber axis (X/length direction) is not included.
- Origin `(0, 0)` is the bottom-left corner of the RVE boundary.
- `radii[i]` is the radius of the fiber centered at `locations[i]` (same index).
- Periodically-projected fibers are **not** included; only one center per generated fiber.

### `fdem.configure(assembly_dir=None)`

Optional. By default, the package auto-discovers `FDEMCore.dll`/`FDEMPython.dll` under
`FDEMPython/bin/<Config>/net10.0`. Call `fdem.configure(assembly_dir="...")`, or set the
`FDEM_ASSEMBLY_DIR` environment variable, to point at a different build/publish output folder.

## Python.NET handling (implementation detail, not part of the public API)

All Python.NET/CLR details — starting the CoreCLR runtime with the correct `net10.0`
`runtimeconfig.json`, `clr.AddReference`, and .NET reflection calls to translate between the
snake_case options and `FDEMCore.RandomRVEGenerationOptions` — are contained in
`FDEMPython/python/FDEMPython/_interop.py`, which is private/internal. NASA developers only ever
import `FDEMPython` and use `RandomRVEOptions` / `generate_random_rve` / `RandomRVEResult`.

Requires `pythonnet` and `numpy` in the Python environment (`pip install pythonnet numpy`).

## Layout

```
FDEMPython/
  FDEMPython.csproj, RveApi.cs      <- .NET project (unchanged public C# API)
  python/
	FDEMPython/
	  __init__.py                    <- public Python API (RandomRVEOptions, generate_random_rve, RandomRVEResult, configure)
	  _interop.py                    <- private: Python.NET/reflection glue
	examples/
	  smoke_test.py                  <- runnable usage example
	tests/
	  test_fdem_python.py            <- pytest tests for the Python API
```

## Running the example / tests

```powershell
dotnet build FDEMPython\FDEMPython.csproj
python FDEMPython\python\examples\smoke_test.py
python -m pytest FDEMPython\python\tests\test_fdem_python.py -v
```

## C# API (unchanged; still available as the underlying implementation API)

`FDEMPython.RveApi.GenerateRandomRVE(FDEMCore.RandomRVEGenerationOptions) -> FDEMCore.RandomRVEGenerationResult`
remains available for .NET callers and is what the Python `_interop` module calls via reflection.
See `FDEMCore/RandomRVEGeneration.cs` for `RandomRVEGenerationOptions`/`RandomRVEGenerationResult`/
`RandomRVEGenerationService` — these are unchanged and remain the shared implementation used by
both the textual random-RVE input-file workflow and this Python layer.

## Known limitation: determinism

`RandomPack`'s fiber seeding uses an unseeded `System.Random`, so two generation runs are not
guaranteed to produce identical fiber positions. Tests verify structural correctness (fiber/radius
counts, valid radii, valid boundary dimensions, coordinate ranges) rather than exact position
equality between runs.

## Known limitation: CLR/Python namespace collision

The Python package is named `FDEMPython` to satisfy `import FDEMPython as fdem`, but the .NET
assembly/namespace loaded via Python.NET is *also* named `FDEMPython`. Using Python.NET's normal
`import FDEMPython` / `from FDEMPython import RveApi` syntax from inside the Python package would
resolve to the Python package itself (circular self-import) rather than the CLR namespace.
`_interop.py` avoids this entirely by using `System.Reflection` (assembly/type/property/method
lookups by string name) instead of CLR namespace imports.

## Known limitation: CoreCLR runtime selection

Because this repository targets a very new `net10.0`, Python.NET's default runtime discovery can
select an older installed CoreCLR (e.g. a `net5.0`-era shared runtime) that fails to load
`FDEMPython.dll`/`FDEMCore.dll` (observed as `System.TypeLoadException: Could not load type
'System.Math'...`). To avoid this, `FDEMPython.csproj` now sets
`<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>` so a
`FDEMPython.runtimeconfig.json` is produced, and `_interop.py` explicitly loads CoreCLR via
`pythonnet.load("coreclr", runtime_config=...)` using that file before doing anything else. This
was verified to work locally (`dotnet --list-runtimes` shows `Microsoft.NETCore.App 10.0.9`
installed alongside older versions).

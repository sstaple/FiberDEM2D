# FDEMPython — Python Interoperability Layer

`FDEMPython` exposes FDEM's random RVE (representative volume element) generation capability to Python applications. It is intended to allow an external Python application, such as a GUI, to construct the RVE-generation options and call the existing FDEM generator without interacting directly with FDEM's internal C# domain objects.

The Python layer does **not** re-implement the random-packing algorithm. The generation request is translated into `FDEMCore.RandomRVEGenerationOptions`, passed through `FDEMPython.RveApi`, and ultimately handled by the existing `FDEMCore.RandomPack` algorithm.

```text
Python application
      |
      v
FDEMPython.RandomRVEOptions
      |
      v
FDEMPython.RveApi
      |
      v
FDEMCore.RandomRVEGenerationService
      |
      v
FDEMCore.RandomPack
      |
      v
NumPy arrays returned to Python
```

## Installation / requirements

The current implementation uses Python.NET to load the FDEM .NET assemblies in-process. The Python environment requires `pythonnet` and `numpy`:

```powershell
pip install pythonnet numpy
```

Build the FDEM solution before using the Python API:

```powershell
dotnet build FDEMPython\FDEMPython.csproj
```

The package automatically looks for the FDEM assemblies in the standard `FDEMPython/bin/<Configuration>/net10.0` build output. An alternate assembly directory can be supplied with `fdem.configure(assembly_dir=...)` or the `FDEM_ASSEMBLY_DIR` environment variable.

## Python API

The normal entry point is:

```python
import FDEMPython as fdem

options = fdem.RandomRVEOptions(
    fiber_radius=3.0,
    fiber_volume_fraction=0.50,
    n_rows=10,
)

result = fdem.generate_random_rve(options)

locations = result.locations
radii = result.radii
boundary = result.boundary
```

**One call generates one RVE realization.** To generate multiple independent realizations, call `generate_random_rve()` repeatedly from Python.

## `RandomRVEOptions`

`RandomRVEOptions` exposes the options used by the FDEM random RVE generator. Python names use `snake_case`. The defaults below match the defaults in the current `FDEMCore` implementation unless otherwise noted.

### Required generation parameters

These are the core parameters used to define the RVE.

| Python option | Default | Description |
|---|---:|---|
| `fiber_radius` | `1.0` | Radius of the fibers. Units are arbitrary, but all length quantities must use a consistent system. A value is still required when `multiple_radii` is used. |
| `fiber_volume_fraction` | `0.5` | Target fiber volume fraction, defined as fiber area divided by RVE area. With multiple radii, the actual volume fraction is calculated from the selected radii and the RVE dimensions are adjusted accordingly. |
| `n_rows` | `5` | By default, the nominal number of fibers is `n_rows²`. If `is_n_rows_actually_n_fibers=True`, this instead specifies the number of fibers directly. |

## Fiber parameters

These values are available in the programmatic FDEM API and are passed to the underlying fiber definition.

| Python option | Default | Description |
|---|---:|---|
| `fiber_linear_density` | `1.0` | Fiber linear density. |
| `fiber_length` | `1.0` | Fiber length. |
| `fiber_axial_modulus` | `1.0` | Fiber axial modulus. |
| `fiber_transverse_modulus` | `1.0` | Fiber transverse modulus. |
| `fiber_poissons_ratio` | `0.3` | Fiber Poisson's ratio. |
| `fiber_global_damping` | `0.0` | Global damping associated with the fiber definition. |

### Multiple radii

`multiple_radii` and `multiple_radii_percentages` can be used to define a discrete distribution of fiber radii.

```python
options = fdem.RandomRVEOptions(
    multiple_radii=[1.0, 2.0, 4.0, 6.0],
    multiple_radii_percentages=[40.0, 30.0, 15.0, 15.0],
)
```

The two arrays must have the same length. The percentages should sum to 100%. A random number is generated for each fiber and its radius is selected from the specified distribution. Because this is a random assignment, the realized percentage distribution can differ slightly from the requested values. With multiple radii, the volume fraction is based on the actual radii assigned to the fibers rather than the nominal `fiber_radius`.

## Fiber seeding

During initialization, the RVE can be divided into square cells. Fibers are allocated among the cells and randomly positioned within them. This can affect the resulting clustering and can also accelerate the relaxation by initially spreading the fibers throughout the RVE.

| Python option | Default | Description |
|---|---:|---|
| `n_fibers_per_square` | `1` | Controls the number of fibers assigned to each seeding square. As with `n_rows`, this value represents the square root of the number of fibers in a square. If it is greater than the effective number of rows, a single square is used. |
| `square_margin` | `0.75` | Dimensionless margin used when placing fibers within each seeding square. The actual margin is `square_margin × fiber_radius`; for multiple radii, the maximum radius is used. |

## Packing and relaxation

After seeding, FDEM relaxes the fibers to produce the final packing.

| Python option | Default | Description |
|---|---:|---|
| `min_spacing_between_fibers` | `0.0` | Minimum desired spacing between fibers. The implementation temporarily increases each fiber radius by half this value during relaxation and restores the requested radius afterward. A small positive value can be useful when the RVE will subsequently be meshed. |
| `contact_damping_coeff` | `0.1` | Damping coefficient for relative motion between contacting fibers. A value of `1` corresponds to critical damping; values below 1 are underdamped and values above 1 are overdamped. |
| `global_damping_coeff` | `1.0` | Damping applied to individual fiber motion during relaxation. |
| `increasing_damping_coeff` | `0.001` | Controls the increase in global damping as the relaxation proceeds, helping the fibers settle. |
| `n_max_steps` | `3000` | Maximum number of relaxation time steps. |
| `n_undamped_steps` | `500` | Number of initial relaxation steps before global damping is applied. |
| `per_ke_tol` | `0.01` | Kinetic-energy cutoff used to stop the relaxation. A higher value can terminate the relaxation sooner but may leave more fiber overlap. |
| `do_not_allow_overlaps` | `False` | Requests a final overlap check and, when necessary, a second relaxation pass. If overlaps remain after the second pass, generation throws an exception. |

The relaxation terminates when the kinetic-energy criterion is reached or the maximum number of time steps is exhausted.

## RVE geometry and boundaries

| Python option | Default | Description |
|---|---:|---|
| `is_n_rows_actually_n_fibers` | `False` | If `True`, `n_rows` is interpreted as the number of fibers rather than the square-root-based row count. |
| `rve_h_over_w` | `1.0` | RVE height-to-width ratio. `1.0` produces a square RVE; values greater than 1 produce a taller RVE and values less than 1 produce a wider RVE. |
| `rve_thickness` | `-1.0` | Controls the RVE thickness used by the underlying FDEM boundary-dimension calculation. A negative value uses the standard aspect-ratio calculation. |
| `min_spacing_between_fiber_and_solid_boundary` | `0.0` | Minimum spacing between fibers and a solid boundary. |
| `solid_boundary_y` | `False` | If `True`, the Y boundary is solid. Otherwise it is periodic. |
| `solid_boundary_z` | `False` | If `True`, the Z boundary is solid. Otherwise it is periodic. |

## Output options

The generated RVE is returned directly to Python, so all file output is **disabled by default**. Enable these options only when files are also desired.

| Python option | Default | Description |
|---|---:|---|
| `save_results` | `False` | Save the relaxation history, including fiber positions, contacts, homogenized stress/strain, and boundary information at intervals during the relaxation. These results can be used with PlotFDEM to inspect the generation process. |
| `save_final_positions` | `False` | Save the final fiber positions and radii after relaxation. |
| `save_final_positions_without_projections` | `False` | Save the final fiber positions and radii without periodically projected fibers. |
| `save_vf_statistics` | `False` | Save statistics describing the distribution of local fiber volume fractions, including the mean, IQR, and kurtosis. |
| `save_connection_plot` | `False` | Save the connection/triangulation plot used with the local-volume-fraction calculations. This option is only active when `save_vf_statistics=True`. |
| `output_directory` | `None` | Directory in which requested output files are written. If omitted, the current working directory is used when file output is enabled. |
| `output_file_name` | `"FDEMPython_RVE"` | Base name used for requested output files. |

## Result

`generate_random_rve()` returns a `RandomRVEResult` containing three NumPy arrays:

```python
result.locations
result.radii
result.boundary
```

Their current representation is intentionally left unchanged so that the calling Python application can reshape or reorganize the data as needed.

- `locations`: fiber-center coordinates, with shape `(N, 2)`. Column 0 is Y and column 1 is Z.
- `radii`: fiber radii, with shape `(N,)`. `radii[i]` corresponds to `locations[i]`.
- `boundary`: RVE dimensions, with shape `(2,)`. Element 0 is width and element 1 is height.

The origin is the bottom-left corner of the RVE. The returned data represent the unwrapped fibers: periodically projected copies are not included.

## Example

```python
import FDEMPython as fdem

options = fdem.RandomRVEOptions(
    fiber_radius=3.0,
    fiber_volume_fraction=0.70,
    n_rows=17,
    n_fibers_per_square=7,
    square_margin=0.61,
    min_spacing_between_fibers=0.5,
    contact_damping_coeff=0.8,
    global_damping_coeff=1.2,
    n_max_steps=8000,
    n_undamped_steps=100,
    per_ke_tol=0.001,
    do_not_allow_overlaps=True,
)

result = fdem.generate_random_rve(options)
```

A second realization with the same parameters is obtained simply by calling the function again:

```python
result2 = fdem.generate_random_rve(options)
```

Because the current random-packing implementation uses unseeded random-number generation, repeated calls are not expected to produce identical fiber positions.

## Python.NET implementation details

Python.NET and CLR details are intentionally hidden inside `FDEMPython/python/FDEMPython/_interop.py`. Users of the package normally only need:

```python
import FDEMPython as fdem
```

and the public `RandomRVEOptions`, `generate_random_rve`, `RandomRVEResult`, and `configure` interfaces.

The package uses reflection internally because the Python package name `FDEMPython` and the CLR namespace `FDEMPython` would otherwise collide during import. The .NET runtime is explicitly loaded from the generated `net10.0` runtime configuration when necessary.

## Tests

The Python API has a smoke test and pytest coverage for option construction, end-to-end execution, NumPy conversion, and result shape/consistency checks.

```powershell
python FDEMPython\python\examples\smoke_test.py
python -m pytest FDEMPython\python\tests\test_fdem_python.py -v
```

## Source and algorithm

The Python interoperability layer is an adapter around the existing FDEM random-packing implementation. The authoritative implementation of the generation options and result contract is in `FDEMCore/RandomRVEGeneration.cs`; the underlying packing behavior is implemented by `RandomPack` in `FDEMCore/SetPacking.cs`.

For a description of the original random-RVE input-file workflow and the generation parameters, see `RandomRVEGenerator/RandomRVEGenerator_UsersManual_v6.pdf`.

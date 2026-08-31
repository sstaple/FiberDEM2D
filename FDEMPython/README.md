# FDEMPython

Python interoperability layer for FDEM random RVE generation.

`FDEMPython` lets an external Python application call the existing FDEM random RVE generator without interacting directly with FDEM's internal C# objects. The Python layer is an adapter; it does not re-implement the random-packing algorithm.

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

## Python API

```python
import FDEMPython as fdem

options = fdem.RandomRVEOptions(
    fiber_radius=3.0,
    fiber_volume_fraction=0.70,
    n_rows=17,
)

result = fdem.generate_random_rve(options)

locations = result.locations
radii = result.radii
boundary = result.boundary
```

**One call generates one RVE.** To generate multiple independent realizations, call `generate_random_rve()` repeatedly from Python.

## Requirements

The current implementation uses Python.NET to load the FDEM .NET assemblies in-process. Install the Python dependencies with:

```powershell
pip install pythonnet numpy
```

Build the FDEM solution before using the Python API:

```powershell
dotnet build FDEMPython\FDEMPython.csproj
```

The package normally discovers `FDEMCore.dll` and `FDEMPython.dll` in the standard `FDEMPython/bin/<Configuration>/net10.0` build output. A different assembly directory can be supplied with `fdem.configure(assembly_dir=...)` or the `FDEM_ASSEMBLY_DIR` environment variable.

## `RandomRVEOptions`

The options below are the parameters intended for the RVE-generation GUI/API. Names use Python `snake_case`. Defaults match the current FDEM random-packing implementation.

### Basic generation

| Option | Default | Description |
|---|---:|---|
| `fiber_radius` | `1.0` | Radius of the fibers. Units are arbitrary, but all length quantities must be consistent. This value is still required when `multiple_radii` is used. |
| `fiber_volume_fraction` | `0.5` | Target fiber volume fraction, defined as total fiber area divided by RVE area. |
| `n_rows` | `5` | Number of rows used to determine the fiber count. By default the number of fibers is `n_rows²`. See `is_n_rows_actually_n_fibers` to use this value directly as the fiber count. |
| `is_n_rows_actually_n_fibers` | `False` | If `True`, `n_rows` specifies the number of fibers directly rather than the square root of the fiber count. |

### Multiple fiber radii

| Option | Default | Description |
|---|---:|---|
| `multiple_radii` | `None` | Optional list of discrete fiber radii. When supplied, these radii are used instead of the single `fiber_radius`. |
| `multiple_radii_percentages` | `None` | Percentage of fibers assigned to each radius. Must have the same length as `multiple_radii`; percentages should sum to 100. |

Example:

```python
options = fdem.RandomRVEOptions(
    multiple_radii=[1.0, 2.0, 4.0, 6.0],
    multiple_radii_percentages=[40.0, 30.0, 15.0, 15.0],
)
```

The radius of each fiber is selected randomly from the specified distribution, so the realized percentages can differ slightly from the requested values. The actual fiber radii are used when calculating the final RVE dimensions.

### Fiber seeding

The RVE can be divided into square cells during initial fiber placement. This can influence clustering and can improve relaxation performance by initially spreading fibers throughout the RVE.

| Option | Default | Description |
|---|---:|---|
| `n_fibers_per_square` | `1` | Controls the fiber population assigned to each seeding square. The parameter follows the same square-root convention used by `n_rows`. |
| `square_margin` | `0.75` | Dimensionless seeding margin. The physical margin is `square_margin × fiber_radius`; the maximum radius is used for multiple-radii cases. |

### Packing and relaxation

| Option | Default | Description |
|---|---:|---|
| `min_spacing_between_fibers` | `0.0` | Minimum desired spacing between fibers. During relaxation the fiber radius is temporarily increased by half this value, then restored. A small positive value can help avoid minor overlaps before finite-element meshing. |
| `contact_damping_coeff` | `0.1` | Damping coefficient for relative motion between contacting fibers. A value of 1 corresponds to critical damping; values below 1 are underdamped and values above 1 are overdamped. |
| `global_damping_coeff` | `1.0` | Damping coefficient applied to individual fiber motion. |
| `increasing_damping_coeff` | `0.001` | Controls the increase in global damping as the relaxation proceeds. |
| `n_max_steps` | `3000` | Maximum number of relaxation time steps. |
| `n_undamped_steps` | `500` | Number of initial steps before global damping is applied. Contact damping is still active during these steps. |
| `per_ke_tol` | `0.01` | Kinetic-energy cutoff for terminating relaxation. A higher value can stop the relaxation sooner but may leave more overlap. |
| `do_not_allow_overlaps` | `False` | Checks for remaining overlaps after relaxation. If overlaps remain, the packing is relaxed again from the current configuration; an exception is raised if overlaps remain after the second pass. |

The relaxation terminates when the kinetic-energy cutoff is reached or the maximum number of steps is exhausted.

### RVE geometry

| Option | Default | Description |
|---|---:|---|
| `rve_h_over_w` | `1.0` | RVE height-to-width ratio. `1.0` gives a square RVE; values greater than 1 give a taller RVE; values less than 1 give a wider RVE. |
| `rve_thickness` | `-1.0` | RVE thickness used in the boundary-dimension calculation. A negative value uses the standard height-to-width calculation. |
| `min_spacing_between_fiber_and_solid_boundary` | `0.0` | Minimum spacing between fibers and a solid RVE boundary. |

### Boundary conditions

| Option | Default | Description |
|---|---:|---|
| `solid_boundary_y` | `False` | If `True`, the Y boundary is solid. Otherwise it is periodic. |
| `solid_boundary_z` | `False` | If `True`, the Z boundary is solid. Otherwise it is periodic. |

### Output

The RVE is returned directly to Python, so file output is disabled by default.

| Option | Default | Description |
|---|---:|---|
| `save_results` | `False` | Save the relaxation history, including fiber positions, contacts, homogenized stress/strain, and boundary information during the relaxation. These files can be inspected with PlotFDEM. |
| `save_final_positions` | `False` | Save the final fiber positions and radii after relaxation. |
| `save_final_positions_without_projections` | `False` | Save the final fiber positions and radii without periodically projected fibers. |
| `save_vf_statistics` | `False` | Save statistics for the distribution of local fiber volume fraction, including mean, IQR, and kurtosis. |
| `save_connection_plot` | `False` | Save the connection/triangulation plot associated with the volume-fraction calculation. This option is only active when `save_vf_statistics=True`. |
| `output_directory` | `None` | Directory for requested output files. If omitted, the current working directory is used. |
| `output_file_name` | `"FDEMPython_RVE"` | Base name for requested output files. |

## Result

`generate_random_rve()` returns a `RandomRVEResult` containing three NumPy arrays. Their representation is intentionally simple so that the calling Python application can reshape or reorganize them as needed.

```python
result.locations
result.radii
result.boundary
```

- `locations`: shape `(N, 2)`, with column 0 = Y and column 1 = Z.
- `radii`: shape `(N,)`, with `radii[i]` corresponding to `locations[i]`.
- `boundary`: shape `(2,)`, with element 0 = width and element 1 = height.

The origin is the bottom-left corner of the RVE. Periodically projected fibers are not included in the returned arrays; one unwrapped center is returned for each generated fiber.

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

A second independent realization is obtained by calling the function again:

```python
result2 = fdem.generate_random_rve(options)
```

## Configuration

For a non-standard build location:

```python
fdem.configure(assembly_dir=r"C:\path\to\FDEMPython\bin\Release\net10.0")
```

## Implementation notes

Python.NET and CLR details are hidden inside `FDEMPython/python/FDEMPython/_interop.py`. The Python package uses reflection internally because the Python package name and the CLR namespace are both `FDEMPython`.

The underlying FDEM generation algorithm remains in `FDEMCore`. The Python layer is intended to be a thin interoperability boundary rather than a second implementation of the RVE generator.

## Tests

```powershell
python FDEMPython\python\examples\smoke_test.py
python -m pytest FDEMPython\python\tests\test_fdem_python.py -v
```

## Reference

The parameter descriptions in this README are based on the Random RVE Generator User's Manual and the current FDEM implementation.

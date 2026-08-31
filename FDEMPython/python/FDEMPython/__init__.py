"""
FDEMPython
==========

Python-facing API for FDEM random RVE (representative volume element) generation.

This package is a thin wrapper around the existing, unmodified FDEM RVE-generation
algorithm (``FDEMCore.RandomRVEGenerationService`` / ``RandomPack``), loaded in-process
via Python.NET. Callers only need this package - there is no need to know about
FDEMCore, .NET DTOs, ``clr.AddReference``, or .NET array types.

Typical usage
-------------
>>> import FDEMPython as fdem
>>> options = fdem.RandomRVEOptions(
...     fiber_radius=1.0,
...     fiber_volume_fraction=0.30,
...     n_rows=10,
... )
>>> result = fdem.generate_random_rve(options)
>>> result.locations.shape   # (N, 2) numpy array: columns are (Y, Z)
>>> result.radii.shape       # (N,) numpy array
>>> result.boundary.shape    # (2,) numpy array: [width, height]

Coordinate convention (matches FDEMCore.RandomRVEGenerationResult):
 - 2-D cross-section (Y/Z); the fiber axis (X/length direction) is not included.
 - Origin (0, 0) is the bottom-left corner of the RVE boundary.
 - ``result.radii[i]`` is the radius of the fiber centered at ``result.locations[i]``.
 - Periodically-projected fibers are not included; only one center per generated fiber.

.NET assembly discovery: by default this package looks for ``FDEMCore.dll`` and
``FDEMPython.dll`` next to its own build output (``FDEMPython/bin/<Config>/net10.0``).
Call ``fdem.configure(assembly_dir=...)`` or set the ``FDEM_ASSEMBLY_DIR`` environment
variable to point at a different build output directory (e.g. for a Release publish).
"""

from dataclasses import dataclass, field
from typing import List, Optional

from . import _interop

__all__ = ["RandomRVEOptions", "RandomRVEResult", "generate_random_rve", "configure"]


@dataclass
class RandomRVEOptions:
	"""
	Python-friendly (snake_case) options for random RVE generation.

	Exposes the complete set of generation parameters currently supported by
	``FDEMCore.RandomRVEGenerationOptions``; defaults match that type's defaults
	exactly. See FDEMCore.RandomRVEGeneration.cs for the authoritative semantics
	of each option.
	"""

	# Required generation parameters
	fiber_radius: float = 1.0
	fiber_volume_fraction: float = 0.5
	n_rows: int = 5
	n_repetitions: int = 1

	# Fiber material parameters
	fiber_linear_density: float = 1.0
	fiber_length: float = 1.0
	fiber_axial_modulus: float = 1.0
	fiber_transverse_modulus: float = 1.0
	fiber_poissons_ratio: float = 0.3
	fiber_global_damping: float = 0.0

	# Optional multiple-radii fiber population (both or neither must be set)
	multiple_radii: Optional[List[float]] = None
	multiple_radii_percentages: Optional[List[float]] = None

	# Optional RandomPack generation options
	min_spacing_between_fibers: float = 0.0
	n_fibers_per_square: int = 1
	square_margin: float = 0.75
	rve_h_over_w: float = 1.0
	rve_thickness: float = -1.0
	contact_damping_coeff: float = 0.1
	global_damping_coeff: float = 1.0
	increasing_damping_coeff: float = 0.001
	per_ke_tol: float = 0.01
	n_max_steps: int = 3000
	n_undamped_steps: int = 500
	is_n_rows_actually_n_fibers: bool = False
	do_not_allow_overlaps: bool = False
	min_spacing_between_fiber_and_solid_boundary: float = 0.0
	solid_boundary_y: bool = False
	solid_boundary_z: bool = False


@dataclass
class RandomRVEResult:
	"""
	Result of a random RVE generation, as plain NumPy arrays.

	- ``locations``: shape (N, 2) - fiber center (Y, Z) coordinates.
	- ``radii``: shape (N,) - fiber radius, aligned by index with ``locations``.
	- ``boundary``: shape (2,) - [width, height] of the RVE.
	"""

	locations: "object"
	radii: "object"
	boundary: "object"


def configure(assembly_dir=None):
	"""
	Explicitly initialize the underlying .NET runtime and load the FDEM assemblies
	from ``assembly_dir``. Optional: the first call to ``generate_random_rve`` will
	auto-initialize using a best-effort default location if this is not called.
	"""
	_interop.configure(assembly_dir)


def generate_random_rve(options: RandomRVEOptions) -> RandomRVEResult:
	"""
	Generate a single random RVE realization.

	Internally: maps ``options`` to ``FDEMCore.RandomRVEGenerationOptions``, invokes
	the existing ``FDEMCore.RandomRVEGenerationService`` (via ``FDEMPython.RveApi``),
	and converts the returned .NET arrays to NumPy arrays. Does not duplicate or alter
	the underlying random-packing algorithm in any way.
	"""
	native_options = _interop.build_native_options(options)
	native_result = _interop.invoke_generate(native_options)
	locations, radii, boundary = _interop.convert_result_to_numpy(native_result)
	return RandomRVEResult(locations=locations, radii=radii, boundary=boundary)

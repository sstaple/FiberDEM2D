"""
FDEMPython
==========

Python-facing API for FDEM random RVE generation.
"""

from dataclasses import dataclass
from typing import List, Optional

from . import _interop

__all__ = ["RandomRVEOptions", "RandomRVEResult", "generate_random_rve", "configure"]


@dataclass
class RandomRVEOptions:
	"""Python-friendly options for generating a single random RVE realization."""

	fiber_radius: float = 1.0
	fiber_volume_fraction: float = 0.5
	n_rows: int = 5

	fiber_linear_density: float = 1.0
	fiber_length: float = 1.0
	fiber_axial_modulus: float = 1.0
	fiber_transverse_modulus: float = 1.0
	fiber_poissons_ratio: float = 0.3
	fiber_global_damping: float = 0.0

	multiple_radii: Optional[List[float]] = None
	multiple_radii_percentages: Optional[List[float]] = None

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

	# Optional file output. All disabled by default because the generated RVE is returned directly.
	save_results: bool = False
	save_final_positions: bool = False
	save_final_positions_without_projections: bool = False
	save_vf_statistics: bool = False
	save_connection_plot: bool = False
	output_directory: Optional[str] = None
	output_file_name: str = "FDEMPython_RVE"


@dataclass
class RandomRVEResult:
	"""Result as plain NumPy arrays: locations (N,2), radii (N,), boundary (2,)."""

	locations: "object"
	radii: "object"
	boundary: "object"


def configure(assembly_dir=None):
	"""Explicitly initialize the .NET runtime and load the FDEM assemblies."""
	_interop.configure(assembly_dir)


def generate_random_rve(options: RandomRVEOptions) -> RandomRVEResult:
	"""Generate exactly one random RVE realization from the supplied options."""
	native_options = _interop.build_native_options(options)
	native_result = _interop.invoke_generate(native_options)
	locations, radii, boundary = _interop.convert_result_to_numpy(native_result)
	return RandomRVEResult(locations=locations, radii=radii, boundary=boundary)

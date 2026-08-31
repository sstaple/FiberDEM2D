"""
Internal interoperability helpers for the FDEMPython package.

This module contains everything that talks to Python.NET / the FDEMCore and
FDEMPython .NET assemblies. Nothing here is part of the public API; callers
should use the top-level ``FDEMPython`` package (``RandomRVEOptions``,
``generate_random_rve``, ``RandomRVEResult``) instead.

Design note - why reflection instead of ``import FDEMCore`` / ``import FDEMPython``:
The public Python package is intentionally named ``FDEMPython`` (so that
``import FDEMPython as fdem`` works, per the desired API). The .NET assembly
that Python.NET loads is *also* named ``FDEMPython`` and defines a
``FDEMPython`` CLR namespace. If this module used Python.NET's normal
``import FDEMPython`` / ``from FDEMPython import RveApi`` syntax, Python's
import system would simply return the already-imported Python package
(circular self-import) instead of resolving the CLR namespace. To avoid that
name collision entirely, this module talks to .NET purely through
``System.Reflection`` (assembly/type/property/method lookups by string name),
which does not go through Python's module import machinery at all.
"""

import glob
import os
import sys
import threading

_lock = threading.Lock()
_initialized = False

# Cached .NET reflection handles, populated by _ensure_initialized().
_options_type = None
_result_type = None
_rve_api_type = None
_generate_method = None
_double_array_ctor = None


def _default_assembly_dir():
	"""
	Best-effort discovery of the folder containing FDEMCore.dll / FDEMPython.dll.

	Looks for the standard build output directories next to this source tree
	(``FDEMPython/bin/Debug/net10.0`` or ``FDEMPython/bin/Release/net10.0``),
	preferring the most recently built one. Can always be overridden explicitly
	via ``configure(assembly_dir=...)`` or the ``FDEM_ASSEMBLY_DIR`` environment
	variable.
	"""
	env_dir = os.environ.get("FDEM_ASSEMBLY_DIR")
	if env_dir:
		return env_dir

	# This file lives at <repo>/FDEMPython/python/FDEMPython/_interop.py
	package_dir = os.path.dirname(os.path.abspath(__file__))
	csproj_dir = os.path.abspath(os.path.join(package_dir, "..", ".."))

	candidates = glob.glob(os.path.join(csproj_dir, "bin", "*", "net*"))
	candidates = [c for c in candidates if os.path.isfile(os.path.join(c, "FDEMPython.dll"))]
	if not candidates:
		raise RuntimeError(
			"Could not locate FDEMPython.dll / FDEMCore.dll build output. "
			"Build the FDEMPython project first (dotnet build FDEMPython/FDEMPython.csproj), "
			"or call FDEMPython.configure(assembly_dir='path/to/build/output'), "
			"or set the FDEM_ASSEMBLY_DIR environment variable."
		)
	candidates.sort(key=os.path.getmtime, reverse=True)
	return candidates[0]


def configure(assembly_dir=None):
	"""
	Explicitly (re-)initialize the .NET runtime and load the FDEM assemblies
	from ``assembly_dir``. Optional - if not called, the first API call will
	auto-initialize using ``_default_assembly_dir()``.
	"""
	_ensure_initialized(assembly_dir, force=True)


def _ensure_initialized(assembly_dir=None, force=False):
	global _initialized, _options_type, _result_type, _rve_api_type
	global _generate_method, _double_array_ctor

	with _lock:
		if _initialized and not force:
			return

		if assembly_dir is None:
			assembly_dir = _default_assembly_dir()

		# Explicitly host the CoreCLR runtime version that matches FDEMPython's target
		# framework (net10.0), using its runtimeconfig.json. Without this, pythonnet's
		# default runtime discovery can select an older installed CoreCLR that fails to
		# load newer runtime assemblies (observed as a System.TypeLoadException for
		# forwarded types such as System.Math).
		import pythonnet

		runtime_config = os.path.join(assembly_dir, "FDEMPython.runtimeconfig.json")
		if os.path.isfile(runtime_config) and not pythonnet.get_runtime_info():
			pythonnet.load("coreclr", runtime_config=runtime_config)

		import clr  # noqa: F401  (import starts the pythonnet/.NET runtime)
		import System
		import System.Reflection as Reflection

		if assembly_dir not in sys.path:
			sys.path.append(assembly_dir)

		clr.AddReference(os.path.join(assembly_dir, "FDEMCore.dll"))
		clr.AddReference(os.path.join(assembly_dir, "FDEMPython.dll"))

		core_assembly = Reflection.Assembly.Load("FDEMCore")
		py_assembly = Reflection.Assembly.Load("FDEMPython")

		_options_type = core_assembly.GetType("FDEMCore.RandomRVEGenerationOptions")
		_result_type = core_assembly.GetType("FDEMCore.RandomRVEGenerationResult")
		_rve_api_type = py_assembly.GetType("FDEMPython.RveApi")
		_generate_method = _rve_api_type.GetMethod("GenerateRandomRVE")
		_double_array_ctor = System.Array[System.Double]

		if _options_type is None or _result_type is None or _rve_api_type is None:
			raise RuntimeError(
				"Loaded FDEMCore/FDEMPython assemblies but could not find the expected "
				"types. This usually means the assemblies in '{}' are out of date; "
				"rebuild the FDEMPython project.".format(assembly_dir)
			)

		_initialized = True


def _to_net_double_array(values):
	return _double_array_ctor([float(v) for v in values])


def build_native_options(options):
	"""Translate a (python) RandomRVEOptions dataclass into the native FDEMCore.RandomRVEGenerationOptions."""
	_ensure_initialized()
	import System

	native = System.Activator.CreateInstance(_options_type)

	# Explicit boxed-.NET-type conversions. Plain Python ints/floats/bools passed
	# through System.Reflection's PropertyInfo.SetValue are not implicitly widened
	# the way a direct pythonnet property assignment would be (e.g. a Python int
	# is boxed as System.Int64 by default, which does not match an Int32 property),
	# so each value is explicitly boxed as the correct .NET primitive type first.
	_net_ctor_by_clr_name = {
		"Double": System.Double,
		"Int32": System.Int32,
		"Boolean": System.Boolean,
	}

	def set_prop(name, value):
		prop = _options_type.GetProperty(name)
		if value is not None and not isinstance(value, System.Array):
			ctor = _net_ctor_by_clr_name.get(prop.PropertyType.Name)
			if ctor is not None:
				value = ctor(value)
		prop.SetValue(native, value)

	set_prop("FiberRadius", float(options.fiber_radius))
	set_prop("FiberVolumeFraction", float(options.fiber_volume_fraction))
	set_prop("NRows", int(options.n_rows))
	set_prop("NRepetitions", int(options.n_repetitions))

	set_prop("FiberLinearDensity", float(options.fiber_linear_density))
	set_prop("FiberLength", float(options.fiber_length))
	set_prop("FiberAxialModulus", float(options.fiber_axial_modulus))
	set_prop("FiberTransverseModulus", float(options.fiber_transverse_modulus))
	set_prop("FiberPoissonsRatio", float(options.fiber_poissons_ratio))
	set_prop("FiberGlobalDamping", float(options.fiber_global_damping))

	if options.multiple_radii is not None:
		set_prop("MultipleRadii", _to_net_double_array(options.multiple_radii))
	if options.multiple_radii_percentages is not None:
		set_prop("MultipleRadiiPercentages", _to_net_double_array(options.multiple_radii_percentages))

	set_prop("MinSpacingBetweenFibers", float(options.min_spacing_between_fibers))
	set_prop("NFibersPerSquare", int(options.n_fibers_per_square))
	set_prop("SquareMargin", float(options.square_margin))
	set_prop("RVEHOverW", float(options.rve_h_over_w))
	set_prop("RVEThickness", float(options.rve_thickness))
	set_prop("ContactDampingCoeff", float(options.contact_damping_coeff))
	set_prop("GlobalDampingCoeff", float(options.global_damping_coeff))
	set_prop("IncreasingDampingCoeff", float(options.increasing_damping_coeff))
	set_prop("PerKETol", float(options.per_ke_tol))
	set_prop("NMaxSteps", int(options.n_max_steps))
	set_prop("NUndampedSteps", int(options.n_undamped_steps))
	set_prop("IsNRowsActuallyNFibers", bool(options.is_n_rows_actually_n_fibers))
	set_prop("DoNotAllowOverlaps", bool(options.do_not_allow_overlaps))
	set_prop("MinSpacingBetweenFiberAndSolidBoundary", float(options.min_spacing_between_fiber_and_solid_boundary))
	set_prop("SolidBoundaryY", bool(options.solid_boundary_y))
	set_prop("SolidBoundaryZ", bool(options.solid_boundary_z))

	return native


def invoke_generate(native_options):
	_ensure_initialized()
	# Static method invocation: instance argument is None, args array holds the one parameter.
	return _generate_method.Invoke(None, [native_options])


def _to_numpy_1d(net_array):
	import numpy as np

	return np.fromiter((x for x in net_array), dtype=np.float64, count=net_array.Length)


def _to_numpy_2d(net_array):
	import numpy as np

	rows = net_array.GetLength(0)
	cols = net_array.GetLength(1)
	flat = np.fromiter((x for x in net_array), dtype=np.float64, count=rows * cols)
	return flat.reshape(rows, cols)


def convert_result_to_numpy(native_result):
	locations = _to_numpy_2d(native_result.FiberLocations)
	radii = _to_numpy_1d(native_result.FiberRadii)
	boundary = _to_numpy_1d(native_result.BoundaryDimensions)
	return locations, radii, boundary

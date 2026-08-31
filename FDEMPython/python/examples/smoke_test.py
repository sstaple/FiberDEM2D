"""
Simple smoke-test / usage example for the FDEMPython package.

Run from the repository root after building the FDEMPython project, e.g.:

	dotnet build FDEMPython/FDEMPython.csproj
	python FDEMPython/python/examples/smoke_test.py

If FDEMCore.dll / FDEMPython.dll are not in the default build output location,
set FDEM_ASSEMBLY_DIR to the folder containing them first.
"""

import os
import sys

# Make the FDEMPython package importable when running this script directly
# from a source checkout (not required if FDEMPython has been pip-installed).
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import FDEMPython as fdem


def main():
	options = fdem.RandomRVEOptions(
		fiber_radius=1.0,
		fiber_volume_fraction=0.30,
		n_rows=4,  # -> 16 fibers
		n_max_steps=300,
		n_undamped_steps=50,
	)

	result = fdem.generate_random_rve(options)

	print("locations shape:", result.locations.shape)
	print("radii shape:", result.radii.shape)
	print("boundary (width, height):", result.boundary)
	print("first fiber:", result.locations[0], "radius:", result.radii[0])

	assert result.locations.shape == (result.radii.shape[0], 2)
	assert result.boundary.shape == (2,)
	print("Smoke test passed.")


if __name__ == "__main__":
	main()

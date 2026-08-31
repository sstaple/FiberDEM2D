"""Tests for the Python-facing FDEMPython API."""

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import numpy as np
import pytest

import FDEMPython as fdem


def _make_options():
	return fdem.RandomRVEOptions(
		fiber_radius=1.0,
		fiber_volume_fraction=0.3,
		n_rows=4,
		n_max_steps=300,
		n_undamped_steps=50,
	)


def test_can_construct_options():
	options = _make_options()
	assert options.fiber_radius == 1.0
	assert options.n_rows == 4
	assert not hasattr(options, "n_repetitions")


def test_output_options_default_to_false():
	options = _make_options()
	assert options.save_results is False
	assert options.save_final_positions is False
	assert options.save_final_positions_without_projections is False
	assert options.save_vf_statistics is False
	assert options.save_connection_plot is False


def test_generate_random_rve_executes_successfully():
	result = fdem.generate_random_rve(_make_options())
	assert result is not None


def test_result_contains_numpy_arrays():
	result = fdem.generate_random_rve(_make_options())
	assert isinstance(result.locations, np.ndarray)
	assert isinstance(result.radii, np.ndarray)
	assert isinstance(result.boundary, np.ndarray)


def test_result_shapes_and_consistency():
	result = fdem.generate_random_rve(_make_options())
	n = result.radii.shape[0]
	assert n > 0
	assert result.locations.shape == (n, 2)
	assert result.radii.shape == (n,)
	assert result.boundary.shape == (2,)
	assert len(result.radii) == result.locations.shape[0]
	assert np.all(result.radii > 0)
	assert result.boundary[0] > 0
	assert result.boundary[1] > 0


if __name__ == "__main__":
	sys.exit(pytest.main([__file__, "-v"]))

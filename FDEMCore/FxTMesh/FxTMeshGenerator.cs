using FDEMCore.FxTMesh.Meshing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh
{
    public static class FxTMeshGenerator
    {
        public static FEMesh GenerateFromPack(Packing pack, string? debugDirectory = null, string? debugFileName = null,
            bool debug = false,  MeshOptions? meshOptions = null, ElementConfig? elementConfig = null)
        {
            if (pack == null)
                throw new ArgumentNullException(nameof(pack));

            meshOptions ??= new MeshOptions();
            elementConfig ??= ElementConfig.Standard;

            DebugOptions? debugOptions = null;

            if (debug)
            {
                debugOptions = new DebugOptions{ Debug = true, Directory = debugDirectory ?? "debug_output", 
                    FileName = debugFileName ?? "FxTMesh"};
            }

            var triangulator = new DelaunayTriangulator();

            var triangulation = triangulator.GenerateTriangulation(pack.Boundary,pack.LFibers, debugOptions, meshOptions);

            var elementBuilder = new ElementBuilder();

            var feMesh = elementBuilder.BuildMesh(triangulation, pack.LFibers,pack.Boundary,  elementConfig, debugOptions);

            return feMesh;
        }
    }
}
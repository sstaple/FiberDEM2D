using FDEMCore.FxTMesh.Meshing;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh
{
    public static class FxTMeshGenerator
    {
        public static FEMesh GenerateFromPack(Packing pack, DebugOptions debugOptions = null,  MeshOptions meshOptions = null, FxTElementFamily elementConfig=FxTElementFamily.Type2)
        {
            if (pack == null)
                throw new ArgumentNullException(nameof(pack));

            meshOptions ??= new MeshOptions();

            var triangulator = new DelaunayTriangulator();

            var triangulation = triangulator.GenerateTriangulation(pack.Boundary,pack.LFibers, debugOptions, meshOptions);

            var elementBuilder = new MeshBuilder();

            var feMesh = elementBuilder.BuildMesh(triangulation, pack.LFibers,pack.Boundary,  elementConfig, debugOptions);

            return feMesh;
        }
    }
}
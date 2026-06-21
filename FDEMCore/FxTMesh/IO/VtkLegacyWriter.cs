
using FDEMCore.FxTMesh.Geometry;
using FDEMCore.FxTMesh.Meshing;
using FDEMCore.FxTMesh.Meshing.Elements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FDEMCore.FxTMesh.IO
{
    /// <summary>
    /// Writes VTK files for visualization in ParaView or VisIt.
    /// 
    /// Output files:
    /// 1. *_tri.vtk: Delaunay triangulation (fiber centers only)
    ///    - Points colored by node_type, fiber_id
    ///    - Cells are triangles connecting fiber centers
    /// 
    /// 2. *_mesh.vtk: Complete finite element mesh with all elements
    ///    - CELL_DATA fields:
    ///      * element_phase: 0=Matrix, 1=Fiber
    ///      * element_id: Unique element ID
    ///      * element_type: 0=Interior triangle (3-node matrix), 
    ///                      1=Fiber element (6-node curved triangle), 
    ///                      2=Matrix quad (8-node connecting fibers),
    ///                      3=Matrix triangle (6-node fiber-boundary)
    ///      * element_nodes: Number of nodes in element
    ///    - POINT_DATA fields:
    ///      * node_label: Global node index
    /// 
    /// Visualization Tips:
    /// - Color by 'element_phase' to see fiber vs matrix regions
    /// - Color by 'element_type' to distinguish element categories
    /// - Use 'Extract Surface' filter to see boundary
    /// - Use 'Glyph' filter with 'node_label' to show node numbers
    /// </summary>
    public static class VtkLegacyWriter
    {
        /// <summary>
        /// Writes an ASCII legacy VTK unstructured grid (.vtk) with triangle cells (VTK cell type 5).
        /// Adds a POINT_DATA scalar "node_type" (0=fiber center, 1=boundary point).
        /// </summary>
        public static void WriteUnstructuredGrid2D(string path, TriangulationMesh2D mesh)
        {
            if (mesh is null) throw new ArgumentNullException(nameof(mesh));

            using var sw = new StreamWriter(path + ".vtk");
            sw.WriteLine("# vtk DataFile Version 3.0");
            sw.WriteLine("FiberMeshGen output");
            sw.WriteLine("ASCII");
            sw.WriteLine("DATASET UNSTRUCTURED_GRID");

            sw.WriteLine($"POINTS {mesh.Nodes.Count} double");
            foreach (var n in mesh.Nodes)
            {
                sw.WriteLine(Form($"{n.P.X} {n.P.Y} 0.0"));
            }

            int nCells = mesh.Triangles.Count;
            int intsPerCell = 4; // 3 vertices + 1 leading count
            sw.WriteLine($"CELLS {nCells} {nCells * intsPerCell}");
            foreach (int[] node in mesh.Triangles)
            {
                sw.WriteLine($"{3} {node[0]} {node[1]} {node[2]}");
            }

            sw.WriteLine($"CELL_TYPES {nCells}");
            for (int i = 0; i < nCells; i++)
                sw.WriteLine("5"); // triangle

            sw.WriteLine($"POINT_DATA {mesh.Nodes.Count}");

            // Node type
            sw.WriteLine("SCALARS node_type int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            for (int i = 0; i < mesh.Nodes.Count; i++)
                sw.WriteLine(((int)mesh.Nodes[i].Type).ToString(CultureInfo.InvariantCulture));

            // Node labels (indices)
            sw.WriteLine("SCALARS node_label int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            for (int i = 0; i < mesh.Nodes.Count; i++)
                sw.WriteLine(i.ToString(CultureInfo.InvariantCulture));

            // Fiber IDs
            sw.WriteLine("SCALARS fiber_id int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            for (int i = 0; i < mesh.Nodes.Count; i++)
            {
                int fiberId = mesh.Nodes[i].FiberId ?? -1; // -1 for boundary nodes
                sw.WriteLine(fiberId.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Writes a finite element mesh with elements of varying types (triangles and quads).
        /// </summary>
        public static void WriteUnstructuredMesh(string path, FEMesh mesh)
        {
            if (mesh is null) throw new ArgumentNullException(nameof(mesh));

            using var sw = new StreamWriter(path + ".vtk");
            sw.WriteLine("# vtk DataFile Version 3.0");
            sw.WriteLine("FxT Mesh output");
            sw.WriteLine("ASCII");
            sw.WriteLine("DATASET UNSTRUCTURED_GRID");

            // Write points
            sw.WriteLine($"POINTS {mesh.GlobalNodes.Count} double");
            foreach (var pt in mesh.GlobalNodes)
            {
                sw.WriteLine(Form($"{pt.X} {pt.Y} 0.0"));
            }

            // Count total connectivity size
            int totalConnectivity = 0;
            foreach (var elem in mesh.Elements)
            {
                totalConnectivity += 1 + elem.NodeCount; // count + nodes
            }

            // Write cells
            sw.WriteLine($"CELLS {mesh.Elements.Count} {totalConnectivity}");
            foreach (var elem in mesh.Elements)
            {
                //sw.Write($"{elem.NodeCount}");

                int[] reorderMap = GetVTKOrder(elem);

                sw.Write($"{reorderMap.Length}");

                foreach (int localIndex in reorderMap)
                {
                    int idx = FindNodeIndex(mesh.GlobalNodes, elem.Nodes[localIndex]);
                    sw.Write($" {idx}");
                }

                sw.WriteLine();
            }

            // Write cell types
            sw.WriteLine($"CELL_TYPES {mesh.Elements.Count}");
            foreach (var elem in mesh.Elements)
            {
                sw.WriteLine(GetVTKType(elem));
            }

            // Write cell data (element phase)
            sw.WriteLine($"CELL_DATA {mesh.Elements.Count}");

            // Element phase (0 = Matrix, 1 = Fiber)
            sw.WriteLine("SCALARS element_phase int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            foreach (var elem in mesh.Elements)
            {
                sw.WriteLine(((int)elem.Phase).ToString(CultureInfo.InvariantCulture));
            }

            // Element IDs
            sw.WriteLine("SCALARS element_id int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            foreach (var elem in mesh.Elements)
            {
                sw.WriteLine(elem.Id.ToString(CultureInfo.InvariantCulture));
            }

            // Element node count
            sw.WriteLine("SCALARS element_nodes int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            foreach (var elem in mesh.Elements)
            {
                sw.WriteLine(elem.NodeCount.ToString(CultureInfo.InvariantCulture));
            }

            // Write point data
            sw.WriteLine($"POINT_DATA {mesh.GlobalNodes.Count}");

            // Node labels (indices)
            sw.WriteLine("SCALARS node_label int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            for (int i = 0; i < mesh.GlobalNodes.Count; i++)
            {
                sw.WriteLine(i.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static int FindNodeIndex(IReadOnlyList<Point2D> globalNodes, Point2D node)
        {
            for (int i = 0; i < globalNodes.Count; i++)
            {
                var dx = globalNodes[i].X - node.X;
                var dy = globalNodes[i].Y - node.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < 1e-10)
                    return i;
            }
            return -1; // not found
        }

        private static int GetVTKType(Element elem)
        {
            return elem.ElementName switch
            {
                "2DT3" => 5,
                "2DT6" => 22,
                "2DT6.4" => 22,
                "2DT9" => 69,   // fallback/check later if ParaView supports this as expected

                "2DQ4" => 9,
                "2DQ6" => 9,    // fallback visualization only
                "2DQ8" => 23,
                "2DQ8.9" => 23,
                "2DQ9" => 28,

                "2DQ12" => 70,  // fallback/check later
                "2DQ16" => 70,  // fallback/check later

                _ => throw new NotSupportedException($"No VTK type for {elem.ElementName}")
            };
        }

        private static int[] GetVTKOrder(Element elem)
        {
            return elem.ElementName switch
            {
                "2DT3" => new[] { 0, 1, 2 },

                "2DT6" or "2DT6.4" => new[] { 0, 2, 4, 1, 3, 5 },

                "2DQ8" or "2DQ8.9" => new[] { 0, 2, 4, 6, 1, 3, 5, 7 },

                "2DQ9" => new[] { 0, 2, 4, 6, 1, 3, 5, 7, 8 },

                _ => Enumerable.Range(0, elem.NodeCount).ToArray()
            };
        }

        private static string Form(string s) => s.Replace(",", "."); // defensive if culture changes
    }
}

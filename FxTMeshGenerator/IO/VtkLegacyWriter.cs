
using System;
using System.Globalization;
using System.IO;
using FxTMeshGenerator.Geometry;
using FxTMeshGenerator.Meshing;
using FxTMeshGenerator.Meshing.Elements;

namespace FxTMeshGenerator.IO
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
    ///                      2=Matrix quad (8-node connecting fibers)
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

            using var sw = new StreamWriter(path);
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

            using var sw = new StreamWriter(path);
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
                sw.Write($"{elem.NodeCount}");

                // Special handling for 6-node fiber triangles: reorder nodes to match VTK_QUADRATIC_TRIANGLE
                if (elem is TriangleElement tri && tri.NodeCount == 6 && tri.Phase == ElementPhase.Fiber)
                {
                    // Our node order: [0]=center, [1]=mid01, [2]=corner, [3]=arc, [4]=corner, [5]=mid45
                    // VTK expects: [0,1,2]=corners, [3]=mid01, [4]=mid12, [5]=mid20
                    // Remapping: [2,4,0,3,5,1] -> [0,1,2,3,4,5]
                    int[] reorderMap = new int[] { 2, 4, 0, 3, 5, 1 };
                    for (int i = 0; i < 6; i++)
                    {
                        int idx = FindNodeIndex(mesh.GlobalNodes, elem.Nodes[reorderMap[i]]);
                        sw.Write($" {idx}");
                    }
                }
                else
                {
                    // Standard order for all other elements
                    for (int i = 0; i < elem.NodeCount; i++)
                    {
                        int idx = FindNodeIndex(mesh.GlobalNodes, elem.Nodes[i]);
                        sw.Write($" {idx}");
                    }
                }
                sw.WriteLine();
            }

            // Write cell types
            sw.WriteLine($"CELL_TYPES {mesh.Elements.Count}");
            foreach (var elem in mesh.Elements)
            {
                // VTK cell types: Triangle=5, Quad=9
                int vtkType = elem is TriangleElement ? GetTriangleVTKType(elem.NodeCount) 
                                                       : GetQuadVTKType(elem.NodeCount);
                sw.WriteLine(vtkType);
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

            // Element type (0 = interior triangle, 1 = fiber triangle, 2 = matrix quad)
            sw.WriteLine("SCALARS element_type int 1");
            sw.WriteLine("LOOKUP_TABLE default");
            foreach (var elem in mesh.Elements)
            {
                int elemType = elem switch
                {
                    TriangleElement tri when tri.NodeCount == 3 && tri.Phase == ElementPhase.Matrix => 0,
                    TriangleElement tri when tri.Phase == ElementPhase.Fiber => 1,
                    QuadElement quad when quad.Phase == ElementPhase.Matrix => 2,
                    _ => -1
                };
                sw.WriteLine(elemType.ToString(CultureInfo.InvariantCulture));
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

        private static int GetTriangleVTKType(int nodeCount) => nodeCount switch
        {
            3 => 5,   // VTK_TRIANGLE
            6 => 22,  // VTK_QUADRATIC_TRIANGLE
            _ => 5
        };

        private static int GetQuadVTKType(int nodeCount) => nodeCount switch
        {
            4 => 9,   // VTK_QUAD
            8 => 23,  // VTK_QUADRATIC_QUAD
            9 => 28,  // VTK_BIQUADRATIC_QUAD
            _ => 9
        };

        private static string Form(string s) => s.Replace(",", "."); // defensive if culture changes
    }
}

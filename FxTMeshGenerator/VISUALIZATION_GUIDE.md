# FxTMeshGenerator Visualization Guide

## Output Files

The mesh generator creates two VTK files for visualization in ParaView:

### 1. `*_tri.vtk` - Delaunay Triangulation
Shows the raw triangulation of fiber centers before element building.

**Point Data:**
- `node_type`: Type of node
  - `0` = FiberCenter
  - `1` = ProjectedFiber  
  - `2` = BoundaryPoint
  - `3` = BoundaryCorner
  - `4` = ProjectedBoundary
- `node_label`: Global node index
- `fiber_id`: Fiber ID (-1 for boundary nodes)

**Cell Data:**
- All cells are triangles (VTK type 5)

### 2. `*_mesh.vtk` - Complete Finite Element Mesh
Shows the final mesh with all interior and fiber/matrix elements.

**Cell Data:**
- `element_phase`: Material phase
  - `0` = Matrix
  - `1` = Fiber
- `element_id`: Unique element identifier
- `element_type`: Element category
  - `0` = Interior triangle (3-node matrix element at fiber surface)
  - `1` = Fiber element (6-node curved triangle on fiber surface)
  - `2` = Matrix quad (8-node quad connecting two fiber elements)
- `element_nodes`: Number of nodes in the element

**Point Data:**
- `node_label`: Global node index

## Element Types Explained

### Interior Triangle Elements (Type 0)
- **3 nodes** (linear triangle)
- **Phase:** Matrix
- **Location:** At the surface of fibers, inside each Delaunay triangle
- **Purpose:** Represents matrix material in the interior of the RVE
- Nodes are placed on fiber surfaces at calculated bisector angles

### Fiber Elements (Type 1)
- **6 nodes** (quadratic triangle)
- **Phase:** Fiber
- **Location:** Wraps around fiber surface between two adjacent triangles
- **Purpose:** Represents the fiber material
- Node ordering:
  - Node 0: Fiber center
  - Nodes 1-2: Midpoint and edge node
  - Node 3: Middle node on fiber surface (curved)
  - Nodes 4-5: Edge node and midpoint

### Matrix Quad Elements (Type 2)
- **8 nodes** (quadratic quadrilateral)
- **Phase:** Matrix
- **Location:** Connects two fiber elements along a shared edge
- **Purpose:** Represents matrix material between adjacent fibers
- Bridges the gap between fiber surfaces

## ParaView Visualization Tips

### Basic Visualization
1. Open the `*_mesh.vtk` file in ParaView
2. Click "Apply" in the Properties panel
3. In the toolbar, change coloring from "Solid Color" to a field:
   - **Color by `element_phase`**: Shows fiber (red) vs matrix (blue) regions
   - **Color by `element_type`**: Distinguishes the three element categories

### Advanced Visualization

#### Show Element IDs
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Cell Centers`
3. In the pipeline, select "CellCenters1"
4. Add filter: `Filters` → `Alphabetical` → `Point Labels`
5. In Properties, set "Label Mode" to "element_id"
6. Adjust font size as needed

#### Show Node Numbers
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Point Labels`
3. In Properties, set "Label Mode" to "node_label"
4. Adjust font size and visibility

#### Extract Boundary
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Extract Surface`
3. Color by `element_phase` or `element_type`

#### Show Only Fiber Elements
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Threshold`
3. Set "Scalars" to "element_phase"
4. Set range to [1, 1]
5. Click "Apply"

#### Show Only Matrix Elements
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Threshold`
3. Set "Scalars" to "element_phase"
4. Set range to [0, 0]
5. Click "Apply"

#### Show Specific Element Type
1. Select the mesh
2. Add filter: `Filters` → `Alphabetical` → `Threshold`
3. Set "Scalars" to "element_type"
4. Set range to desired type (0, 1, or 2)
5. Click "Apply"

### Color Maps
For best results with discrete fields:
1. Click the color bar legend
2. In "Color Map Editor", change "Color Space" to "Step"
3. For `element_type`, use 3 discrete colors
4. For `element_phase`, use 2 discrete colors

## Debugging Tips

### Check Element Counts
In ParaView's Information panel:
- Look at "Number of Cells" for total elements
- Use Threshold filter to count each element type

### Verify Connectivity
1. Use "Surface With Edges" representation
2. Look for gaps or overlaps
3. Check that fiber elements properly connect to matrix quads

### Check for Zero-Thickness Elements
1. Add filter: `Filters` → `Alphabetical` → `Cell Size`
2. Color by "Area" or "Volume"
3. Look for suspiciously small values (< 1e-5)

## Common Issues

### Elements Not Showing
- Check that BuildInteriorFiberMatrixElements was called
- Verify that adjacent triangles were found
- Check console for any error messages

### Overlapping Elements
- Review overlap detection settings
- Check `ChangeMiddleNodeIfFibersAreTooClose` threshold (ratio >= 0.90)
- Verify fiber radius values are reasonable

### Missing Fiber/Matrix Connections
- Ensure shared edges are being detected properly
- Check that CCW/CW ordering is correct
- Verify that zero-thickness check isn't too aggressive

## Output Statistics

After running, check the VTK file headers for:
- Total POINTS (global nodes)
- Total CELLS (elements)
- CELL_DATA count should match number of elements
- POINT_DATA count should match number of nodes

Expected element distribution for typical RVE:
- **Interior triangles**: N_triangles (one per Delaunay triangle)
- **Fiber elements**: ~2 × N_edges (two per shared edge)
- **Matrix quads**: ~N_edges (one per shared edge)

Where N_triangles ≈ 2 × N_fibers and N_edges ≈ 3 × N_fibers for typical packings.

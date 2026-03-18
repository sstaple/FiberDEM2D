clear; clc; close all
packfile = "50_pack.csv";
dir = "C:\Users\eric_\OneDrive - UMass Lowell\FFEM_Matlab_Source\PackFiles\";

FiberData  = struct('ModelID', '2', 'E', 25000, 'nu', 0.26, 'E1', 276000, 'E2', 14000, 'nu12', 0.26, 'nu23', 0.26, 'G12', 20000, 'Strength', 100, 'GIC', 1, 'PS_PE', 2, 'NumTestPoints', 1);
TriMatrixData  = struct('ModelID', '3a', 'E', 4080, 'nu', 0.36, 'E1', 276000, 'E2', 14000, 'nu12', 0.26, 'nu23', 0.36, 'G12', 20000, 'Strength', 100, 'GIC', 1, 'PS_PE', 2, 'NumTestPoints', 1);
QuadMatrixData = struct('ModelID', '3a', 'E', 4080, 'nu', 0.36, 'E1', 276000, 'E2', 14000, 'nu12', 0.26, 'nu23', 0.36, 'G12', 20000, 'Strength', 100, 'GIC', 1, 'PS_PE', 2, 'NumTestPoints', 1);
MaterialInputs = struct('FiberData', FiberData, 'TriMatrixData', TriMatrixData, 'QuadMatrixData', QuadMatrixData);
Mesh = FE_Mesh.GenerateMesh("2D_Reduced", MaterialInputs, packfile, dir);
assembly = Mesh.CreateAssemblyFromMesh;

% Plotting:
Mesh.PlotFibers(30, true)
FE_Mesh.PlotTriadConnectivity(Mesh.ListOfFiberCenters, Mesh.FiberConnectivity);
Mesh.PlotBoundary;
Mesh.PlotElements('k-*', 'b-*', 0.5, 2)
%Mesh.PlotNodePairs;
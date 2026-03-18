function [assembly, Inputs] = GenerateMesh(packFile, Inputs)
   
   % Type of assembly
   if(Inputs.AssemblyData.NumberOfDimensions == 2 && ~Inputs.AssemblyData.isReduced)
       type = "2D";
   elseif(Inputs.AssemblyData.NumberOfDimensions == 2 && Inputs.AssemblyData.isReduced)
       type = "2D_Reduced";
   elseif(Inputs.AssemblyData.NumberOfDimensions == 3 && ~Inputs.AssemblyData.isReduced)
       type = "2p5D";
   elseif(Inputs.AssemblyData.NumberOfDimensions == 3 && Inputs.AssemblyData.isReduced)
       type = "2p5D_Reduced";
   end

   % Generate mesh and create assembly:
   Mesh = FE_Mesh.GenerateMesh(type, Inputs, packFile);
   assembly = Mesh.CreateAssemblyFromMesh;

   Inputs = InitializePinnedBCs(Mesh, Inputs);

   % Plotting?
   if(Inputs.Plotting.Mesh)
       Mesh.PlotFibers(30, false)
       %FE_Mesh.PlotTriadConnectivity(Mesh.ListOfFiberCenters, Mesh.FiberConnectivity);
       Mesh.PlotBoundary;
       Mesh.PlotElements('k-*', 'b-*', 0.5, 2)
       Mesh.PlotGlobalNodeNumbers;
       %Mesh.PlotNodePairs;
   end
end

function Inputs = InitializePinnedBCs(Mesh, Inputs)
     Inputs.BCData.BCs(2).NDOFPNode = Mesh.NDOFPNode;
     Inputs.BCData.BCs(2).Nodes = Mesh.PinnedNode;
     Inputs.BCData.BCs(2).AppliedDOF = 'all';
end
classdef (Abstract) FE_Mesh < handle
    % Mesh for FxT
    
    %% Properties
    properties (Abstract, Constant)
        Type
        NumberOfDimensions
        NDOFPNode
        isReducedOrder
    end

    properties
        NumberOfFibers
        NumberOfTriads
        ListOfFibers
        ListOfTriads
        ArrayOfTriadPairs
        ListOfFiberCenters
        FiberConnectivity
        NumberOfNodes
        NodalLocations
        NodePairArray
        TopEdge
        RightEdge
        NumberOfElements
        ListOfElements
        RVEBoundary
        PinnedNode
    end

    %% Abstract Static Methods
    methods (Abstract, Static)
        DetermineInteriorTriangleNodes(TriangleMidpoints);
        BuildInteriorTriangleElement(ModelID, NodalLocations);
    end
    
    %% Methods
    methods
        % Create assembly from mesh:
        function assembly = CreateAssemblyFromMesh(obj)
            switch obj.Type
                case "2D"
                    MaxNodesInElement = 8;
                    assembly = FE_Assembly_Periodic_2D(obj.NumberOfElements, obj.NumberOfNodes);
                case "2D_Reduced"
                    MaxNodesInElement = 6;
                    assembly = FE_Assembly_Periodic_2D(obj.NumberOfElements, obj.NumberOfNodes);
                case "2p5D"
                    MaxNodesInElement = 9;
                    obj.AssignGhostNodeAndLocation;
                    assembly = FE_Assembly_Periodic_2p5D(obj.NumberOfElements, obj.NumberOfNodes);
                    assembly.GhostNode = obj.GhostNode;
                case "2p5D_Reduced"
                    MaxNodesInElement = 7;
                    obj.AssignGhostNodeAndLocation;
                    assembly = FE_Assembly_Periodic_2p5D(obj.NumberOfElements, obj.NumberOfNodes);
                    assembly.GhostNode = obj.GhostNode;
            end
            assembly.RVELength = obj.RVEBoundary;
            assembly.NumberOfNodes = obj.NumberOfNodes;
            assembly.NodalLocations = obj.NodalLocations;
            assembly.ListOfElements = obj.ListOfElements;
            assembly.NodePairArray = obj.NodePairArray;
            assembly.AssembleConnectivity(MaxNodesInElement);
            assembly.TopEdge = obj.TopEdge;
            assembly.RightEdge = obj.RightEdge;
            assembly.AssembleGlobalM;
        end

        % Delete unwanted triads after triangulation:
        function DeleteUnwantedFibersAndTriads(obj)
            NumTriadsBeforeDeletion = length(obj.FiberConnectivity(:, 1));
            TriadsToKeep = ones(NumTriadsBeforeDeletion, 1);
            % Determine which triads to delete:
            for i = 1 : NumTriadsBeforeDeletion
                TriadsToKeep(i) = obj.isTriadBeingDeleted(obj.FiberConnectivity(i, :));
            end
            TriadsToKeep = logical(TriadsToKeep);
            obj.FiberConnectivity = obj.FiberConnectivity(TriadsToKeep, :);
            NumTriadsAfterDeletion = length(obj.FiberConnectivity(:, 1));
            % Determine which fibers to delete:
            FibersToKeep = zeros(obj.NumberOfFibers, 1);
            for i = 1 : NumTriadsAfterDeletion
                for j = 1 : 3
                    FibersToKeep(obj.FiberConnectivity(i, j)) = 1;
                end
            end
            FibersToKeep = logical(FibersToKeep);
            obj.ListOfFibers = obj.ListOfFibers(FibersToKeep);
            obj.ListOfFiberCenters = obj.ListOfFiberCenters(FibersToKeep, :);
            obj.NumberOfTriads = NumTriadsAfterDeletion;
            obj.NumberOfFibers = length(obj.ListOfFibers);
        end

        % Renumber fibers after deletion:
        function ReNumberFiberConnectivityAfterDeletion(obj, ListOfOldFibers)
            NewFiberNumbering = zeros(obj.NumberOfTriads, 3);
            NewFiberNumber = 1;
            for i = 1 : obj.NumberOfTriads
                for j = 1 : 3
                    [NewFiberFlag, m, n] = obj.isThisANewFiberNumber(i, obj.FiberConnectivity(i, j));
                    if(NewFiberFlag)
                        NewFiberNumbering(i, j) = NewFiberNumber;
                        obj.ListOfFibers{NewFiberNumber} = ListOfOldFibers{obj.FiberConnectivity(i, j)};
                        obj.ListOfFibers{NewFiberNumber}.Number = NewFiberNumber;
                        obj.ListOfFiberCenters(NewFiberNumber, :) = ListOfOldFibers{obj.FiberConnectivity(i, j)}.Center;
                        NewFiberNumber = NewFiberNumber + 1;
                    else
                        NewFiberNumbering(i, j) = NewFiberNumbering(m, n);
                    end
                end
            end
            obj.DetermineFiberPairs;
            obj.FiberConnectivity = NewFiberNumbering;
        end

        % Update the fiber projection pair numbers:
        function DetermineFiberPairs(obj)
            PositionsToCheck = zeros(obj.NumberOfFibers, 2);
            ProjectionChecks = [2, 3, 4, 6];
            for i = 1 : obj.NumberOfFibers
                for j = 1 : length(ProjectionChecks)
                    Offset = Quadrant.GetQuadrantOffset(ProjectionChecks(j));
                    PositionsToCheck(:, 1) = obj.ListOfFiberCenters(:, 1) + Offset(1) * obj.RVEBoundary(1);
                    PositionsToCheck(:, 2) = obj.ListOfFiberCenters(:, 2) + Offset(2) * obj.RVEBoundary(2);
                    for k = 1 : obj.NumberOfFibers
                        OverlapFlag = MathMethods.OverlapCheck(obj.ListOfFiberCenters(i, :), PositionsToCheck(k, :));
                        if(OverlapFlag)
                            obj.ListOfFibers{i}.NumberOfFiberPairs = obj.ListOfFibers{i}.NumberOfFiberPairs + 1;
                            obj.ListOfFibers{k}.NumberOfFiberPairs = obj.ListOfFibers{k}.NumberOfFiberPairs + 1;
                            obj.ListOfFibers{i}.FiberPairs(obj.ListOfFibers{i}.NumberOfFiberPairs) = k;
                            obj.ListOfFibers{k}.FiberPairs(obj.ListOfFibers{k}.NumberOfFiberPairs) = i;
                        end
                    end
                end
                obj.ListOfFibers{i}.FiberPairs(obj.ListOfFibers{i}.FiberPairs == 0) = [];
            end
        end

        % Create a list of triads:
        function CreateListOfTriads(obj)
            obj.ListOfTriads = cell(1, obj.NumberOfTriads);
            for i = 1 : obj.NumberOfTriads
                FN = zeros(1, 3);
                for j = 1 : 3
                    FN(j) = obj.FiberConnectivity(i, j);
                end
                Fibers = [obj.ListOfFibers(FN(1)), obj.ListOfFibers(FN(2)), obj.ListOfFibers(FN(3))];
                obj.ListOfTriads{i} = Triad(i, Fibers);
            end
        end

        % Create an array of all triad pairs:
        function CreateArrayOfAllTriadPairs(obj)
            AllTriadPairs = zeros(obj.NumberOfTriads*3, 2);
            PairLength = 0;
            for i = 1 : obj.NumberOfTriads
                CurrentTriadPairs = obj.ListOfTriads{i}.CreateArrayOfConnectedTriads;
                AllTriadPairs(PairLength+1 : PairLength + length(CurrentTriadPairs(:, 1)), :) = CurrentTriadPairs;
                PairLength = PairLength + length(CurrentTriadPairs(:, 1));
            end
            AllTriadPairs(all(AllTriadPairs == 0, 2), :) = [];
            SortedPairs = sort(AllTriadPairs, 2);
            [~, idx] = unique(SortedPairs, 'rows', 'stable'); % Stable maintains original order
            UniquePairs = SortedPairs(idx, :);
            obj.ArrayOfTriadPairs = UniquePairs;
        end

        % Determine all boundary triads:
        function FindWhichTriadsAreConnectedAndWhichAreOnBoundary(obj)
            for i = 1 : obj.NumberOfTriads
                % Check which triads share at least 2 fibers with current triad:
                for j = 1 : obj.NumberOfTriads
                    FiberNumbers = obj.ListOfTriads{j}.GetFiberNumbers;
                    % Dont check current triad against current triad
                    if(i == j); continue; end
                    obj.ListOfTriads{i}.AreTriadsConnectedAndIsTriadBoundary(FiberNumbers, j)
                end
                obj.ListOfTriads{i}.CheckIfTriadIsBoundaryAfterFindingEdgePairs;
            end
        end

        % Find overlapping triads
        function FindOverlappingTriads(obj)
            Counter = 0;
            while(true)
                Counter = Counter + 1;
                OverlapFlag = false;
                for i = 1 : obj.NumberOfTriads
                    obj.ListOfTriads{i}.DetermineIfFibersOverlapTriad;
                    if(any(obj.ListOfTriads{i}.FibersWhichOverlapTriad))
                        OverlapFlag = true;
                        obj.Retriangulate(obj.ListOfTriads{i});
                    end
                end
                if(Counter >= 5)
                    OverlapFlag = false;
                    warning('Possible Fiber/Triad Overlap');
                end
                if(~OverlapFlag)
                    break
                end
            end
        end

        % Retriangulate 
        function Retriangulate(obj, OverlappingTriad)
            NonOverlapIdxs = find(OverlappingTriad.FibersWhichOverlapTriad == 0);
            OverlapFiberIdx = find(OverlappingTriad.FibersWhichOverlapTriad);
            NonOverlappingEdge = [OverlappingTriad.Fibers{NonOverlapIdxs(1)}.Number, OverlappingTriad.Fibers{NonOverlapIdxs(2)}.Number];
            % Find other connecting triad
            for j = 1 : obj.NumberOfTriads
                if(OverlappingTriad.Number == j); continue; end
                OtherTriad = obj.ListOfTriads{j};
                OtherTriadFibers = [OtherTriad.Fibers{1}.Number, OtherTriad.Fibers{2}.Number, OtherTriad.Fibers{3}.Number];
                FiberPairInTriad = OverlappingTriad.isFiberPairInTriad(NonOverlappingEdge, OtherTriadFibers);
                if(FiberPairInTriad)
                    OverlappingTriadFibers = [OverlappingTriad.Fibers{1}.Number, OverlappingTriad.Fibers{2}.Number, OverlappingTriad.Fibers{3}.Number];
                    FiberToSwapInOtherTriad   = setdiff(OtherTriadFibers, OverlappingTriadFibers, 'stable');
                    OtherTriadFiberIdx = find(OtherTriadFibers == FiberToSwapInOtherTriad);
                    if(OverlapFiberIdx == 3)
                        obj.ListOfTriads{OverlappingTriad.Number}.Fibers{1} = OtherTriad.Fibers{OtherTriadFiberIdx};
                    else
                        obj.ListOfTriads{OverlappingTriad.Number}.Fibers{OverlapFiberIdx + 1} = OtherTriad.Fibers{OtherTriadFiberIdx};
                    end
                    if(OtherTriadFiberIdx == 3)
                        obj.ListOfTriads{OtherTriad.Number}.Fibers{1} = OverlappingTriad.Fibers{OverlapFiberIdx};
                    else
                        obj.ListOfTriads{OtherTriad.Number}.Fibers{OtherTriadFiberIdx + 1} = OverlappingTriad.Fibers{OverlapFiberIdx};
                    end
                    obj.ListOfTriads{OverlappingTriad.Number}.InitializeEdges;
                    obj.ListOfTriads{OtherTriad.Number}.InitializeEdges;
                    obj.FiberConnectivity(OverlappingTriad.Number, :) = [obj.ListOfTriads{OverlappingTriad.Number}.Fibers{1}.Number, obj.ListOfTriads{OverlappingTriad.Number}.Fibers{2}.Number, obj.ListOfTriads{OverlappingTriad.Number}.Fibers{3}.Number];
                    obj.FiberConnectivity(OtherTriad.Number, :) = [obj.ListOfTriads{OtherTriad.Number}.Fibers{1}.Number, obj.ListOfTriads{OtherTriad.Number}.Fibers{2}.Number, obj.ListOfTriads{OtherTriad.Number}.Fibers{3}.Number];
                    return
                end
            end
        end

        % Build interior triangle elements:
        function BuildAllInteriorTriangleElements(obj, MaterialInputs)
             obj.CalculateCurrentNumberOfElements;
             for i = 1 : obj.NumberOfTriads
                 obj.IncreaseElementCount;
                 FiberMidPoints = obj.ListOfTriads{i}.CalculateFiberMidpoints;
                 TrianlgeNodes = obj.DetermineInteriorTriangleNodes(FiberMidPoints);
                 obj.ListOfElements{obj.NumberOfElements} = obj.BuildInteriorTriangleElement(MaterialInputs.TriMatrixData, TrianlgeNodes);
                 obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
             end
        end

        % Build Interior Fiber/Matrix Elements:
        function BuildInteriorFiberMatrixElements(obj, MaterialInputs)
            for i = 1 : length(obj.ArrayOfTriadPairs(:, 1))
                % Find which edge is shared between triads (Return the corresponding row numbers from each triad):
                Triad1 = obj.ListOfTriads{obj.ArrayOfTriadPairs(i, 1)};
                Triad2 = obj.ListOfTriads{obj.ArrayOfTriadPairs(i, 2)};
                Triad1Edges = Triad1.Edges;
                Triad2Edges = Triad2.Edges;
                CommonIdx = ismember(sort(Triad1Edges, 2), sort(Triad2Edges, 2), 'rows');
                SharedEdge = Triad1Edges((CommonIdx == 1), :);
                SharedFiber1 = obj.ListOfFibers{SharedEdge(1)};
                SharedFiber2 = obj.ListOfFibers{SharedEdge(2)};
                % Get interior triangle nodes associated with each triad:
                E1 = obj.ListOfElements{obj.ArrayOfTriadPairs(i, 1)};
                E2 = obj.ListOfElements{obj.ArrayOfTriadPairs(i, 2)}; 
                Triangle1Nodes = VectorToTable(E1.NodalLocations, E1.NumberOfNodes, obj.NumberOfDimensions);
                Triangle2Nodes = VectorToTable(E2.NodalLocations, E2.NumberOfNodes, obj.NumberOfDimensions);
                % Relate interior triangle nodes to fiber nodes:
                Fiber1Node_Triad1 = obj.FindInteriorTriangleNodeClosestToFiber(SharedFiber1.Center, Triangle1Nodes, obj.NumberOfDimensions);
                Fiber1Node_Triad2 = obj.FindInteriorTriangleNodeClosestToFiber(SharedFiber1.Center, Triangle2Nodes, obj.NumberOfDimensions);
                Fiber2Node_Triad1 = obj.FindInteriorTriangleNodeClosestToFiber(SharedFiber2.Center, Triangle1Nodes, obj.NumberOfDimensions);
                Fiber2Node_Triad2 = obj.FindInteriorTriangleNodeClosestToFiber(SharedFiber2.Center, Triangle2Nodes, obj.NumberOfDimensions);
                % Determine if SharedEdge is CCW or CW in triad order (Only need to check for triad 1):
                isEdgeCCW = Triad1.CheckIfSharedEdgeIsCCWOrder(SharedEdge);
                % Correctly order nodal locations in fiber and matrix elements:
                Fiber1Nodes = obj.DetermineFiberNodeOrder(SharedFiber1.Center, Fiber1Node_Triad1, Fiber1Node_Triad2, SharedFiber1.Radius, isEdgeCCW);
                Fiber2Nodes = obj.DetermineFiberNodeOrder(SharedFiber2.Center, Fiber2Node_Triad1, Fiber2Node_Triad2, SharedFiber2.Radius, ~isEdgeCCW);
                % Check if this both triangles had their interior element shifted due to potential overlap 
                % If so, just continue so an element of 0 thickness isn't inserted
                Fiber1Check = Fiber1Nodes(4, :) - Fiber1Nodes(3, :);
                if(sum(abs(Fiber1Check)) < 1e-5)
                    continue; 
                end
                % (Check distance between fibers. If they are too close, align nodes to be in line with one another. This prevents bad distortions)
                [Fiber1Nodes, Fiber2Nodes] = obj.ChangeMiddleNodeIfFibersAreTooClose(Fiber1Nodes, Fiber2Nodes, SharedFiber1.Radius, SharedFiber2.Radius, obj.isReducedOrder, obj.NumberOfDimensions);
                MatrixNodes = obj.DetermineInteriorMatrixNodeOrder(Fiber1Nodes, Fiber2Nodes, isEdgeCCW);
                % Build fiber/matrix elements:
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, Fiber1Nodes);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                SharedFiber1.AddElementNumberToFiber(obj.NumberOfElements);
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, Fiber2Nodes);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                SharedFiber2.AddElementNumberToFiber(obj.NumberOfElements);
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.QuadMatrixData, MatrixNodes, MaterialInputs.AssemblyData.isQuadNineNoded);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
            end
        end

        % Build boundary fiber/matrix elements:
        function BuildBoundaryFiberMatrixElements(obj, MaterialInputs)
            OGProjDir = obj.CreateTableOfOriginalAndProjectedFibersOnBoundary;
            for i = 1 : length(OGProjDir(:, 1))
                dir = OGProjDir(i, 5);
                OGTriadNumber   = obj.FindTriadNumberFromTwoFibers(OGProjDir(i, 1), OGProjDir(i, 2));
                ProjTriadNumber = obj.FindTriadNumberFromTwoFibers(OGProjDir(i, 3), OGProjDir(i, 4));
                OGFN1 = OGProjDir(i, 1); 
                OGFN2 = OGProjDir(i, 2); 
                ProjFN1 = OGProjDir(i, 3); 
                ProjFN2 = OGProjDir(i, 4);
                Fiber1Node1 = obj.FindInteriorTriangleNodeConnectedToFiber(OGTriadNumber,   OGFN1, false, dir);
                Fiber1Node2 = obj.FindInteriorTriangleNodeConnectedToFiber(ProjTriadNumber, ProjFN1, true,  dir);
                Fiber2Node1 = obj.FindInteriorTriangleNodeConnectedToFiber(ProjTriadNumber, ProjFN2, true,  dir);
                Fiber2Node2 = obj.FindInteriorTriangleNodeConnectedToFiber(OGTriadNumber,   OGFN2, false, dir);
                % Correctly order nodal locations in fiber and matrix elements:
                Fiber1Nodes = obj.DetermineFiberNodeOrder(obj.ListOfFibers{OGFN1}.Center, Fiber1Node1, Fiber1Node2, obj.ListOfFibers{OGFN1}.Radius, false);
                Fiber2Nodes = obj.DetermineFiberNodeOrder(obj.ListOfFibers{OGFN2}.Center, Fiber2Node1, Fiber2Node2, obj.ListOfFibers{OGFN2}.Radius, false);
                % Double check that nodes are order CCW (Very rarely they can be CW)
                Fiber1Nodes = obj.MakeSureFiberNodesAreCCW(Fiber1Nodes, obj.ListOfFibers{OGFN1}.Radius);
                Fiber2Nodes = obj.MakeSureFiberNodesAreCCW(Fiber2Nodes, obj.ListOfFibers{OGFN2}.Radius);
                % (Check distance between fibers. If they are too close, align nodes to be in line with one another. This prevents bad distortions)
                [Fiber1Nodes, Fiber2Nodes] = obj.ChangeMiddleNodeIfFibersAreTooClose(Fiber1Nodes, Fiber2Nodes, obj.ListOfFibers{OGFN1}.Radius, obj.ListOfFibers{OGFN2}.Radius, obj.isReducedOrder, obj.NumberOfDimensions);
                if(dir == 1 || dir == 4)
                    MatrixNodes = obj.DetermineInteriorMatrixNodeOrder(Fiber1Nodes, Fiber2Nodes, true);
                elseif(dir == 2 || dir == 3)
                    MatrixNodes = obj.DetermineInteriorMatrixNodeOrder(Fiber1Nodes, Fiber2Nodes, false);
                end
                % Build fiber/matrix elements:
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, Fiber1Nodes);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                obj.ListOfFibers{OGFN1}.AddElementNumberToFiber(obj.NumberOfElements);
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, Fiber2Nodes);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                obj.ListOfFibers{OGFN2}.AddElementNumberToFiber(obj.NumberOfElements);
                % Build matrix element:
                obj.IncreaseElementCount;
                obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.QuadMatrixData, MatrixNodes, MaterialInputs.AssemblyData.isQuadNineNoded);
                obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
            end
        end

        % Determine which origninal fibers are edge fibers:
        function DetermineOriginalFibersAlongEdge(obj)
            for i = 1 : obj.NumberOfFibers
                CurrentFiber = obj.ListOfFibers{i};
                CurrentFiber.CheckIfFiberHasPairs;
                if(~CurrentFiber.doesFiberHavePairs); continue; end
                if(~isempty(CurrentFiber.OGEdge)); continue; end
                CurrentFiber.InitializeCornerProjections;
                for j = 1 : length(CurrentFiber.FiberPairs)
                    PairCoordinates = obj.ListOfFiberCenters(CurrentFiber.FiberPairs(j), :);
                    RightProjCheck    = [CurrentFiber.Center(1) + obj.RVEBoundary(1), CurrentFiber.Center(2)];
                    TopProjCheck      = [CurrentFiber.Center(1), CurrentFiber.Center(2) + obj.RVEBoundary(2)];
                    TopRightProjCheck = [CurrentFiber.Center(1) + obj.RVEBoundary(1), CurrentFiber.Center(2) + obj.RVEBoundary(2)];
                    TopLeftProjCheck  = [CurrentFiber.Center(1) - obj.RVEBoundary(1), CurrentFiber.Center(2) + obj.RVEBoundary(2)];
                    RightOverlap      = MathMethods.OverlapCheck(RightProjCheck, PairCoordinates);
                    TopOverlap        = MathMethods.OverlapCheck(TopProjCheck, PairCoordinates);
                    TopRightOverlap   = MathMethods.OverlapCheck(TopRightProjCheck, PairCoordinates);
                    TopLeftOverlap    = MathMethods.OverlapCheck(TopLeftProjCheck, PairCoordinates);
                    if(RightOverlap)
                        CurrentFiber.UpdateRightEdge;
                    elseif(TopOverlap)
                        CurrentFiber.UpdateTopEdge;
                    else
                        CurrentFiber.UpdateNoEdge;
                    end
                    if(TopRightOverlap)
                        CurrentFiber.hasTopRightProjection = true;
                        CurrentFiber.isFiberPairATopRightProjection(j) = true;
                    end
                    if(TopLeftOverlap)
                        CurrentFiber.hasTopLeftProjection = true;
                        CurrentFiber.isFiberPairATopLeftProjection(j) = true;
                    end
                end
            end
        end

        % Create table of Original and Projected Fibers along Boundary:
        function OGProjDir = CreateTableOfOriginalAndProjectedFibersOnBoundary(obj)
            OGProjDir = zeros(obj.NumberOfTriads, 5);
            PairCount = 0;
            for i = 1 : obj.NumberOfTriads
                [OGProjDir, PairCount] = obj.ListOfTriads{i}.FindEdgeFiberPairs(OGProjDir, PairCount);
                [OGProjDir, PairCount] = obj.ListOfTriads{i}.FindCornerFiberPairs(OGProjDir, PairCount);
            end
            OGProjDir(all(OGProjDir == 0, 2), :) = [];
            OGProjDir = obj.OrganizeOGProjDirTable(OGProjDir);
        end

        % Organize OGProjTable:
        function OGProjDir = OrganizeOGProjDirTable(obj, OGProjDir)
            % Order OGProjDir table
            % Fibers on left:   Bot-most   OG col = 1, Top-most OG col = 2
            % Fibers on bottom: Right-most OG col = 1, Left-most OG col = 2
            for i = 1 : length(OGProjDir(:, 1))
                Edge = OGProjDir(i, 5);
                OGA   = OGProjDir(i, 1); OGB = OGProjDir(i, 2);
                ProjA = OGProjDir(i, 3); ProjB = OGProjDir(i, 4);
                if(Edge == 1 || Edge == 4)
                    OGA_y = obj.ListOfFibers{OGA}.Center(2);
                    OGB_y = obj.ListOfFibers{OGB}.Center(2);
                    if(OGB_y < OGA_y)
                        OGProjDir(i, 1) = OGB;
                        OGProjDir(i, 2) = OGA;
                        OGProjDir(i, 3) = ProjB;
                        OGProjDir(i, 4) = ProjA;
                    end
                elseif(Edge == 2 || Edge == 3)
                    OGA_x = obj.ListOfFibers{OGA}.Center(1);
                    OGB_x = obj.ListOfFibers{OGB}.Center(1);
                    if(OGB_x > OGA_x)
                        OGProjDir(i, 1) = OGB;
                        OGProjDir(i, 2) = OGA;
                        OGProjDir(i, 3) = ProjB;
                        OGProjDir(i, 4) = ProjA;
                    end
                end
            end
        end

        % Create voids by deleting fibers:
        function EditCertainFiberProperties(obj, Inputs)
            for i = 1 : obj.NumberOfFibers
                if(obj.ListOfFibers{i}.isVoid)
                    for j = 1 : length(obj.ListOfFibers{i}.ElementNumbersInFiber)
                        ElemNum = obj.ListOfFibers{i}.ElementNumbersInFiber(j);
                        Nodes = obj.ListOfElements{ElemNum}.NodalLocations;
                        obj.ListOfElements{ElemNum} = Element_6NT(Inputs.Fiber2Data, Nodes);
                        obj.ListOfElements{ElemNum}.ElementPhase = "Fiber2";
                    end
                end
            end
        end

        % Create a list of global nodal locations:
        function DetermineGlobalNodeLocations(obj)
            GlobalNodes = zeros(obj.NumberOfElements * 8, obj.NumberOfDimensions);
            NodeCount = 0;
            isZero = false;
            for i = 1 : obj.NumberOfElements
                element = obj.ListOfElements{i};
                LocalNodes = VectorToTable(element.NodalLocations, element.NumberOfNodes, obj.NumberOfDimensions);
                for j = 1 : 1 : element.NumberOfNodes
                    CurrentNode = LocalNodes(j, :);
                    % If (0,0) is a node, add the first occurrence of it
                    if(isequal(CurrentNode, zeros(1, obj.NumberOfDimensions)) && ~isZero)
                        isZero = true;
                        continue
                    end
                    % Node number 1 always added to assembly
                    if(NodeCount == 0)
                        NodeCount = NodeCount + 1;
                        GlobalNodes(NodeCount, :) = CurrentNode;
                    else
                        % Check if the current node already exists in the
                        % global node table. If so, skip to next node
                        for k = 1 : NodeCount
                            Overlap = Element.CheckNodeOverlap(CurrentNode, GlobalNodes(k, :));
                            if(Overlap)
                                break
                            end
                            if(k == NodeCount)
                               NodeCount = NodeCount + 1;
                               GlobalNodes(NodeCount, :) = CurrentNode;
                            end
                        end
                    end
                end
            end
            GlobalNodes(all(GlobalNodes == 0, 2), :) = [];
            % For 2.5D assemblies, move the 'ghost node' to the end
            if(strcmp(obj.Type, "2p5D") || strcmp(obj.Type, "2p5D_Reduced"))
                GlobalNodes(obj.ListOfElements{1}.NumberOfNodes, :) = [];
                GlobalNodes = [GlobalNodes; [1, 0, 0]];
            end
            % If (0,0) was found, add this in after deleting zeros from preallocation:
            if(isZero) 
                GlobalNodes = [zeros(1, obj.NumberOfDimensions); GlobalNodes]; 
            end
            obj.NumberOfNodes = length(GlobalNodes(:, 1));
            obj.NodalLocations = TableToVec(GlobalNodes);
        end

        % Assemble boundary node pair array:
        function AssembleBoundaryNodePairArray(obj)
            PairCount = 0;
            obj.NodePairArray = zeros(obj.NumberOfNodes, 2);
            obj.TopEdge   = zeros(obj.NumberOfNodes); TopCount   = 0;
            obj.RightEdge = zeros(obj.NumberOfNodes); RightCount = 0;
            GlobalNodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NumberOfDimensions);
            for i = 1 : 4
                switch i
                    case 1
                        xDir = -1 * obj.RVEBoundary(1);
                        yDir = obj.RVEBoundary(2);
                    case 2
                        xDir = 0.0;
                        yDir = obj.RVEBoundary(2);
                    case 3
                        xDir = obj.RVEBoundary(1);
                        yDir = obj.RVEBoundary(2);
                    case 4
                        xDir = obj.RVEBoundary(1);
                        yDir = 0.0;
                end
                for j = 1 : obj.NumberOfNodes
                    CurrentNode = GlobalNodes(j, :);
                    if(obj.NumberOfDimensions == 2)
                        CurrentNode(1) = CurrentNode(1) + xDir;
                        CurrentNode(2) = CurrentNode(2) + yDir;
                    elseif(obj.NumberOfDimensions == 3)
                        CurrentNode(2) = CurrentNode(2) + xDir;
                        CurrentNode(3) = CurrentNode(3) + yDir;
                    end
                    for k = 1 : obj.NumberOfNodes
                        if(j == k); continue; end
                        Overlap = Element.CheckNodeOverlap(CurrentNode, GlobalNodes(k, :));
                        if(Overlap)
                            PairCount = PairCount + 1;
                            obj.NodePairArray(PairCount, 1) = j;
                            obj.NodePairArray(PairCount, 2) = k;
                            % OG Node Num   = k
                            % Proj Node Num = j
                            % (See FindRVEEdgeNodes FxT_V7 for more details)
                            switch i
                                case 1
                                    TopCount = TopCount + 1;
                                    RightCount = RightCount + 1;
                                    obj.TopEdge(TopCount) = k;
                                    obj.RightEdge(RightCount) = j;
                                case 2
                                    TopCount = TopCount + 1;
                                    obj.TopEdge(TopCount) = k;
                                case 3
                                    TopCount = TopCount + 1;
                                    RightCount = RightCount + 1;
                                    obj.TopEdge(TopCount) = k;
                                    obj.RightEdge(RightCount) = k;
                                case 4
                                    RightCount = RightCount + 1;
                                    obj.RightEdge(RightCount) = k;
                            end
                            break
                        end
                    end
                end
            end
            obj.NodePairArray(all(obj.NodePairArray == 0, 2), :) = [];
            obj.TopEdge = obj.TopEdge(obj.TopEdge ~= 0);
            obj.RightEdge = obj.RightEdge(obj.RightEdge ~= 0);
            obj.TopEdge = unique(obj.TopEdge);
            obj.RightEdge = unique(obj.RightEdge);
        end

        % Find number of interior triangle elements:
        function NumberOfInteriorElements = FindNumberOfInteriorTriangleElements(obj)
            for i = 1 : obj.NumberOfElements
                element = obj.ListOfElements{i};
                if(strcmp(element.IsometricShape, "Tri") && strcmp(element.ElementPhase, "Matrix"))
                    continue
                else
                    NumberOfInteriorElements = i-1;
                    break
                end
            end
        end

        % Determine current element count:
        function CalculateCurrentNumberOfElements(obj)
            obj.NumberOfElements = sum(~cellfun('isempty', obj.ListOfElements));
        end

        % Increase element count:
        function IncreaseElementCount(obj)
            obj.NumberOfElements = obj.NumberOfElements + 1;
        end

        % Decrease element count:
        function DecreaseElementCount(obj)
            obj.NumberOfElements = obj.NumberOfElements - 1;
        end

        % Plot fibers:
        function PlotFibers(obj, n, PlotFiberNumbers)
            for i = 1 : obj.NumberOfFibers
                obj.ListOfFibers{i}.PlotFiber(n, PlotFiberNumbers);
                hold on
            end
            axis equal
            grid on
        end

        % Plot boundary:
        function PlotBoundary(obj)
            xmin = 0.0;
            ymin = 0.0;
            width  = obj.RVEBoundary(1);
            height = obj.RVEBoundary(2);
            rectangle('Position', [xmin, ymin, width, height], 'LineWidth', 1.5, 'EdgeColor', 'm')
        end

        % Plot Elements:
        function PlotElements(obj, FiberColor, MatrixColor, LineWidth, MarkerSize)
            if nargin < 5; MarkerSize = 4; end
            if nargin < 4; LineWidth = 1; end
            if nargin < 3; MatrixColor = 'b-*'; end
            if nargin < 2; FiberColor = 'k-*'; end
            for i = 1 : obj.NumberOfElements
                if(isempty(obj.ListOfElements{i})); continue; end
                Phase = obj.ListOfElements{i}.ElementPhase;
                if(strcmp(Phase, "Fiber"))
                    obj.ListOfElements{i}.PlotElement(FiberColor, LineWidth, MarkerSize);
                elseif(strcmp(Phase, "Matrix"))
                    obj.ListOfElements{i}.PlotElement(MatrixColor, LineWidth, MarkerSize);
                else
                    warning("Please Assign Element Phase for Element Number %d", i)
                end
                hold on
            end
        end

        % Plot global node numbers
        function PlotGlobalNodeNumbers(obj)
            AllNodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NDOFPNode);
            for i = 1 : obj.NumberOfNodes
                x = AllNodes(i, obj.NumberOfDimensions - 1);
                y = AllNodes(i, obj.NumberOfDimensions);
                NodeNum = num2str(i);
                text(x+0.01, y+0.01, 1e1, NodeNum, 'FontSize', 6)
            end
        end

        % Plot Boundary Node Pairs
        function PlotNodePairs(obj)
            for i = 1 : length(obj.NodePairArray)
                Nodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NumberOfDimensions);
                n1 = obj.NodePairArray(i, 1);
                n2 = obj.NodePairArray(i, 2);
                node1 = Nodes(n1, :);
                node2 = Nodes(n2, :);
                if(obj.NumberOfDimensions == 2)
                    text(node1(1), node1(2), num2str(n1), 'FontSize', 10, 'Color', 'r')
                    hold on
                    text(node2(1), node2(2), num2str(n2), 'FontSize', 10, 'Color', 'r')
                elseif(obj.NumberOfDimensions == 3)
                    text(node1(2), node1(3), num2str(n1), 'FontSize', 10, 'Color', 'r')
                    hold on
                    text(node2(2), node2(3), num2str(n2), 'FontSize', 10, 'Color', 'r')
                end
            end
        end

        % Make edits to default element types:
        function MakeChangesToDefaultElementTypes(obj, MaterialInputs)
            % Check if any fibers are set as voids (or altered properties)
            % Recall to alter properties, put a '1' in col next to radius
            % in pack file
            obj.EditCertainFiberProperties(MaterialInputs);
            % Add quad elements to fibers:
            %Mesh.AddQuadElementsToFibers(MaterialInputs);
            % Split interior triangles into 4 elements:
            %Mesh.SplitInteriorTrianglesIntoFourElements(MaterialInputs);
            % Split quad elements between fibers into two:
            %Mesh.SplitMatrixQuadIntoTwo(MaterialInputs);
            % Increase fiber element and matrix element order:
            if obj.NumberOfDimensions == 2 && ~obj.isReducedOrder
                if MaterialInputs.AssemblyData.isQuadTenNoded
                    obj.Fiber10NodedTriMatrix6NodedQuadMatrix10Noded(MaterialInputs);
                elseif MaterialInputs.AssemblyData.isQuadTwelveNoded || MaterialInputs.AssemblyData.isQuadSixteenNoded
                    obj.Fiber10NodedTriMatrix10NodedQuadMatrix12NodedOr16Noded(MaterialInputs);
                end
            end
            % Replace interior triangle elements with 2 linear tris and 1 linear quad:
            %Mesh.ReplaceInteriorTrianglesWithLinearTrisAndQuad(MaterialInputs);
        end
    end

    %% Private Methods
    methods (Access = private)
        % Check if triad should be kept or deleted:
        function KeepTriad = isTriadBeingDeleted(obj, triad)
            KeepTriad = true;
            isProjections = zeros(1, 3);
            for i = 1 : 3
                % If fiber is in unwanted quadrant, delete triad:
                if(~obj.ListOfFibers{triad(i)}.inQuadrantToKeep)
                    KeepTriad = false;
                    return
                end
                % Is fiber a projection?
                isProjections(i) = obj.ListOfFibers{triad(i)}.isProjection;
            end
            % Delete triad if all fibers are projections:
            if(all(isProjections))
                KeepTriad = false;
            end
        end

        % Method for renumbering fibers in connectivity:
        function [NewFiberFlag, i, j] = isThisANewFiberNumber(obj, Row, NumberToCheck)
            NewFiberFlag = true;
            if(Row == 1)
                i = 0; j = 0;
                return
            end
            for i = 1 : Row-1
                for j = 1 : 3
                    if(isequal(obj.FiberConnectivity(i, j), NumberToCheck))
                        NewFiberFlag = false;
                        return
                    end
                end
            end
        end

        % Find a triad number from two fiber numbers:
        function TriadNumber = FindTriadNumberFromTwoFibers(obj, FN1, FN2)
            TriadNumber = 0;
            for i = 1 : obj.NumberOfTriads
                TriadFlag = obj.ListOfTriads{i}.AreTheseTwoFibersPartOfThisTriad(FN1, FN2);
                if(TriadFlag)
                    TriadNumber = i;
                    return
                end
            end
        end

        % Find interior triangle node connected to a fiber element in a
        % certain triad (For building boundary elements):
        function Node = FindInteriorTriangleNodeConnectedToFiber(obj, TN, FN, isFiberAProjection, dir)
            % TN = Triad Number
            % FN = Fiber Number
            % IE = Interior Matrix Element
            IE = obj.ListOfElements{TN};
            IENodes = obj.CreateTableOfNodes(IE, 2);
            ElementsInFiber = obj.ListOfFibers{FN}.ElementNumbersInFiber;
            if(~isempty(ElementsInFiber))
                for i = 1 : length(ElementsInFiber)
                    FE = obj.ListOfElements{ElementsInFiber(i)};
                    FENodes = obj.CreateTableOfNodes(FE, 2);
                    for j = 1 : length(FENodes(:, 1))
                        for k = 1 : length(IENodes(:, 1))
                            OverlapFlag = Element.CheckNodeOverlap(FENodes(j, :), IENodes(k, :));
                            if(OverlapFlag)
                                Node = IENodes(k, :);
                                if(isFiberAProjection)
                                   if(dir == 3)
                                       Node(1) = Node(1) + obj.RVEBoundary(1);
                                       Node(2) = Node(2) - obj.RVEBoundary(2);
                                   elseif(dir == 4)
                                       Node(1) = Node(1) - obj.RVEBoundary(1);
                                       Node(2) = Node(2) - obj.RVEBoundary(2);
                                   else
                                       Node(dir) = Node(dir) - obj.RVEBoundary(dir);
                                   end
                                end
                                return
                            end
                        end
                    end
                end
            else
                % If a corner fiber with no elements yet, check for minimum
                % distance from IE nodes to fiber center
                FC = obj.ListOfFibers{FN}.Center;
                MinDist = 1E10;
                for i = 1 : length(IENodes(:, 1))
                    Dist = MathMethods.CalcDistanceBetweenTwoPoints(FC, IENodes(i, :));
                    if(Dist <= MinDist)
                        MinDist = Dist;
                        Node = IENodes(i, :);
                        if(isFiberAProjection)
                            if(dir == 3)
                                Node(1) = Node(1) + obj.RVEBoundary(1);
                                Node(2) = Node(2) - obj.RVEBoundary(2);
                            else
                                Node(dir) = Node(dir) - obj.RVEBoundary(dir);
                            end
                        end
                    end
                end
            end
        end
    end

    %% Static Methods
    methods (Static)
        % Generate mesh:
        function Mesh = GenerateMesh(Type, MaterialInputs, Pack)
            % Read Pack File:
            pack = PackFile(Pack);
            [ListOfFibers, RVEBoundary] = pack.LoadPackFile;
            % Create Quadrants:
            [ListOfQuadrants, ListOfAllFibers] = Quadrant.CreateQuadrantsOfFibersForTriangulation(ListOfFibers, RVEBoundary);
            % Create Mesh and Triangulate:
            Mesh = FE_Mesh.InitializeMesh(Type, ListOfQuadrants);
            DT = delaunayTriangulation(Mesh.ListOfFiberCenters);
            Mesh.FiberConnectivity = DT.ConnectivityList;
            % Delete Unwanted Triads and Fibers:
            Mesh.DeleteUnwantedFibersAndTriads;
            Mesh.ReNumberFiberConnectivityAfterDeletion(ListOfAllFibers);
            % With remaining triads and fibers, create a list of triads:
            Mesh.CreateListOfTriads;
            % Check if triads need to be retriangulated:
            Mesh.FindOverlappingTriads;
            % Find which triads share borders and which are on the boundary:
            Mesh.FindWhichTriadsAreConnectedAndWhichAreOnBoundary;
            % Create an array of triad pairs (This is will be used for building interior fiber/matrix elements):
            Mesh.CreateArrayOfAllTriadPairs;
            % Create interior triangle elements:
            Mesh.BuildAllInteriorTriangleElements(MaterialInputs)
            % Create interior fiber/matrix elements:
            Mesh.BuildInteriorFiberMatrixElements(MaterialInputs);
            % Determine which edge original fibers are on:
            Mesh.DetermineOriginalFibersAlongEdge;
            % Build Boundary Fiber/Matrix elements:
            Mesh.BuildBoundaryFiberMatrixElements(MaterialInputs);
            % Edit Default mesh
            Mesh.MakeChangesToDefaultElementTypes(MaterialInputs);
            % Delete extra elements from preallocation:
            Mesh.ListOfElements = Mesh.ListOfElements(~cellfun('isempty', Mesh.ListOfElements));
            % Determine assembly nodal locations:
            Mesh.DetermineGlobalNodeLocations;
            % Assemble node pair array:
            Mesh.AssembleBoundaryNodePairArray;
            % Find pinned node:
            Mesh.FindPinnedNode; 
        end

        % Initialize mesh:
        function obj = InitializeMesh(Type, Quadrants)
            switch Type
                case "2D"
                    obj = FE_Mesh_2D(Quadrants);
                case "2D_Reduced"
                    obj = FE_Mesh_2D_Reduced(Quadrants);
                case "2p5D"
                    obj = FE_Mesh_2p5D(Quadrants);
                case "2p5D_Reduced"
                    obj = FE_Mesh_2p5D_Reduced(Quadrants);
            end
        end

        % Build 3-Noded Triangle (2D):
        function Element = Build3NodedTriangle(MaterialModel, NodalLocations)
            Element = Element_3NT(MaterialModel, NodalLocations);
        end

        % Build 4-Noded Triangle (2D):
        function Element = Build4NodedTriangle(MaterialModel, NodalLocations)
            Element = Element_4NT(MaterialModel, NodalLocations);
        end

        % Build 6-Noded Triangle (2D):
        function Element = Build6NodedTriangle(MaterialModel, NodalLocations)
            Element = Element_6NT(MaterialModel, NodalLocations);
        end

        % Build 4-Noded Quad (2D):
        function Element = Build4NodedQuad(MaterialModel, NodalLocations)
            Element = Element_4NQ(MaterialModel, NodalLocations);
        end

        % Build 6-Noded Quad (2D):
        function Element = Build6NodedQuad(MaterialModel, NodalLocations)
            Element = Element_6NQ(MaterialModel, NodalLocations);
        end

        % Build 8-Noded Quad (2D):
        function Element = Build8NodedQuad(MaterialModel, NodalLocations)
            Element = Element_8NQ(MaterialModel, NodalLocations);
        end

        % Build 9-Noded Quad (2D):
        function Element = Build9NodedQuad(MaterialModel, NodalLocations)
            Element = Element_9NQ(MaterialModel, NodalLocations);
        end

        % Build 10-Noded Tri (2D):
        function Element = Build10NodedTriangle(MaterialModel, NodalLocations)
            Element = Element_10NT(MaterialModel, TableToVec(NodalLocations));
        end

        % Build 10-Noded Quad (2D):
        function Element = Build10NodedQuad(MaterialModel, NodalLocations)
            Element = Element_10NQ(MaterialModel, TableToVec(NodalLocations));
        end

        % Build 12-Noded Quad (2D):
        function Element = Build12NodedQuad(MaterialModel, NodalLocations)
            Element = Element_12NQ(MaterialModel, TableToVec(NodalLocations));
        end

        % Build 12-Noded Quad (2D):
        function Element = Build16NodedQuad(MaterialModel, NodalLocations)
            Element = Element_16NQ(MaterialModel, TableToVec(NodalLocations));
        end

        % Build 3-Noded Triangle (2.5D):
        function Element = Build3NodedTriangle_2p5D(MaterialModel, NodalLocations)
            Element = Element_4NT_2p5D(MaterialModel, NodalLocations);
        end

        % Build 5-Noded Triangle (2.5D):
        function Element = Build4NodedTriangle_2p5D(MaterialModel, NodalLocations)
            Element = Element_5NT_2p5D(MaterialModel, NodalLocations);
        end

        % Build 7-Noded Triangle (2.5D):
        function Element = Build6NodedTriangle_2p5D(MaterialModel, NodalLocations)
            Element = Element_7NT_2p5D(MaterialModel, NodalLocations);
        end

        % Build 7-Noded Quad (2.5D):
        function Element = Build6NodedQuad_2p5D(MaterialModel, NodalLocations)
            Element = Element_7NQ_2p5D(MaterialModel, NodalLocations);
        end

        % Build 9-Noded Quad (2.5D):
        function Element = Build8NodedQuad_2p5D(MaterialModel, NodalLocations)
            Element = Element_9NQ_2p5D(MaterialModel, NodalLocations);
        end

        % Plot fiber connectivity:
        function PlotTriadConnectivity(Points, ConnectMat)
            triplot(ConnectMat, Points(:, 1), Points(:, 2), 'k', 'LineWidth', 0.5)
        end
    end

    %% Private Static Methods
    methods (Static, Access = private)
        % When building interior fiber/matrix elements, check to make sure we don't double count triad edges:
        function isEdgeUsed = CheckIfEdgeHasBeenUsed(Edge, UsedEdges)
            isEdgeUsed = false;
            for i = 1 : length(UsedEdges(:, 1))
                % Ignore extra zeros from preallocation:
                if(isequal(UsedEdges(i, :), [0, 0]))
                    return
                end
                % Check if edge has already been used:
                if(all(ismember(Edge, UsedEdges(i, :))))
                    isEdgeUsed = true;
                end
            end
        end

        % Find interior triangle node closest to fiber center (For creating fiber elements):
        function ClosestNode = FindInteriorTriangleNodeClosestToFiber(FiberCenter, TriangleNodes, nDim)
            MinDist = 1E10;
            if(nDim == 3); TriangleNodes(end, :) = []; TriangleNodes(:, 1) = []; end
            for i = 1 : length(TriangleNodes(:, 1))
                Dist = MathMethods.CalcDistanceBetweenTwoPoints(FiberCenter, TriangleNodes(i, :));
                if(Dist <= MinDist)
                    MinDist = Dist;
                    ClosestNode = TriangleNodes(i, :);
                end
            end
        end

        % Change middle node of fibers/matrix if fibers are too close:
        function [FN1, FN2] = ChangeMiddleNodeIfFibersAreTooClose(FN1, FN2, r1, r2, isReducedOrder, nDim)
            DistanceBetweenFibers = MathMethods.CalcDistanceBetweenTwoPoints(FN1(1, :), FN2(1, :));
            SumOfFiberRadii = r1 + r2;
            Ratio = SumOfFiberRadii / DistanceBetweenFibers;
            if(Ratio >= 0.90)
                if(nDim == 3)
                    FN1(:, 1) = [];
                    FN2(:, 1) = [];
                end
                % Vectors connecting fiber centers:
                V12 = [FN2(1, 1) - FN1(1, 1), FN2(1, 2) - FN1(1, 2)];
                V21 = [FN1(1, 1) - FN2(1, 1), FN1(1, 2) - FN2(1, 2)];
                % Angles between vectors and x axis:
                T12 = MathMethods.CalculateAngleBetweenVectors([1, 0], V12);
                T21 = MathMethods.CalculateAngleBetweenVectors([1, 0], V21);
                % Update middle node location:
                FN1_MiddleNode = [FN1(1, 1) + r1*cos(T12), FN1(1, 2) + r1*sin(T12)];
                FN2_MiddleNode = [FN2(1, 1) + r2*cos(T21), FN2(1, 2) + r2*sin(T21)];
                if(nDim == 3)
                    FN1_MiddleNode = [0.0, FN1_MiddleNode];
                    FN2_MiddleNode = [0.0, FN2_MiddleNode];
                    FN1 = [zeros(length(FN1(:, 1)), 1), FN1]; FN1(end, 1) = 1.0;
                    FN2 = [zeros(length(FN2(:, 1)), 1), FN2]; FN2(end, 1) = 1.0;
                end
                if(isReducedOrder)
                    FN1(3, :) = FN1_MiddleNode;
                    FN2(3, :) = FN2_MiddleNode;
                else
                    FN1(4, :) = FN1_MiddleNode;
                    FN2(4, :) = FN2_MiddleNode;
                end
            end
        end
    end
end


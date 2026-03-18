classdef FE_Mesh_2D < FE_Mesh
    %2D Mesh
    
    %% Properties
    properties (Constant)
        Type = "2D";
        NumberOfDimensions = 2;
        NDOFPNode = 2;
        isReducedOrder = false;
    end
    
    %% Methods
    methods
        % Constructor
        function obj = FE_Mesh_2D(ListOfQuadrants)
           NumberOfFibers = length(ListOfQuadrants) * ListOfQuadrants{1}.NumberOfFibers;
           obj.NumberOfFibers = NumberOfFibers;
           obj.ListOfFibers = cell(1, NumberOfFibers);
           obj.ListOfFiberCenters = zeros(NumberOfFibers, 2);
           count = 0;
           for i = 1 : length(ListOfQuadrants)
               for j = 1 : ListOfQuadrants{1}.NumberOfFibers
                   count = count + 1;
                   obj.ListOfFibers{count} = ListOfQuadrants{i}.ListOfFibers{j};
                   obj.ListOfFiberCenters(count, :) = ListOfQuadrants{i}.ListOfFibers{j}.Center;
               end
           end
           NumElements = 10000;
           obj.NumberOfElements = NumElements;
           obj.ListOfElements = cell(1, NumElements);
           obj.RVEBoundary = [abs(ListOfQuadrants{1}.Corners(2, 1) - ListOfQuadrants{1}.Corners(1, 1)), ...
                              abs(ListOfQuadrants{1}.Corners(2, 2) - ListOfQuadrants{1}.Corners(1, 2))];
        end

        % Build Interior Triangle Element:
        function Element = BuildInteriorTriangleElement(obj, MaterialModel, NodalLocations)
            switch length(NodalLocations)
                case 3
                    Element = obj.Build3NodedTriangle(MaterialModel, TableToVec(NodalLocations));
                case 6
                    Element = obj.Build6NodedTriangle(MaterialModel, TableToVec(NodalLocations));
                case 10
                    Element = obj.Build10NodedTriangle(MaterialModel, TableToVec(NodalLocations));
            end
        end

        % Build Tri Fiber Element:
        function Element = BuildFiberElement(obj, MaterialModel, NodalLocations)
            switch length(NodalLocations)
                case 6
                    Element = obj.Build6NodedTriangle(MaterialModel, TableToVec(NodalLocations));
                case 8
                    Element = obj.Build8NodedQuad(MaterialModel, TableToVec(NodalLocations));
                case 10
                    Element = obj.Build10NodedTriangle(MaterialModel, TableToVec(NodalLocations));
            end
        end

        % Build Quad Matrix Element:
        function Element = BuildQuadMatrixElement(obj, MaterialModel, NodalLocations, isNineNoded)
            if nargin < 3; isNineNoded = false; end
            switch length(NodalLocations)
                case 4
                    Element = obj.Build4NodedQuad(MaterialModel, TableToVec(NodalLocations));
                case 8
                    if ~isNineNoded
                        Element = obj.Build8NodedQuad(MaterialModel, TableToVec(NodalLocations));
                    else
                        NodalLocations = obj.FindNinthNodeForQuad(NodalLocations);
                        Element = obj.Build9NodedQuad(MaterialModel, TableToVec(NodalLocations));
                    end
                case 10
                    Element = obj.Build10NodedQuad(MaterialModel, TableToVec(NodalLocations));
                case 12
                    Element = obj.Build12NodedQuad(MaterialModel, TableToVec(NodalLocations));
                case 16
                    Element = obj.Build16NodedQuad(MaterialModel, TableToVec(NodalLocations));
            end
        end

        % Add Quad Elements to Fiber:
        function AddQuadElementsToFibers(obj, MaterialInputs)
            ElementsToDelete = zeros(obj.NumberOfElements, 1);
            count = 0;
            for i = 1 : obj.NumberOfFibers
                Fiber = obj.ListOfFibers{i};
                EN = Fiber.GetElementNumbersInFiber;
                for j = 1 : length(EN)
                    count = count + 1;
                    ElementsToDelete(count) = EN(j);
                    element = obj.ListOfElements{EN(j)};
                    OldFiberNodes = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NumberOfDimensions);
                    TriFiberNodes = obj.GetNewInnerTriFiberNodes(OldFiberNodes);
                    QuadFiberNodes = obj.GetNewInnerQuadFiberNodes(OldFiberNodes, TriFiberNodes);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, TriFiberNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, QuadFiberNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            for i = 1 : obj.NumberOfFibers
                obj.ListOfFibers{i}.ReduceElementNumbersInFiber(count);
            end
            obj.NumberOfElements = obj.NumberOfElements - count;
        end

        % Split interior triangles into four elements:
        function SplitInteriorTrianglesIntoFourElements(obj, MaterialInputs)
            ElementsToDelete = zeros(obj.NumberOfElements, 1);
            count = 0;
            for i = 1 : obj.NumberOfElements
                element = obj.ListOfElements{i};
                if(strcmp(element.ElementPhase, "Matrix") && strcmp(element.IsometricShape, "Tri"))
                    count = count + 1;
                    ElementsToDelete(count) = i;
                    OldTriNodes = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NumberOfDimensions);
                    for j = 1 : 4
                        NewTriNodes = obj.GetNewInteriorTriNodes_Quadractic(OldTriNodes, j);
                        obj.IncreaseElementCount;
                        obj.ListOfElements{obj.NumberOfElements} = obj.BuildInteriorTriangleElement(MaterialInputs.TriMatrixData, NewTriNodes);
                        obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                    end
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            obj.NumberOfElements = obj.NumberOfElements - count;
        end

        % Replace interior triangle elements with 2 linear tris and 1 linear quad:
        function ReplaceInteriorTrianglesWithLinearTrisAndQuad(obj, MaterialInputs)
            ElementsToDelete = zeros(obj.NumberOfElements, 1);
            count = 0;
            for i = 1 : obj.NumberOfElements
                element = obj.ListOfElements{i};
                if(strcmp(element.ElementPhase, "Matrix") && strcmp(element.IsometricShape, "Tri"))
                    count = count + 1;
                    ElementsToDelete(count) = i;
                    OldTriNodes = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NumberOfDimensions);
                    for j = 1 : 3
                        obj.IncreaseElementCount;
                        if(j == 1 || j == 2)
                            NewTriNodes = obj.GetNewInteriorTriNodes_Linear(OldTriNodes, j);
                            obj.ListOfElements{obj.NumberOfElements} = obj.BuildInteriorTriangleElement(MaterialInputs.TriMatrixData, NewTriNodes);
                        else
                            NewQuadNodes = obj.GetNewInteriorQuadNodes_Linear(OldTriNodes);
                            obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.TriMatrixData, NewQuadNodes);
                        end
                        obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                    end
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            obj.NumberOfElements = obj.NumberOfElements - count;
        end

        % Split matrix quad into two elements:
        function SplitMatrixQuadIntoTwo(obj, MaterialInputs)
            ElementsToDelete = zeros(obj.NumberOfElements, 1);
            count = 0;
            for i = 1 : obj.NumberOfElements
                element = obj.ListOfElements{i};
                if(strcmp(element.ElementPhase, "Matrix") && strcmp(element.IsometricShape, "Quad"))
                    count = count + 1;
                    ElementsToDelete(count) = i;
                    OldMatrixNodes = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NumberOfDimensions);
                    for j = 1 : 2
                        NewQuadNodes = obj.GetNewMatrixNodes(OldMatrixNodes, j);
                        obj.IncreaseElementCount;
                        obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.QuadMatrixData, NewQuadNodes, MaterialInputs.AssemblyData.isQuadNineNoded);
                        obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                    end
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            obj.NumberOfElements = obj.NumberOfElements - count;
        end

        % Transform fibers into 10NT and matrix into 10NQ:
        function Fiber10NodedTriMatrix6NodedQuadMatrix10Noded(obj, MaterialInputs)
            % Find number of interior triangle elements:
            NumberOfInteriorElements = obj.FindNumberOfInteriorTriangleElements;
            count = 0;
            ElementsToDelete = NumberOfInteriorElements + 1 : 1 : obj.NumberOfElements;
            for i = NumberOfInteriorElements+1 : obj.NumberOfElements
                count = count + 1;
                % Fiber element
                if(mod(count, 3) ~= 0)
                    OldFiberNodes = VectorToTable(obj.ListOfElements{i}.NodalLocations, obj.ListOfElements{i}.NumberOfNodes, 2);
                    NewFiberNodes = obj.Find10NTFiberNodes(OldFiberNodes);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, NewFiberNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                % Matrix element
                else
                    Fiber1Nodes = VectorToTable(obj.ListOfElements{obj.NumberOfElements-1}.NodalLocations, obj.ListOfElements{obj.NumberOfElements-1}.NumberOfNodes, 2);
                    Fiber2Nodes = VectorToTable(obj.ListOfElements{obj.NumberOfElements}.NodalLocations, obj.ListOfElements{obj.NumberOfElements}.NumberOfNodes, 2);
                    NewMatrixNodes = obj.Find10NQMatrixNodes(Fiber1Nodes, Fiber2Nodes);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.QuadMatrixData, NewMatrixNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            obj.NumberOfElements = obj.NumberOfElements - count;
        end

        % Transform fibers and interior matrix into 10NT and matrix into 12NQ:
        function Fiber10NodedTriMatrix10NodedQuadMatrix12NodedOr16Noded(obj, MaterialInputs)
            % Find number of interior triangle elements:
            NumberOfInteriorElements = obj.FindNumberOfInteriorTriangleElements;
            count = 0;
            ElementsToDelete = 1 : obj.NumberOfElements;
            for i = 1 : obj.NumberOfElements
                % Interior matrix elements:
                if i <= NumberOfInteriorElements
                    OldTriMatrixNodes = VectorToTable(obj.ListOfElements{i}.NodalLocations, obj.ListOfElements{i}.NumberOfNodes, 2);
                    NewTriMatrixNodes = obj.Find10NTMatrixNodes(OldTriMatrixNodes);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildInteriorTriangleElement(MaterialInputs.TriMatrixData, NewTriMatrixNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                    continue
                end
                count = count + 1;
                % Fiber elements:
                if(mod(count, 3) ~= 0)
                    OldFiberNodes = VectorToTable(obj.ListOfElements{i}.NodalLocations, obj.ListOfElements{i}.NumberOfNodes, 2);
                    NewFiberNodes = obj.Find10NTFiberNodes(OldFiberNodes);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildFiberElement(MaterialInputs.FiberData, NewFiberNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Fiber";
                % Matrix elements:
                else
                    Fiber1Nodes = VectorToTable(obj.ListOfElements{obj.NumberOfElements-1}.NodalLocations, obj.ListOfElements{obj.NumberOfElements-1}.NumberOfNodes, 2);
                    Fiber2Nodes = VectorToTable(obj.ListOfElements{obj.NumberOfElements}.NodalLocations, obj.ListOfElements{obj.NumberOfElements}.NumberOfNodes, 2);
                    NewMatrixNodes = obj.Find12NQOr16NQMatrixNodes(Fiber1Nodes, Fiber2Nodes, MaterialInputs.AssemblyData.isQuadSixteenNoded);
                    obj.IncreaseElementCount;
                    obj.ListOfElements{obj.NumberOfElements} = obj.BuildQuadMatrixElement(MaterialInputs.QuadMatrixData, NewMatrixNodes);
                    obj.ListOfElements{obj.NumberOfElements}.ElementPhase = "Matrix";
                end
            end
            ElementsToDelete(ElementsToDelete == 0) = [];
            obj.ListOfElements(ElementsToDelete) = [];
            obj.NumberOfElements = obj.NumberOfElements - (1 / 2) * obj.NumberOfElements;
        end

        % Find Pinned Node:
        function FindPinnedNode(obj)
            MinDist = 1e10;
            Pin = 0;
            AllNodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NumberOfDimensions);
            for i = 1 : obj.NumberOfNodes
                p1 = [0, 0];
                p2 = AllNodes(i, :);
                Dist = MathMethods.CalcDistanceBetweenTwoPoints(p1, p2);
                if(Dist <= MinDist)
                    MinDist = Dist;
                    Pin = i;
                end
            end
            obj.PinnedNode = Pin;
        end
    end

    %% Static Methods
    methods (Static)
        % Create nodal locations for interior triangle elements:
        function NodalLocations = DetermineInteriorTriangleNodes(FiberMidpoints)
            NodalLocations = zeros(6, 2);
            NodalLocations(1, :) = FiberMidpoints(1, :);
            NodalLocations(2, :) = (FiberMidpoints(1, :) + FiberMidpoints(2, :)) / 2;
            NodalLocations(3, :) = FiberMidpoints(2, :);
            NodalLocations(4, :) = (FiberMidpoints(2, :) + FiberMidpoints(3, :)) / 2;
            NodalLocations(5, :) = FiberMidpoints(3, :);
            NodalLocations(6, :) = (FiberMidpoints(1, :) + FiberMidpoints(3, :)) / 2;
        end

        % Create nodal locations for fiber elements:
        function NodalLocations = DetermineFiberNodeOrder(FN1, FN2, FN3, FiberRadius, isEdgeCCW)
             % FN = Fiber Nodes
             NodalLocations = zeros(6, 2);
             NodalLocations(1, :) = FN1;
             if(isEdgeCCW)
                 NodalLocations(3, :) = FN3;
                 NodalLocations(5, :) = FN2;
             else
                 NodalLocations(3, :) = FN2;
                 NodalLocations(5, :) = FN3;
             end
             NodalLocations(2, :) = (NodalLocations(1, :) + NodalLocations(3, :)) / 2.0;
             NodalLocations(6, :) = (NodalLocations(1, :) + NodalLocations(5, :)) / 2.0;
             MidPointBetweenNodes3And5 = (NodalLocations(3, :) + NodalLocations(5, :)) / 2.0;
             MidPointVector = MathMethods.MakeVector2D(FN1, MidPointBetweenNodes3And5);
             MidPointAngle  = MathMethods.CalculateAngleBetweenVectors([1, 0], MidPointVector);
             Node4x = FN1(1) + FiberRadius * cos(MidPointAngle);
             Node4y = FN1(2) + FiberRadius * sin(MidPointAngle);
             NodalLocations(4, :) = [Node4x, Node4y];
        end

        % Create nodal locations for interior matrix elements:
        function NodalLocations = DetermineInteriorMatrixNodeOrder(FN1, FN2, isEdgeCCW)
             % FN = Fiber Nodes
             NodalLocations = zeros(8, 2);
             if(isEdgeCCW)
                 NodalLocations(1, :) = FN1(5, :);
                 NodalLocations(2, :) = FN1(4, :);
                 NodalLocations(3, :) = FN1(3, :);
                 NodalLocations(5, :) = FN2(5, :);
                 NodalLocations(6, :) = FN2(4, :);
                 NodalLocations(7, :) = FN2(3, :);
             else
                 NodalLocations(1, :) = FN2(5, :);
                 NodalLocations(2, :) = FN2(4, :);
                 NodalLocations(3, :) = FN2(3, :);
                 NodalLocations(5, :) = FN1(5, :);
                 NodalLocations(6, :) = FN1(4, :);
                 NodalLocations(7, :) = FN1(3, :);
             end
             NodalLocations(4, :) = (NodalLocations(3, :) + NodalLocations(5, :)) / 2.0;
             NodalLocations(8, :) = (NodalLocations(1, :) + NodalLocations(7, :)) / 2.0;
        end

        % Make sure fiber nodes are CCW:
        function FiberNodes = MakeSureFiberNodesAreCCW(FiberNodes, radius)
            V13 = MathMethods.MakeVector2D(FiberNodes(1, :), FiberNodes(3, :));
            V14 = MathMethods.MakeVector2D(FiberNodes(1, :), FiberNodes(4, :));
            T13_T14 = MathMethods.CalculateAngleBetweenVectors(V13, V14);
            T13 = MathMethods.CalculateAngleBetweenVectors([1, 0], V13);
            T14 = T13 + T13_T14;
            N4x = FiberNodes(1, 1) + radius * cos(T14);
            N4y = FiberNodes(1, 2) + radius * sin(T14);
            N4Check = Element.CheckNodeOverlap([N4x, N4y], FiberNodes(4, :));
            if(N4Check)
                return
            else
                TempNodes = FiberNodes;
                TempNodes(2, :) = FiberNodes(6, :);
                TempNodes(3, :) = FiberNodes(5, :);
                TempNodes(5, :) = FiberNodes(3, :);
                TempNodes(6, :) = FiberNodes(2, :);
                FiberNodes = TempNodes;
            end
        end

        % Find new inner tri fiber nodes
        function NewTriNodes = GetNewInnerTriFiberNodes(OldFiberNodes)
            NewTriNodes = zeros(6, 2);
            NewTriNodes(1, :) = OldFiberNodes(1, :);
            NewTriNodes(2, :) = (OldFiberNodes(1, :) + OldFiberNodes(2, :)) / 2.0;
            NewTriNodes(3, :) = OldFiberNodes(2, :);
            NewTriNodes(5, :) = OldFiberNodes(6, :);
            NewTriNodes(6, :) = (OldFiberNodes(1, :) + OldFiberNodes(6, :)) / 2.0;
            rf = MathMethods.CalcDistanceBetweenTwoPoints(OldFiberNodes(1, :), OldFiberNodes(2, :));
            V4 = MathMethods.MakeVector2D([OldFiberNodes(1, :)], [OldFiberNodes(4, :)]);
            T4 = MathMethods.CalculateAngleBetweenVectors(V4, [1, 0]);
            x4 = OldFiberNodes(1, 1) + rf * cos(T4);
            y4 = OldFiberNodes(1, 2) + rf * sin(T4);
            NewTriNodes(4, :) = [x4, y4];
        end

        % Find new inner quad fiber nodes:
        function NewQuadFiberNodes = GetNewInnerQuadFiberNodes(OldFiberNodes, TriFiberNodes)
            NewQuadFiberNodes = zeros(8, 2);
            NewQuadFiberNodes(1, :) = TriFiberNodes(5, :);
            NewQuadFiberNodes(2, :) = TriFiberNodes(4, :);
            NewQuadFiberNodes(3, :) = TriFiberNodes(3, :);
            NewQuadFiberNodes(5, :) = OldFiberNodes(3, :);
            NewQuadFiberNodes(6, :) = OldFiberNodes(4, :);
            NewQuadFiberNodes(7, :) = OldFiberNodes(5, :);
            NewQuadFiberNodes(4, :) = (OldFiberNodes(2, :) + OldFiberNodes(3, :)) / 2.0;
            NewQuadFiberNodes(8, :) = (OldFiberNodes(6, :) + OldFiberNodes(5, :)) / 2.0;
        end

        % Split interior triangle into 4 elements:
        function NewTriNodes = GetNewInteriorTriNodes_Quadractic(OldTriNodes, idx)
            NewTriNodes = zeros(6, 2);
            switch idx
                case 1
                    NewTriNodes(1, :) = OldTriNodes(1, :);
                    NewTriNodes(3, :) = (OldTriNodes(1, :) + OldTriNodes(3, :)) / 2.0;
                    NewTriNodes(5, :) = (OldTriNodes(1, :) + OldTriNodes(5, :)) / 2.0;
                    NewTriNodes(2, :) = (OldTriNodes(1, :) + OldTriNodes(2, :)) / 2.0;
                    NewTriNodes(4, :) = (OldTriNodes(2, :) + OldTriNodes(6, :)) / 2.0;
                    NewTriNodes(6, :) = (OldTriNodes(1, :) + OldTriNodes(6, :)) / 2.0;
                case 2
                    NewTriNodes(1, :) = OldTriNodes(3, :);
                    NewTriNodes(3, :) = (OldTriNodes(3, :) + OldTriNodes(5, :)) / 2.0;
                    NewTriNodes(5, :) = (OldTriNodes(1, :) + OldTriNodes(3, :)) / 2.0;
                    NewTriNodes(2, :) = (OldTriNodes(3, :) + OldTriNodes(4, :)) / 2.0;
                    NewTriNodes(4, :) = (OldTriNodes(2, :) + OldTriNodes(4, :)) / 2.0;
                    NewTriNodes(6, :) = (OldTriNodes(2, :) + OldTriNodes(3, :)) / 2.0;
                case 3
                    NewTriNodes(1, :) = OldTriNodes(5, :);
                    NewTriNodes(3, :) = (OldTriNodes(5, :) + OldTriNodes(1, :)) / 2.0;
                    NewTriNodes(5, :) = (OldTriNodes(3, :) + OldTriNodes(5, :)) / 2.0;
                    NewTriNodes(2, :) = (OldTriNodes(5, :) + OldTriNodes(6, :)) / 2.0;
                    NewTriNodes(4, :) = (OldTriNodes(4, :) + OldTriNodes(6, :)) / 2.0;
                    NewTriNodes(6, :) = (OldTriNodes(4, :) + OldTriNodes(5, :)) / 2.0;
                case 4
                    NewTriNodes(1, :) = OldTriNodes(4, :);
                    NewTriNodes(3, :) = (OldTriNodes(1, :) + OldTriNodes(5, :)) / 2.0;
                    NewTriNodes(5, :) = (OldTriNodes(1, :) + OldTriNodes(3, :)) / 2.0;
                    NewTriNodes(2, :) = (OldTriNodes(4, :) + OldTriNodes(6, :)) / 2.0;
                    NewTriNodes(4, :) = (OldTriNodes(2, :) + OldTriNodes(6, :)) / 2.0;
                    NewTriNodes(6, :) = (OldTriNodes(2, :) + OldTriNodes(4, :)) / 2.0;
            end
        end

        % Splitting interior triangle elements: Get nodes for linear quad element
        function NewQuadNodes = GetNewInteriorQuadNodes_Linear(OldTriNodes)
            NewQuadNodes = zeros(4, 2);
            NewQuadNodes(1, :) = OldTriNodes(1, :);
            NewQuadNodes(2, :) = OldTriNodes(2, :);
            NewQuadNodes(3, :) = OldTriNodes(4, :);
            NewQuadNodes(4, :) = OldTriNodes(6, :);
        end

        % Splitting interior triangle elements: Get nodes for linear tri element
        function NewTriNodes = GetNewInteriorTriNodes_Linear(OldTriNodes, idx)
            NewTriNodes = zeros(3, 2);
            switch idx
                case 1
                    NewTriNodes(1, :) = OldTriNodes(2, :);
                    NewTriNodes(2, :) = OldTriNodes(3, :);
                    NewTriNodes(3, :) = OldTriNodes(4, :);
                case 2
                    NewTriNodes(1, :) = OldTriNodes(6, :);
                    NewTriNodes(2, :) = OldTriNodes(4, :);
                    NewTriNodes(3, :) = OldTriNodes(5, :);
            end
        end

        % Split quad matrix elements into 2 elements:
        function NewMatrixNodes = GetNewMatrixNodes(OldMatrixNodes, idx)
            NewMatrixNodes = zeros(8, 2);
            switch idx
                case 1
                    NewMatrixNodes(1, :) = OldMatrixNodes(1, :);
                    NewMatrixNodes(2, :) = OldMatrixNodes(2, :);
                    NewMatrixNodes(3, :) = OldMatrixNodes(3, :);
                    NewMatrixNodes(5, :) = OldMatrixNodes(4, :);
                    NewMatrixNodes(7, :) = OldMatrixNodes(8, :);
                    NewMatrixNodes(4, :) = (OldMatrixNodes(3, :) + OldMatrixNodes(4, :)) / 2.0;
                    NewMatrixNodes(6, :) = (OldMatrixNodes(4, :) + OldMatrixNodes(8, :)) / 2.0;
                    NewMatrixNodes(8, :) = (OldMatrixNodes(1, :) + OldMatrixNodes(8, :)) / 2.0;
                case 2
                    NewMatrixNodes(1, :) = OldMatrixNodes(5, :);
                    NewMatrixNodes(2, :) = OldMatrixNodes(6, :);
                    NewMatrixNodes(3, :) = OldMatrixNodes(7, :);
                    NewMatrixNodes(5, :) = OldMatrixNodes(8, :);
                    NewMatrixNodes(7, :) = OldMatrixNodes(4, :);
                    NewMatrixNodes(4, :) = (OldMatrixNodes(7, :) + OldMatrixNodes(8, :)) / 2.0;
                    NewMatrixNodes(6, :) = (OldMatrixNodes(4, :) + OldMatrixNodes(8, :)) / 2.0;
                    NewMatrixNodes(8, :) = (OldMatrixNodes(4, :) + OldMatrixNodes(5, :)) / 2.0;
            end
        end

        % Find 10NT fiber nodes:
        function NewTriNodes = Find10NTFiberNodes(OldTriNodes)
            NewTriNodes = zeros(10, 2);
            rf = MathMethods.CalcDistanceBetweenTwoPoints(OldTriNodes(1, :), OldTriNodes(3, :));
            NewTriNodes(1, :) = OldTriNodes(1, :);
            NewTriNodes(4, :) = OldTriNodes(3, :);
            NewTriNodes(7, :) = OldTriNodes(5, :);
            NewTriNodes(2, :) = OldTriNodes(1, :) + (OldTriNodes(3, :) - OldTriNodes(1, :)) / 3.0;
            NewTriNodes(3, :) = OldTriNodes(1, :) + 2.0 * (OldTriNodes(3, :) - OldTriNodes(1, :)) / 3.0;
            NewTriNodes(8, :) = OldTriNodes(1, :) + 2.0 * (OldTriNodes(5, :) - OldTriNodes(1, :)) / 3.0;
            NewTriNodes(9, :) = OldTriNodes(1, :) + (OldTriNodes(5, :) - OldTriNodes(1, :)) / 3.0;
            V14 = MathMethods.MakeVector2D(OldTriNodes(1, :), OldTriNodes(3, :));
            V17 = MathMethods.MakeVector2D(OldTriNodes(1, :), OldTriNodes(5, :));
            T4 = MathMethods.CalculateAngleBetweenVectors(V14, [1, 0]);
            T4_7 = MathMethods.CalculateAngleBetweenVectors(V14, V17);
            T5 = T4 + T4_7 / 3.0;
            T6 = T4 + 2.0 * T4_7 / 3.0;
            T10 = T4 + T4_7 / 2.0;
            x5  = OldTriNodes(1, 1) + rf * cos(T5);  y5 = OldTriNodes(1, 2) + rf * sin(T5);
            x6  = OldTriNodes(1, 1) + rf * cos(T6);  y6 = OldTriNodes(1, 2) + rf * sin(T6);
            x10 = OldTriNodes(1, 1) + 2.0 * rf * cos(T10) / 3.0; y10 = OldTriNodes(1, 2) + 2.0 * rf * sin(T10) / 3.0;
            NewTriNodes(5, :) = [x5, y5];
            NewTriNodes(6, :) = [x6, y6];
            NewTriNodes(10, :) = [x10, y10];
        end

        % Find 10NT interior triangle nodes:
        function NewMatrixNodes = Find10NTMatrixNodes(OldMatrixNodes)
            NewMatrixNodes = zeros(10, 2);
            NewMatrixNodes(1, :) = OldMatrixNodes(1, :);
            NewMatrixNodes(4, :) = OldMatrixNodes(3, :);
            NewMatrixNodes(7, :) = OldMatrixNodes(5, :);
            NewMatrixNodes(2, :) = NewMatrixNodes(1, :) + (1 / 3) * (NewMatrixNodes(4, :) - NewMatrixNodes(1, :));
            NewMatrixNodes(3, :) = NewMatrixNodes(1, :) + (2 / 3) * (NewMatrixNodes(4, :) - NewMatrixNodes(1, :));
            NewMatrixNodes(5, :) = NewMatrixNodes(4, :) + (1 / 3) * (NewMatrixNodes(7, :) - NewMatrixNodes(4, :));
            NewMatrixNodes(6, :) = NewMatrixNodes(4, :) + (2 / 3) * (NewMatrixNodes(7, :) - NewMatrixNodes(4, :));
            NewMatrixNodes(8, :) = NewMatrixNodes(7, :) + (1 / 3) * (NewMatrixNodes(1, :) - NewMatrixNodes(7, :));
            NewMatrixNodes(9, :) = NewMatrixNodes(7, :) + (2 / 3) * (NewMatrixNodes(1, :) - NewMatrixNodes(7, :));
            NewMatrixNodes(10, :) = MathMethods.CalculateCentroidOfTriangle(NewMatrixNodes(1, :), NewMatrixNodes(4, :), NewMatrixNodes(7, :));
        end

        % Find 10NQ matrix nodes:
        function NewMatrixNodes = Find10NQMatrixNodes(FN1, FN2)
            NewMatrixNodes = zeros(10, 2);
            NewMatrixNodes(1, :) = FN1(7, :);
            NewMatrixNodes(2, :) = FN1(6, :);
            NewMatrixNodes(3, :) = FN1(5, :);
            NewMatrixNodes(4, :) = FN1(4, :);
            NewMatrixNodes(6, :) = FN2(7, :);
            NewMatrixNodes(7, :) = FN2(6, :);
            NewMatrixNodes(8, :) = FN2(5, :);
            NewMatrixNodes(9, :) = FN2(4, :);
            NewMatrixNodes(5, :)  = (FN1(4, :) + FN2(7, :)) / 2.0;
            NewMatrixNodes(10, :) = (FN1(7, :) + FN2(4, :)) / 2.0;
        end

        % Find 10NQ matrix nodes:
        function NewMatrixNodes = Find12NQOr16NQMatrixNodes(FN1, FN2, isQuadSixteenNoded)
            NewMatrixNodes = zeros(12, 2);
            NewMatrixNodes(1, :) = FN1(7, :);
            NewMatrixNodes(2, :) = FN1(6, :);
            NewMatrixNodes(3, :) = FN1(5, :);
            NewMatrixNodes(4, :) = FN1(4, :);
            NewMatrixNodes(7, :) = FN2(7, :);
            NewMatrixNodes(8, :) = FN2(6, :);
            NewMatrixNodes(9, :) = FN2(5, :);
            NewMatrixNodes(10, :) = FN2(4, :);
            NewMatrixNodes(5, :)  = FN1(4, :) + (1 / 3) * (FN2(7, :) - FN1(4, :));
            NewMatrixNodes(6, :)  = FN1(4, :) + (2 / 3) * (FN2(7, :) - FN1(4, :)); 
            NewMatrixNodes(11, :) = FN2(4, :) + (1 / 3) * (FN1(7, :) - FN2(4, :));
            NewMatrixNodes(12, :) = FN2(4, :) + (2 / 3) * (FN1(7, :) - FN2(4, :));
            if isQuadSixteenNoded
                NewMatrixNodes = [NewMatrixNodes ; zeros(4, 2)];
                NewMatrixNodes(13, :) = FN2(5, :) + (2 / 3) * (FN1(6, :) - FN2(5, :));
                NewMatrixNodes(14, :) = FN1(5, :) + (1 / 3) * (FN2(6, :) - FN1(5, :));
                NewMatrixNodes(15, :) = FN1(5, :) + (2 / 3) * (FN2(6, :) - FN1(5, :));
                NewMatrixNodes(16, :) = FN2(5, :) + (1 / 3) * (FN1(6, :) - FN2(5, :));
            end
        end

        % Find location of 9th node for matrix:
        function NewNodalLocations = FindNinthNodeForQuad(NodalLocations)
            n2_test = (NodalLocations(1, :) + NodalLocations(3, :)) / 2;
            if(abs(NodalLocations(2, :) - n2_test)) < 1e-4
                nodeNine = (NodalLocations(4, :) + NodalLocations(8, :)) / 2;
            else
                nodeNine = (NodalLocations(2, :) + NodalLocations(6, :)) / 2;
            end
            NewNodalLocations = [NodalLocations ; nodeNine];
        end

        % Create a table of nodes for a certain element or assembly:
        function Nodes = CreateTableOfNodes(ObjectWithNodes, ~)
            Nodes = VectorToTable(ObjectWithNodes.NodalLocations, ObjectWithNodes.NumberOfNodes, ObjectWithNodes.NDOFPNode);
        end
    end
end


classdef FE_Mesh_2p5D_Reduced < FE_Mesh
    %2.5D Mesh with reduced order elements
    
    %% Properties
    properties (Constant)
        Type = "2p5D_Reduced";
        NumberOfDimensions = 3;
        NDOFPNode = 3;
        isReducedOrder = true;
    end

    properties
        GhostNode
    end
    
    %% Methods
    methods
        function obj = FE_Mesh_2p5D_Reduced(ListOfQuadrants)
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

        % Create Interior Triangle Element:
        function Element = BuildInteriorTriangleElement(obj, MaterialModel, NodalLocations)
            Element = obj.Build3NodedTriangle_2p5D(MaterialModel, TableToVec(NodalLocations));
        end

        % Build Fiber Element:
        function Element = BuildFiberElement(obj, MaterialModel, NodalLocations)
            Element = obj.Build4NodedTriangle_2p5D(MaterialModel, TableToVec(NodalLocations));
        end

        % Build Quad Matrix Element:
        function Element = BuildQuadMatrixElement(obj, MaterialModel, NodalLocations, ~)
            Element = obj.Build6NodedQuad_2p5D(MaterialModel, TableToVec(NodalLocations));
        end

        % Find Pinned Node:
        function FindPinnedNode(obj)
            MinDist = 1e10;
            Pin = 0;
            AllNodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NumberOfDimensions);
            for i = 1 : obj.NumberOfNodes - 1
                p1 = [0, 0, 0];
                p2 = AllNodes(i, :);
                Dist = MathMethods.CalcDistanceBetweenTwoPoints(p1, p2);
                if(Dist <= MinDist)
                    MinDist = Dist;
                    Pin = i;
                end
            end
            obj.PinnedNode = Pin;
        end

        % Assign ghost node location:
        function AssignGhostNodeAndLocation(obj)
            obj.GhostNode = obj.NumberOfNodes;
            PinnedNode = obj.PinnedNode;
            Nodes = VectorToTable(obj.NodalLocations, obj.NumberOfNodes, obj.NumberOfDimensions);
            Nodes(end, 2:3) = Nodes(PinnedNode, 2:3);
            obj.NodalLocations = TableToVec(Nodes);
        end
    end
        
    %% Static Methods
    methods (Static)
        % Create nodal locations for interior triangle elements:
        function NodalLocations = DetermineInteriorTriangleNodes(FiberMidpoints)
            NodalLocations = zeros(4, 2);
            NodalLocations(1, :) = FiberMidpoints(1, :);
            NodalLocations(2, :) = FiberMidpoints(2, :);
            NodalLocations(3, :) = FiberMidpoints(3, :);
            NodalLocations = [zeros(4, 1), NodalLocations]; % Add x-direction
            NodalLocations(4, 1) = 1; % OOP Node
        end

        % Create nodal locations for fiber elements:
        function NodalLocations = DetermineFiberNodeOrder(FN1, FN2, FN3, FiberRadius, isEdgeCCW)
             % FN = FiberNode
             NodalLocations = zeros(5, 2);
             NodalLocations(1, :) = FN1;
             if(isEdgeCCW)
                 NodalLocations(2, :) = FN3;
                 NodalLocations(4, :) = FN2;
             else
                 NodalLocations(2, :) = FN2;
                 NodalLocations(4, :) = FN3;
             end
             MidPointBetweenNodes2And4 = (NodalLocations(2, :) + NodalLocations(4, :)) / 2.0;
             MidPointVector = MathMethods.MakeVector2D(FN1, MidPointBetweenNodes2And4);
             MidPointAngle  = MathMethods.CalculateAngleBetweenVectors([1, 0], MidPointVector);
             Node3x = FN1(1) + FiberRadius * cos(MidPointAngle);
             Node3y = FN1(2) + FiberRadius * sin(MidPointAngle);
             NodalLocations(3, :) = [Node3x, Node3y];
             NodalLocations = [zeros(5, 1), NodalLocations]; % Add x-direction
             NodalLocations(5, 1) = 1; % OOP Node
        end

        % Create nodal locations for interior matrix elements:
        function NodalLocations = DetermineInteriorMatrixNodeOrder(FN1, FN2, isEdgeCCW)
             % FN = Fiber Nodes
             NodalLocations = zeros(7, 3);
             if(isEdgeCCW)
                 NodalLocations(1, :) = FN1(4, :);
                 NodalLocations(2, :) = FN1(3, :);
                 NodalLocations(3, :) = FN1(2, :);
                 NodalLocations(4, :) = FN2(4, :);
                 NodalLocations(5, :) = FN2(3, :);
                 NodalLocations(6, :) = FN2(2, :);
             else
                 NodalLocations(1, :) = FN2(4, :);
                 NodalLocations(2, :) = FN2(3, :);
                 NodalLocations(3, :) = FN2(2, :);
                 NodalLocations(4, :) = FN1(4, :);
                 NodalLocations(5, :) = FN1(3, :);
                 NodalLocations(6, :) = FN1(2, :);
             end
             NodalLocations(7, 1) = 1.0; % OOP Node
        end

        % Make sure fiber nodes are CCW:
        function FiberNodes = MakeSureFiberNodesAreCCW(FiberNodes, radius)
            V12 = MathMethods.MakeVector2D(FiberNodes(1, 2:3), FiberNodes(2, 2:3));
            V13 = MathMethods.MakeVector2D(FiberNodes(1, 2:3), FiberNodes(3, 2:3));
            T12_T13 = MathMethods.CalculateAngleBetweenVectors(V12, V13);
            T12 = MathMethods.CalculateAngleBetweenVectors([1, 0], V12);
            T13 = T12 + T12_T13;
            N3x = FiberNodes(1, 2) + radius * cos(T13);
            N3y = FiberNodes(1, 3) + radius * sin(T13);
            N3Check = Element.CheckNodeOverlap([N3x, N3y], FiberNodes(3, 2:3));
            if(N3Check)
                return
            else
                TempNodes = FiberNodes;
                TempNodes(2, :) = FiberNodes(4, :);
                TempNodes(4, :) = FiberNodes(2, :);
                FiberNodes = TempNodes;
            end
        end

        % Create a table of nodes for a certain element or assembly
        function Nodes = CreateTableOfNodes(ObjectWithNodes, nDim)
            % nDim = 2: Return table of just 2D coordinates of nodes
            % nDim = 3: Return table of full 3D coordinates of nodes
            if(nDim == 2)
                Nodes = VectorToTable(ObjectWithNodes.NodalLocations, ObjectWithNodes.NumberOfNodes, ObjectWithNodes.NDOFPNode);
                Nodes(:, 1) = [];
                Nodes(end, :) = [];
            elseif(nDim == 3)
                Nodes = VectorToTable(ObjectWithNodes.NodalLocations, ObjectWithNodes.NumberOfNodes, ObjectWithNodes.NDOFPNode);
            else
                error("CreateTableOfNodes (2.5D_Reduced Mesh): nDim input must be 2 or 3")
            end
        end
    end
end


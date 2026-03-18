classdef Triad < handle
    % Triad
    
    %% Properties
    properties
        Number
        Fibers
        isBoundary
        Edges
        isEdgeBoundary
        TriadsWhichShareEdges
        FibersWhichOverlapTriad
        willInteriorElementHaveOverlapWithFiber
    end
    
    %% Methods
    methods
        % Constructor
        function obj = Triad(number, fibers)
            obj.Number = number;
            obj.Fibers = fibers;
            obj.isBoundary = false;
            obj.isEdgeBoundary = ones(3, 1);
            obj.TriadsWhichShareEdges = zeros(3, 1);
            obj.FibersWhichOverlapTriad = zeros(3, 1);
            obj.willInteriorElementHaveOverlapWithFiber = zeros(3, 1);
            obj.InitializeEdges;
        end

        % Initialize Triad Edges:
        function InitializeEdges(obj)
            obj.Edges(1, :) = [obj.Fibers{1}.Number, obj.Fibers{2}.Number];
            obj.Edges(2, :) = [obj.Fibers{1}.Number, obj.Fibers{3}.Number];
            obj.Edges(3, :) = [obj.Fibers{2}.Number, obj.Fibers{3}.Number];
        end

        % Get fiber numbers in triad:
        function FiberNumbers = GetFiberNumbers(obj)
            FiberNumbers = [obj.Fibers{1}.Number, obj.Fibers{2}.Number, obj.Fibers{3}.Number];
        end

        % Check if another triad shares an edge. Also determine if this
        % triad is a boundary triad:
        function AreTriadsConnectedAndIsTriadBoundary(obj, OtherTriadFibers, OtherTriadNumber)
            for i = 1 : 3
                % Check if edge is found in other triad:
                FiberPairInTriad = obj.isFiberPairInTriad(obj.Edges(i, :), OtherTriadFibers);
                if(FiberPairInTriad)
                    obj.TriadsWhichShareEdges(i) = OtherTriadNumber;
                    obj.isEdgeBoundary(i) = false;
                end
            end
        end

        % Check if triad is a boundary triad after figuring out the edge pairs:
        function CheckIfTriadIsBoundaryAfterFindingEdgePairs(obj)
            if(~all(obj.TriadsWhichShareEdges))
                obj.isBoundary = true;
                obj.TriadsWhichShareEdges(obj.TriadsWhichShareEdges == 0) = [];
            end
        end

        % Calculate all fiber midpoints in the triad:
        function FiberMidPoints = CalculateFiberMidpoints(obj)
            FiberMidPoints = zeros(3, 2);
            possilbeOverlapFlag = false;
            shiftCenterDueToOverlap = false;
            overlapIdx = [];
            if(any(obj.willInteriorElementHaveOverlapWithFiber))
                possilbeOverlapFlag = true;
                overlapIdx = find(obj.willInteriorElementHaveOverlapWithFiber);
            end
            for i = 1 : 3
                if(possilbeOverlapFlag); shiftCenterDueToOverlap = ~obj.willInteriorElementHaveOverlapWithFiber(i); end
                isMiddleEdge = false;
                otherIndices = setdiff(1:3, i);
                FiberOrder = [obj.Fibers{i}, obj.Fibers{otherIndices(1)}, obj.Fibers{otherIndices(2)}];
                if(i == 2); isMiddleEdge = true; end
                FiberMidPoints(i, :) = obj.CalculateMidPointBetweenTwoEdges(FiberOrder, isMiddleEdge, shiftCenterDueToOverlap, overlapIdx, i);
            end
        end

        % Calculate midpoint between two edges:
        function Midpoint = CalculateMidPointBetweenTwoEdges(obj, Fibers, isMiddleEdge, shiftDueToOverlap, overlapIdx, idx)
            unit = [1, 0];
            Midpoint = zeros(1, 2);
            % Get the three fibers of interest:
            FiberA = Fibers(1);
            FiberB = Fibers(2);
            FiberC = Fibers(3);
            % Create vectors from FiberA -> FiberB and FiberA -> FiberC
            VAB = MathMethods.MakeVector2D(FiberA.Center, FiberB.Center);
            VAC = MathMethods.MakeVector2D(FiberA.Center, FiberC.Center);
            if(isMiddleEdge)
                T_Unit = MathMethods.CalculateAngleBetweenVectors(VAC, unit);
            else
                T_Unit = MathMethods.CalculateAngleBetweenVectors(VAB, unit);
            end
            TAB_AC = MathMethods.CalculateAngleBetweenVectors(VAB, VAC);
            if(~shiftDueToOverlap)
                TM = T_Unit + TAB_AC / 2;
            else
                TM = obj.AdjustMidPointDueToOverlap(T_Unit, TAB_AC, overlapIdx, idx);
            end
            % Calculate midpoints of interior triangle element:
            Midpoint(1) = FiberA.Center(1) + FiberA.Radius * cos(TM);
            Midpoint(2) = FiberA.Center(2) + FiberA.Radius * sin(TM);
        end

        % Output array of which triads are connected to current triad
        function ArrayOfConnectedTriads = CreateArrayOfConnectedTriads(obj)
            ArrayOfConnectedTriads = zeros(length(obj.TriadsWhichShareEdges), 2);
            for i = 1 : length(obj.TriadsWhichShareEdges)
                ArrayOfConnectedTriads(i, :) = [obj.Number, obj.TriadsWhichShareEdges(i)];
            end
        end

        % Check if an edge is in a CCW or CW order (For bulding fiber/matrix elements):
        function isEdgeCCW = CheckIfSharedEdgeIsCCWOrder(obj, SharedEdge)
            TriadConnect = [obj.Fibers{1}.Number, obj.Fibers{2}.Number, obj.Fibers{3}.Number];
            idx1 = find(TriadConnect == SharedEdge(1));
            idx2 = find(TriadConnect == SharedEdge(2));
            if(idx1 == 1 && idx2 == 3)
                isEdgeCCW = false;
            elseif(idx1 < idx2 || idx1 == length(TriadConnect) && idx2 == 1)
                isEdgeCCW = true;
            else
                isEdgeCCW = false;
            end
        end

        % Determine if triad has fiber overlap:
        function DetermineIfFibersOverlapTriad(obj)
            % Make vectors between each fiber:
            FiberA = obj.Fibers{1}; FiberB = obj.Fibers{2}; FiberC = obj.Fibers{3};
            FA = FiberA.Center; FB = FiberB.Center; FC = FiberC.Center;
            VAB = MathMethods.MakeVector2D(FA, FB); VBA = MathMethods.MakeVector2D(FB, FA);
            VAC = MathMethods.MakeVector2D(FA, FC); VCA = MathMethods.MakeVector2D(FC, FA);
            VBC = MathMethods.MakeVector2D(FB, FC); VCB = MathMethods.MakeVector2D(FC, FB);
            % Calculate angle between vectors:
            TAB_AC = MathMethods.CalculateAngleBetweenVectors(VAB, VAC);
            TBA_BC = MathMethods.CalculateAngleBetweenVectors(VBA, VBC);
            TCA_CB = MathMethods.CalculateAngleBetweenVectors(VCA, VCB);
            % Calculate T1 and T3 for each fiber:
            T1_A = MathMethods.CalculateAngleBetweenVectors(VAB, [1, 0]); T3_A = T1_A + TAB_AC;
            T1_B = MathMethods.CalculateAngleBetweenVectors(VBC, [1, 0]); T3_B = T1_B + TBA_BC;
            T1_C = MathMethods.CalculateAngleBetweenVectors(VCA, [1, 0]); T3_C = T1_C + TCA_CB;
            % Calculate angle representing shortest distance between a fiber the opposite triad base:
            Tdiff_ABase =  -atan((FB(1) - FC(1))/(FB(2) - FC(2)));
            Tdiff_BBase =  -atan((FC(1) - FA(1))/(FC(2) - FA(2)));
            Tdiff_CBase =  -atan((FA(1) - FB(1))/(FA(2) - FB(2)));
            % Calculate minimum distances between each fiber and opposite triad base:
            dmin_A = obj.CalculateMinimumDistanceBetweenFiberAndOppositeTriadBase(Tdiff_ABase,T1_A,T3_A,FA,FB,FC);
            dmin_B = obj.CalculateMinimumDistanceBetweenFiberAndOppositeTriadBase(Tdiff_BBase,T1_B,T3_B,FB,FC,FA);
            dmin_C = obj.CalculateMinimumDistanceBetweenFiberAndOppositeTriadBase(Tdiff_CBase,T1_C,T3_C,FC,FA,FB);
            % Check which fibers overlap triad:
            obj.doesFiberHaveOverlapWithTriad(FiberA, dmin_A, 1, 20);
            obj.doesFiberHaveOverlapWithTriad(FiberB, dmin_B, 2, 20);
            obj.doesFiberHaveOverlapWithTriad(FiberC, dmin_C, 3, 20);
            if(any(obj.FibersWhichOverlapTriad)); return; end
            obj.willInteriorElementHaveToBeAdjusted(FiberA, dmin_A, 1, 2);
            obj.willInteriorElementHaveToBeAdjusted(FiberB, dmin_B, 2, 2);
            obj.willInteriorElementHaveToBeAdjusted(FiberC, dmin_C, 3, 2);
        end

        % Check if fiber has overlap with triad:
        function doesFiberHaveOverlapWithTriad(obj, FibertoCheck, FiberToBaseDist, idx, factor)
            MinDist = FibertoCheck.Radius + FibertoCheck.Radius / factor;
            if(FiberToBaseDist <= MinDist)
                obj.FibersWhichOverlapTriad(idx) = FibertoCheck.Number;
            else
                obj.FibersWhichOverlapTriad(idx) = false;
            end
        end

        % Check if interior element will need to be adjusted
        function willInteriorElementHaveToBeAdjusted(obj, FibertoCheck, FiberToBaseDist, idx, factor)
            MinDist = FibertoCheck.Radius + FibertoCheck.Radius / factor;
            if(FiberToBaseDist <= MinDist)
                obj.willInteriorElementHaveOverlapWithFiber(idx) = true;
            end
        end

        % Find fiber pairs for edges (OG and Projected):
        function [OGProjDir, PairCount] = FindEdgeFiberPairs(obj, OGProjDir, PairCount)
            if(~obj.isBoundary); return; end
            EdgeCheck = [obj.Fibers{1}.OGEdge, obj.Fibers{2}.OGEdge, obj.Fibers{3}.OGEdge];
            if(sum(EdgeCheck) == 0); return; end
            TriadNumbers = [obj.Fibers{1}.Number, obj.Fibers{2}.Number, obj.Fibers{3}.Number];
            LocalEdges = zeros(3, 2);
            for i = 1 : 3
                LocalEdges(i, 1) = find(TriadNumbers == obj.Edges(i, 1));
                LocalEdges(i, 2) = find(TriadNumbers == obj.Edges(i, 2));
            end
            for i = 1 : 3
                if(~obj.isEdgeBoundary(i)); continue; end
                if(sum(obj.Fibers{LocalEdges(i, 1)}.OGEdge) == 0 || sum(obj.Fibers{LocalEdges(i, 2)}.OGEdge) == 0); continue; end
                [hasTopRightProj, hasTopLeftProj]  = obj.DoesEdgeHaveTopLeftOrTopRightProjection(LocalEdges(i, :));
                if(hasTopRightProj || hasTopLeftProj)
                    continue
                end
                Fiber1 = obj.Fibers{LocalEdges(i, 1)};
                Fiber2 = obj.Fibers{LocalEdges(i, 2)};
                Edge1 = Fiber1.OGEdge;
                Edge2 = Fiber2.OGEdge;
                idx1 = obj.DetermineIndexOfProjectionPair(Edge1, Edge2);
                idx2 = obj.DetermineIndexOfProjectionPair(Edge2, Edge1);
                Proj1 = Fiber1.FiberPairs(idx1);
                Proj2 = Fiber2.FiberPairs(idx2);
                dir = Fiber1.OGEdge(idx1);
                [OGProjDir, PairCount] = obj.UpdateOGProjDirTableForEdges(Fiber1.Number, Fiber2.Number, Proj1, Proj2, dir, OGProjDir, PairCount);
            end
        end

        % Find fiber pairs for corners (OG and Projected):
        function [OGProjDir, PairCount] = FindCornerFiberPairs(obj, OGProjDir, PairCount)
            if(~obj.isBoundary); return; end
            TriadNumbers = [obj.Fibers{1}.Number, obj.Fibers{2}.Number, obj.Fibers{3}.Number];
            LocalEdges = zeros(3, 2);
            for i = 1 : 3
                LocalEdges(i, 1) = find(TriadNumbers == obj.Edges(i, 1));
                LocalEdges(i, 2) = find(TriadNumbers == obj.Edges(i, 2));
            end
            for i = 1 : 3
                [hasTopRightProj, hasTopLeftProj]  = obj.DoesEdgeHaveTopLeftOrTopRightProjection(LocalEdges(i, :));
                if(hasTopRightProj || hasTopLeftProj)
                    Fiber1 = obj.Fibers{LocalEdges(i, 1)};
                    Fiber2 = obj.Fibers{LocalEdges(i, 2)};
                    if(hasTopRightProj)
                        Proj1 = Fiber1.FiberPairs(find(Fiber1.isFiberPairATopRightProjection));
                        Proj2 = Fiber2.FiberPairs(find(Fiber2.isFiberPairATopRightProjection));
                    elseif(hasTopLeftProj)
                        Proj1 = Fiber1.FiberPairs(find(Fiber1.isFiberPairATopLeftProjection));
                        Proj2 = Fiber2.FiberPairs(find(Fiber2.isFiberPairATopLeftProjection));
                    end
                    [OGProjDir, PairCount] = obj.UpdateOGProjDirTableForCorners(Fiber1.Number, Fiber2.Number, Proj1, Proj2, OGProjDir, PairCount, hasTopRightProj, hasTopLeftProj);
                end
            end
        end

        % Determine if two fibers are inside triad:
        function TriadFlag = AreTheseTwoFibersPartOfThisTriad(obj, FN1, FN2)
            TriadFlag = false;
            FibersInTriad = [obj.Fibers{1}.Number, obj.Fibers{2}.Number, obj.Fibers{3}.Number];
            if(sum(ismember([FN1, FN2], FibersInTriad)) == 2)
                TriadFlag = true;
            end
        end

        % Check if triad edge has a top left or top right projection:
        function [TopRight, TopLeft] = DoesEdgeHaveTopLeftOrTopRightProjection(obj, LocalEdges)
            TopRight = false; TopLeft = false;
            FiberA = obj.Fibers{LocalEdges(1)};
            FiberB = obj.Fibers{LocalEdges(2)};
            if(FiberA.hasTopRightProjection && FiberB.hasTopRightProjection)
                TopRight = true;
                return
            end
            if(FiberA.hasTopLeftProjection && FiberB.hasTopLeftProjection)
                TopLeft = true;
                return
            end
        end
    end

    %% Static Methods
    methods (Static)
        % Check if a fiber pair exists in another triad:
        function FiberInTriad = isFiberPairInTriad(Pair, TriadFibers)
            FiberInTriad = false;
            if(sum(ismember(Pair, TriadFibers)) == 2)
                FiberInTriad = true;
            end
        end
    end

    %% Private Static Methods
    methods (Static, Access = private)
        % Adjust fiber midpoint for interior triangle if there is possible
        % overlap
        function TM = AdjustMidPointDueToOverlap(T_Unit, TAB_AC, overlapIdx, idx)
            switch overlapIdx
                case 1
                    if(idx == 2)
                        TM = T_Unit;
                    elseif(idx == 3)
                        TM = T_Unit + TAB_AC;
                    end
                case 2
                    if(idx == 1)
                        TM = T_Unit + TAB_AC;
                    elseif(idx == 3)
                        TM = T_Unit;
                    end
                case 3
                    if(idx == 1)
                        TM = T_Unit; 
                    elseif(idx == 2)
                        TM = T_Unit + TAB_AC;
                    end
            end
        end

        % Calculate distance between fiber and opposite triad base (For determining if triad has fiber overlap):
        function dmin = CalculateMinimumDistanceBetweenFiberAndOppositeTriadBase(T_diff,T1,T3,p1,p2,p3)
            xa = p1(1); ya = p1(2);
            xb = p2(1); yb = p2(2);
            xc = p3(1); yc = p3(2);
            T_diff_pi = T_diff + pi;
            T_diff_2pi = T_diff + 2*pi;
            if(T_diff >= T1 && T_diff <= T3)
                T = T_diff;
            elseif(T_diff_pi >= T1 && T_diff_pi <= T3)
                T = T_diff_pi;
            elseif(T_diff_2pi >= T1 && T_diff_2pi <= T3)
                T = T_diff_2pi;
            % If shortest distance isnt between the triad edges, then the shortest distance is one of the edges:
            else
                T = [T1, T3];
                d_1 = ((xa + (ya - yb - xa*tan(T1) + (xb*(yb - yc))/(xb - xc))/(tan(T1) - (yb - yc)/(xb - xc)))^2 + (ya - (tan(T1)*(yb - (xb*(yb - yc))/(xb - xc)) - ((ya - xa*tan(T1))*(yb - yc))/(xb - xc))/(tan(T1) - (yb - yc)/(xb - xc)))^2)^(1/2);
                d_3 = ((xa + (ya - yb - xa*tan(T3) + (xb*(yb - yc))/(xb - xc))/(tan(T3) - (yb - yc)/(xb - xc)))^2 + (ya - (tan(T3)*(yb - (xb*(yb - yc))/(xb - xc)) - ((ya - xa*tan(T3))*(yb - yc))/(xb - xc))/(tan(T3) - (yb - yc)/(xb - xc)))^2)^(1/2);
                dists = [d_1, d_3];
                minIdx = find(dists == min(dists), 2);
                T = T(minIdx);
            end
            % Calculate min distance:
            dmin = ((xa + (ya - yb - xa*tan(T) + (xb*(yb - yc))/(xb - xc))/(tan(T) - (yb - yc)/(xb - xc)))^2 + (ya - (tan(T)*(yb - (xb*(yb - yc))/(xb - xc)) - ((ya - xa*tan(T))*(yb - yc))/(xb - xc))/(tan(T) - (yb - yc)/(xb - xc)))^2)^(1/2);
        end

        % Update the fiber pair table for edge fibers:
        function [OGProjDir, PairCount] = UpdateOGProjDirTableForEdges(Fiber1, Fiber2, Proj1, Proj2, dir, OGProjDir, PairCount)
            PairCount = PairCount + 1;
            OGProjDir(PairCount, 1) = Fiber1;
            OGProjDir(PairCount, 2) = Fiber2;
            OGProjDir(PairCount, 3) = Proj1;
            OGProjDir(PairCount, 4) = Proj2;
            OGProjDir(PairCount, 5) = dir;
        end

        % Update the fiber pair table for corner fibers:
        function [OGProjDir, PairCount] = UpdateOGProjDirTableForCorners(Fiber1, Fiber2, Proj1, Proj2, OGProjDir, PairCount, hasTopRightProj, hasTopLeftProj)
            PairCount = PairCount + 1;
            OGProjDir(PairCount, 1) = Fiber1;
            OGProjDir(PairCount, 2) = Fiber2;
            OGProjDir(PairCount, 3) = Proj1;
            OGProjDir(PairCount, 4) = Proj2;
            if(hasTopLeftProj)
                OGProjDir(PairCount, 5) = 3;
                return
            end
            if(hasTopRightProj)
                OGProjDir(PairCount, 5) = 4;
                return
            end
        end

        % Check if fiber has multiple fiber pairs but is not part of left or bottom edge:
        function isBottomRightCorner = CheckIfFiberIsBottomRightCorner(Fiber)
            isBottomRightCorner = false;
            if(length(Fiber.OGEdge) > 1)
                if(sum(Fiber.OGEdge) == 0 || sum(Fiber.OGEdge) == 2)
                    if(Fiber.Center(1) > Fiber.Center(2))
                       isBottomRightCorner = true;
                    end
                end
            end
        end

        % Find index of OG Edge for determining correct projection pair:
        function idx = DetermineIndexOfProjectionPair(EdgeA, EdgeB)
            for i = 1 : length(EdgeA)
                if(EdgeA(i) == 0); continue; end
                for j = 1 : length(EdgeB)
                    if(EdgeB(j) == 0); continue; end
                    if(EdgeA(i) == EdgeB(j))
                        idx = i;
                        return
                    end
                end
            end
        end

        % If more than one bottom right fiber are found, determine which
        % has the largest X value:
        function BotRightIdx = GetBottomRightFiberIfMoreThanOneAreFound(Fibers, BotRightIdx)
            FibersInTriad = [Fibers{1}.Number, Fibers{2}.Number, Fibers{3}.Number];
            FiberX = zeros(length(BotRightIdx), 1);
            FN = zeros(length(BotRightIdx), 1);
            for i = 1 : length(BotRightIdx)
                FiberX(i) = Fibers{BotRightIdx(i)}.Center(1);
                FN(i) = Fibers{BotRightIdx(i)}.Number;
            end
            [~, Idx] = max(FiberX);
            BotRightIdx = find(FN(Idx) == FibersInTriad);
        end

        % Find Triad edge index to determine if it's along the boundary:
        function EdgeIdx = DetermineEdgeIdx(Edges, FNA, FNB)
            % FN = Fiber Number
            EdgeIdx = 0;
            for i = 1 : 3
                if(ismember(FNA, Edges(i, :)) && ismember(FNB, Edges(i, :)))
                    EdgeIdx = i;
                end
            end
        end
    end
end


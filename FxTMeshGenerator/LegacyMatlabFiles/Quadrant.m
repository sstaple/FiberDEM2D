classdef Quadrant < handle
    % Quadrant
    
    %% Properties
    properties
        NumberOfFibers
        ListOfFibers
        QuadrantNumber
        Corners
        isProjection
        isQuadrantToKeep
    end
    
    %% Methods
    methods
        % Constructor
        function obj = Quadrant(QuadNumber, Fibers, Corners)
            [~, nFib] = size(Fibers);
            [mCorn, nCorn] = size(Corners);
            if(nFib ~= 4)
                error('Error in Quadrant Constructor: ListOfFibers must have 4 columns: X, Y, Radius, isVoid');
            end
            if(mCorn ~= 2 || nCorn ~= 2)
                error('Error in Quadrant Constructor: Corners must be a 2x2 (Row 1 = Bot Left. Row 2 = Top Right)');
            end
            NumberOfFibers = length(Fibers(:, 1));
            obj.QuadrantNumber = QuadNumber;
            obj.NumberOfFibers = NumberOfFibers;
            obj.Corners = Corners;
            obj.ListOfFibers = cell(1, NumberOfFibers);
            for i = 1 : NumberOfFibers
                Center = [Fibers(i, 1), Fibers(i, 2)];
                Radius = Fibers(i, 3);
                isVoid = Fibers(i, 4);
                FiberNumber = (QuadNumber - 1) * NumberOfFibers + i;
                obj.ListOfFibers{i} = Fiber(FiberNumber, Center, Radius, isVoid);
            end
        end

        % Plot boundary of quadrant:
        function PlotBoundary(obj)
            xmin = obj.Corners(1, 1);
            ymin = obj.Corners(1, 2);
            width = obj.Corners(2, 1) - obj.Corners(1, 1);
            height = obj.Corners(2, 2) - obj.Corners(1, 2);
            rectangle('Position', [xmin, ymin, width, height], 'LineWidth', 1.5, 'EdgeColor', 'm')
        end
    end

    %% Static Methods
    methods (Static)
        % Create 9 Quadrants of the same RVE for triangulation:
        function [ListOfQuadrants, AllFibers] = CreateQuadrantsOfFibersForTriangulation(ListOfFibers, RVEBoundary)
            ListOfQuadrants = cell(1, 9);
            for i = 1:9
                % Find the offset of this quadrant:
                Offset = Quadrant.GetQuadrantOffset(i);

                % Find fiber positions in quadrant:
                FibersInQuadrant = ListOfFibers;
                FibersInQuadrant(:, 1) = FibersInQuadrant(:, 1) + Offset(1) * RVEBoundary(1);
                FibersInQuadrant(:, 2) = FibersInQuadrant(:, 2) + Offset(2) * RVEBoundary(2);

                % Find bottom left and top right corners of quadrant:
                BotLeftCornerOfQuadrant = [RVEBoundary(1) * Offset(1), RVEBoundary(2) * Offset(2)];
                TopRightCornerOfQuadrant = [BotLeftCornerOfQuadrant(1) + RVEBoundary(1), BotLeftCornerOfQuadrant(2) + RVEBoundary(2)];
                QuadrantCorners = [BotLeftCornerOfQuadrant ; TopRightCornerOfQuadrant];

                % Create quadrant:
                ListOfQuadrants{i} = Quadrant(i, FibersInQuadrant, QuadrantCorners);
                ListOfQuadrants{i}.DetermineIfQuadrantToKeep(Offset);
                ListOfQuadrants{i}.DetermineIfQuadrantIsProjection(Offset);
                ListOfQuadrants{i}.AssignProjectionAndQuadrantToFibers;
            end
            % Create a list of all the fibers:
            AllFibers = cell(1, 9 * ListOfQuadrants{1}.NumberOfFibers);
            count = 0;
            for i = 1 : 9
                for j = 1 : ListOfQuadrants{1}.NumberOfFibers
                    count = count + 1;
                    AllFibers{count} = ListOfQuadrants{i}.ListOfFibers{j};
                end
            end
        end

        % Table containing Quadrant offset projections:
        function row = GetQuadrantOffset(idx)
             table = zeros(9, 2);
             table(1, :) = [0, 0];
             table(2, :) = [-1, 1];
             table(3, :) = [0, 1];
             table(4, :) = [1, 1];
             table(5, :) = [-1, 0];
             table(6, :) = [1, 0];
             table(7, :) = [-1, -1];
             table(8, :) = [0, -1];
             table(9, :) = [1, -1];
             row = table(idx, :);
        end

        % Plot all fibers from quadrants:
        function PlotQuadrantBoundaries(ListOfQuadrants, QuadrantsToPlot)
            if(strcmp(QuadrantsToPlot.lower, "all"))
                for i = 1 : length(ListOfQuadrants)
                    ListOfQuadrants{i}.PlotBoundary;
                end
            elseif(strcmp(QuadrantsToPlot.lower, "og"))
                ListOfQuadrants{1}.PlotBoundary;
            else
                error('Error in PlotQuadrantBoundaries: Second input must either be "ALL/all" or "OG/og" (Original)')
            end
            grid on
            axis equal
        end
    end

    %% Private Methods
    methods (Access = private)
        % Determine if quadrant number is 1 through 6 (These contain fibers we keep after triangulation):
        function DetermineIfQuadrantToKeep(obj, Offset)
            obj.isQuadrantToKeep = true;
            x0 = Offset(1);
            y0 = Offset(2);
            if(x0 == -1 && y0 == 0 || x0 == -1 && y0 == -1 || x0 == 0 && y0 == -1 || x0 == 1 && y0 == -1)
                obj.isQuadrantToKeep = false;
            end
        end

        % Determine whether this quadrant is a projection
        function DetermineIfQuadrantIsProjection(obj, Offset)
            obj.isProjection = true;
            x0 = Offset(1);
            y0 = Offset(2);
            if(x0 == 0 && y0 == 0)
                obj.isProjection = false;
            end
        end

        % Assign fiber as projection and if it's in quadrant 1 through 6:
        function AssignProjectionAndQuadrantToFibers(obj)
            for i = 1 : obj.NumberOfFibers
                obj.ListOfFibers{i}.isProjection = obj.isProjection;
                obj.ListOfFibers{i}.inQuadrantToKeep = obj.isQuadrantToKeep;
            end
        end
    end
end


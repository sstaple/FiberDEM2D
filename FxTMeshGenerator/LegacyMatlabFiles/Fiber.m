classdef Fiber < handle
    % Fiber
    
    %% Properties
    properties
        Center
        Radius
        Number
        NumberOfFiberPairs
        FiberPairs
        doesFiberHavePairs
        isProjection
        inQuadrantToKeep
        ElementNumbersInFiber
        isInBottomLeftTriad
        isFiberPairATopRightProjection
        isFiberPairATopLeftProjection
        hasTopRightProjection
        hasTopLeftProjection
        OGEdge
        isVoid
    end
    
    %% Methods
    methods
        % Constructor
        function obj = Fiber(number, center, radius, isvoid)
            obj.Number = number;
            obj.Center = center;
            obj.Radius = radius;
            obj.NumberOfFiberPairs = 0;
            obj.FiberPairs = zeros(1, 3);
            obj.doesFiberHavePairs = false;
            obj.ElementNumbersInFiber = [];
            obj.isInBottomLeftTriad = false;
            obj.isFiberPairATopLeftProjection = [];
            obj.isFiberPairATopRightProjection = [];
            obj.hasTopRightProjection = false;
            obj.hasTopLeftProjection = false;
            obj.OGEdge = [];
            obj.isVoid = isvoid;
        end
        
        % Add element number to fiber (To determine which fiber numbers
        % contain which element numbers):
        function AddElementNumberToFiber(obj, ElementNumber)
            obj.ElementNumbersInFiber = [obj.ElementNumbersInFiber, ElementNumber];
        end

        % Reduce element numbers in fiber
        function ReduceElementNumbersInFiber(obj, count)
            obj.ElementNumbersInFiber = obj.ElementNumbersInFiber - count;
        end

        % Check if fiber has pairs:
        function CheckIfFiberHasPairs(obj)
            if(~isempty(obj.FiberPairs))
                obj.doesFiberHavePairs = true;
            else
                obj.UpdateNoEdge;
            end
        end

        % Get element numbers in fiber:
        function ElementNumbersInFiber = GetElementNumbersInFiber(obj)
            ElementNumbersInFiber = obj.ElementNumbersInFiber;
        end

        % Update list of edges fiber is on (Right):
        function UpdateRightEdge(obj)
            obj.OGEdge = [obj.OGEdge, 1];
        end

        % Update list of edges fiber is on (Top):
        function UpdateTopEdge(obj)
            obj.OGEdge = [obj.OGEdge, 2];
        end

        % Assign no edge:
        function UpdateNoEdge(obj)
            obj.OGEdge = [obj.OGEdge, 0];
        end

        % Initialize top left pair projections:
        function InitializeCornerProjections(obj)
            obj.isFiberPairATopRightProjection = zeros(1, length(obj.FiberPairs));
            obj.isFiberPairATopLeftProjection = zeros(1, length(obj.FiberPairs));
        end
        
        % Plot fiber
        function PlotFiber(obj, n, PlotNumber)
            theta = linspace(0, 2*pi, n);
            x = zeros(1, n);
            y = zeros(1, n);
            for i = 1 : n
                x(i) = obj.Center(1) + obj.Radius * cos(theta(i));
                y(i) = obj.Center(2) + obj.Radius * sin(theta(i));
            end
            plot(x, y, 'r-', 'LineWidth', 1)
            if(PlotNumber)
              hold on
              text(obj.Center(1), obj.Center(2), num2str(obj.Number), "FontSize", 8)
            end
        end
    end
end


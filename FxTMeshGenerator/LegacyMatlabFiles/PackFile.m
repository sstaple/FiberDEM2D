classdef PackFile < InputOutput
    % Reading in pack file
    
    %% Methods
    methods
        % Constructor
        function obj = PackFile(fullDirec)
            obj.FullDirectory = fullDirec;
        end
        
        % Load in fibers and RVE dimensions from pack file
        function [ListOfFibers, RVEBoundary] = LoadPackFile(obj)
            table = readmatrix(obj.FullDirectory);
            ListOfFibers = zeros(length(table(:, 1)), 4);
            nanrow = 0;
            FiberCount = 0;
            for i = 1 : length(ListOfFibers(:, 1))
                % If row is all nans, move to next
                [nanflag, nanrow] = obj.CheckIfRowIsNAN(table(i, :), nanrow);
                if(nanflag)
                    continue
                end
                % If nanrow = 2, read in RVE length and break out
                if(~isnan(table(i, 5)) && ~isnan(table(i, 6)))
                    RVEBoundary = [table(i,5), table(i,6)];
                    break
                end
                FiberCount = FiberCount + 1;
                ListOfFibers(FiberCount, 1) = table(i, 1);
                ListOfFibers(FiberCount, 2) = table(i, 2);
                ListOfFibers(FiberCount, 3) = table(i, 3);
                if(isnan(table(i, 4)))
                    ListOfFibers(FiberCount, 4) = 0;
                else
                    ListOfFibers(FiberCount, 4) = 1;
                end
            end
            RowsToKeep = any(ListOfFibers, 2);
            ListOfFibers = ListOfFibers(RowsToKeep, :);

            % Delete fibers outside of RVE boundary:
            FibersToKeep = ones(length(ListOfFibers(:, 1)), 1);
            for i = 1 : length(ListOfFibers(:, 1))
                FibersToKeep(i) = obj.CheckIfFiberIsInsideOfBoundary([ListOfFibers(i, 1), ListOfFibers(i, 2)], RVEBoundary);
            end
            RowsToKeep = any(FibersToKeep, 2);
            ListOfFibers = ListOfFibers(RowsToKeep, :);
        end
    end

    %% Private Static Methods
    methods (Static, Access = private)
        % Check if a row in the pack file contains all nans:
        function [nanflag, nanrow] = CheckIfRowIsNAN(row, nanrow)
            nanflag = false;
            if(all(isnan(row)))
                nanflag = true;
                nanrow = nanrow + 1;
            end
        end

        % Check if a fiber is outside of RVE boundary:
        function KeepFiber = CheckIfFiberIsInsideOfBoundary(Center, Boundary)
              KeepFiber = 1;
              isInside = (Center(1) >= 0.0) && (Center(1) <= Boundary(1)) && (Center(2) >= 0.0) && (Center(2) <= Boundary(2));
              if(~isInside)
                  KeepFiber = 0;
              end
        end
    end
end


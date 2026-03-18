function [] = RVEFill_V2(assembly)
    for i = 1:length(assembly.ListOfElements)
        element = assembly.ListOfElements{i};
        nodes_tbl = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NDOFPNode); % Nodes in a table (x, y)
        if(strcmp(element.ElementPhase, "Fiber"))
            color = [0.5, 0.5, 0.5];
        end
        if(strcmp(element.ElementPhase, "Matrix"))
            a = strcmp(element.ElementPhase, "Matrix") && element.NumberOfNodes == 4;
            if(strcmp(element.IsometricShape,"Tri") || a)
            color = [254, 204, 51] / 256;
            end
            if(strcmp(element.IsometricShape,"Quad") && ~ a)
                color = [244, 218, 144] / 256;
            end
        end
        if(assembly.NumberOfDimensions == 2)
            fill3(nodes_tbl(:, 1), nodes_tbl(:, 2), 1e2*ones(length(nodes_tbl(:, 1))), color, 'EdgeColor', 'none', 'FaceAlpha', 1)
        else
            fill3(nodes_tbl(1:end-1, 2), nodes_tbl(1:end-1, 3), 1e2*ones(length(nodes_tbl(1:end-1, 1))), color, 'EdgeColor', 'none', 'FaceAlpha', 1)
        end
        hold on
    
    
    %replot the fibers only
    if(strcmp(element.ElementPhase, "Fiber"))
        nodes = VectorToTable(element.NodalLocations,element.NumberOfNodes,element.NDOFPNode);
        if(assembly.NumberOfDimensions == 2)
            P1 = nodes(1,:);
            P2 = nodes(3,:);
            P3 = nodes(5,:);
        else
            P1 = nodes(1, 2:3);
            P2 = nodes(3, 2:3);
            P3 = nodes(5, 2:3);
        end
        angle1 = atan2(P2(2) - P1(2), P2(1) - P1(1));
        angle2 = atan2(P3(2) - P1(2), P3(1) - P1(1));
        radius = pdist([P1;P3],'euclidean');

        if angle1 > angle2
            angle2 = angle2 + 2*pi;
        end
        
        theta = linspace(angle1, angle2, 100);
        x = [P1(1), P1(1) + radius * cos(theta), P1(1)];
        y = [P1(2), P1(2) + radius * sin(theta), P1(2)];
        z = repelem(101,length(x));
        fill3(x, y, z,[0.5, 0.5, 0.5],'EdgeColor','none');
    end


    end
    axis equal
    view(2)
end

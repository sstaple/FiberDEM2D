function [] = RVEFill(assembly)
    for i = 1:length(assembly.ListOfElements)
        element = assembly.ListOfElements{i};
        nodes_tbl = VectorToTable(element.NodalLocations, element.NumberOfNodes, element.NDOFPNode); % Nodes in a table (x, y)
        if(strcmp(element.ElementPhase, "Fiber"))
            color = [0.5, 0.5, 0.5];
        elseif(strcmp(element.ElementPhase, "Matrix"))
            color = [244, 218, 144] / 256;
        end
        if(assembly.NumberOfDimensions == 2)
            fill3(nodes_tbl(:, 1), nodes_tbl(:, 2), 1e2*ones(length(nodes_tbl(:, 1))), color, 'EdgeColor', [0, 0, 0], 'FaceAlpha', 1)
        else
            fill3(nodes_tbl(1:end-1, 2), nodes_tbl(1:end-1, 3), 1e2*ones(length(nodes_tbl(1:end-1, 1))), color, 'EdgeColor', [0, 0, 0], 'FaceAlpha', 1)
        end
        hold on
    end
    axis equal
    view(2)
end
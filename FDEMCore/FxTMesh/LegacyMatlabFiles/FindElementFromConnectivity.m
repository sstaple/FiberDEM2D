function Element = FindElementFromConnectivity(assembly, Nodes)
   connect = assembly.Connectivity;
   for i = 1 : assembly.NumberOfElements
       NodesInElem = connect(i, :);
       if(all(ismember(Nodes, NodesInElem)))
           Element = i; 
           return
       end
   end
end


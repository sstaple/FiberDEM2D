namespace FxTMeshGenerator.Meshing
{
    /// <summary>
    /// Configuration for element node counts.
    /// </summary>
    public sealed class ElementConfig
    {
        /// <summary>Number of nodes in quadrilateral elements</summary>
        public int QuadNodes { get; init; }
        
        /// <summary>Number of nodes in triangular elements</summary>
        public int TriangleNodes { get; init; }

        /// <summary>Predefined configuration: Simple (Quad=6, Triangle=3)</summary>
        public static ElementConfig Simple => new() { QuadNodes = 6, TriangleNodes = 3 };
        
        /// <summary>Predefined configuration: Standard (Quad=8, Triangle=6)</summary>
        public static ElementConfig Standard => new() { QuadNodes = 8, TriangleNodes = 6 };
        
        /// <summary>Predefined configuration: High Order (Quad=12, Triangle=9)</summary>
        public static ElementConfig HighOrder => new() { QuadNodes = 12, TriangleNodes = 9 };
        
        /// <summary>Predefined configuration: Very High Order (Quad=16, Triangle=12)</summary>
        public static ElementConfig VeryHighOrder => new() { QuadNodes = 16, TriangleNodes = 12 };
    }
}

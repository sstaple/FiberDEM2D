using System;

namespace FDEMCore.FxTMesh.Meshing
{
    public enum FxTElementFamily
    {
        Type1 = 1,
        Type2 = 2,
        Type3 = 3,
        Type4 = 4,
        Type5 = 5
    }

    public sealed class ElementConfig
    {
        public FxTElementFamily Family { get; init; } = FxTElementFamily.Type2;

        public int FiberTriangleNodes => Family switch
        {
            FxTElementFamily.Type1 => 6,
            FxTElementFamily.Type2 => 6,
            FxTElementFamily.Type3 => 6,
            FxTElementFamily.Type4 => 9,
            FxTElementFamily.Type5 => 9,
            _ => throw new NotSupportedException()
        };

        public int MatrixTriangleNodes => Family switch
        {
            FxTElementFamily.Type1 => 3,
            FxTElementFamily.Type2 => 6,
            FxTElementFamily.Type3 => 6,
            FxTElementFamily.Type4 => 9,
            FxTElementFamily.Type5 => 9,
            _ => throw new NotSupportedException()
        };

        public int MatrixQuadNodes => Family switch
        {
            FxTElementFamily.Type1 => 6,
            FxTElementFamily.Type2 => 8,
            FxTElementFamily.Type3 => 9,
            FxTElementFamily.Type4 => 12,
            FxTElementFamily.Type5 => 16,
            _ => throw new NotSupportedException()
        };

        public string FiberTriangleFxTType => FiberTriangleNodes switch
        {
            6 => "2DT6.4",
            9 => "2DT9",
            _ => throw new NotSupportedException()
        };

        public string MatrixTriangleFxTType => MatrixTriangleNodes switch
        {
            3 => "2DT3",
            6 => "2DT6.4",
            9 => "2DT9",
            _ => throw new NotSupportedException()
        };

        public string MatrixQuadFxTType => MatrixQuadNodes switch
        {
            6 => "2DQ6",
            8 => "2DQ8.9",
            9 => "2DQ9",
            12 => "2DQ12",
            16 => "2DQ16",
            _ => throw new NotSupportedException()
        };

        public static ElementConfig Type1 => new() { Family = FxTElementFamily.Type1 };
        public static ElementConfig Type2 => new() { Family = FxTElementFamily.Type2 };
        public static ElementConfig Type3 => new() { Family = FxTElementFamily.Type3 };
        public static ElementConfig Type4 => new() { Family = FxTElementFamily.Type4 };
        public static ElementConfig Type5 => new() { Family = FxTElementFamily.Type5 };

        public static ElementConfig Standard => Type2;
    }
}
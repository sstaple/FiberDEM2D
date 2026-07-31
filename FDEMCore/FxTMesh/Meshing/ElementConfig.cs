using FDEMCore.FxTMesh.Meshing.Elements;
using System;

namespace FDEMCore.FxTMesh.Meshing
{
    public enum FxTElementFamily
    {
        Type2 = 2,
        Type3 = 3,
        Type4 = 4,
        Type5 = 5,
        Type2_2p5 = 22,
        Type3_2p5 = 23,
        Type4_2p5 = 24,
        Type5_2p5 = 25,
    }
    public static class ElementBuilderProvider
    {
        public static IElementBuilder Create(FxTElementFamily config)
        {
            return config switch
            {
                FxTElementFamily.Type2 => new ElementBuilderType2(),
                FxTElementFamily.Type3 => new ElementBuilderType3(),
                FxTElementFamily.Type4 => new ElementBuilderType4(),
                FxTElementFamily.Type5 => new ElementBuilderType5(),
                FxTElementFamily.Type2_2p5 => new ElementBuilderType2_2p5D(),
                FxTElementFamily.Type3_2p5 => new ElementBuilderType3_2p5D(),
                FxTElementFamily.Type4_2p5 => new ElementBuilderType4_2p5D(),
                FxTElementFamily.Type5_2p5 => new ElementBuilderType5_2p5D(),
                _ => throw new NotSupportedException()
            };
        }
    }
}
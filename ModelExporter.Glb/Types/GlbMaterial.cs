namespace ModelExporter.Glb.Types
{
    class GlbMaterial
    {
        public Metallic PbrMetallicRoughness { get; }
        public string AlphaMode { get; }
        public bool DoubleSided { get; }

        public GlbMaterial(Metallic pbrMetallicRoughness, string alphaMode, bool doubleSided)
        {
            PbrMetallicRoughness = pbrMetallicRoughness;
            AlphaMode = alphaMode;
            DoubleSided = doubleSided;
        }

        public GlbMaterial(double r, double g, double b, double a, double metallic, double roughness)
        {
            var baseColorFactor = new double[4] { r, g, b, a };
            PbrMetallicRoughness = new Metallic(baseColorFactor, metallic, roughness);
            AlphaMode = a < 1
                ? AlphaModesTypes.BLEND
                : AlphaModesTypes.OPAQUE;
            DoubleSided = true;
        }
    }

    class Metallic
    {
        public double[] BaseColorFactor { get; }
        public double MetallicFactor { get; }
        public double RoughnessFactor { get; }

        public Metallic(double[] baseColorFactor, double metallicFactor, double roughnessFactor)
        {
            BaseColorFactor = baseColorFactor;
            MetallicFactor = metallicFactor;
            RoughnessFactor = roughnessFactor;
        }
    }

    static class AlphaModesTypes
    {
        public const string OPAQUE = "OPAQUE";

        public const string MASK = "MASK";

        public const string BLEND = "BLEND";
    }
}

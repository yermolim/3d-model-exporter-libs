using System.Numerics;

namespace ModelExporter.Core.ModelDataTypes.Structs
{
    public readonly struct Trs
    {
        public Vector3 Translation { get; }

        public Quaternion Rotation { get; }

        public Vector3 Scale { get; }

        public Trs(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }
    }
}

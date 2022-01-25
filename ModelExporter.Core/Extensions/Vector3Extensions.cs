using System.Numerics;

namespace ModelExporter.Core.Extensions
{
    public static class Vector3Extensions
    {
        public static float[] ToArray(this Vector3 vector)
        {
            return new float[3] { vector.X, vector.Y, vector.Z };
        }
    }
}

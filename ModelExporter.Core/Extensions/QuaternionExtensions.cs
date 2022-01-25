using System.Numerics;

namespace ModelExporter.Core.Extensions
{
    public static class QuaternionExtensions
    {
        public static float[] ToArray(this Quaternion quaternion)
        {
            return new float[4] { quaternion.X, quaternion.Y, quaternion.Z, quaternion.W };
        }
    }
}

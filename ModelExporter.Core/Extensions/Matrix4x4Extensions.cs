using System.Numerics;

namespace ModelExporter.Core.Extensions
{
    public static class Matrix4x4Extensions
    {
        public static readonly Matrix4x4 yUpRhTransform = new Matrix4x4(
            1, 0, 0, 0,
            0, 0, -1, 0,
            0, 1, 0, 0,
            0, 0, 0, 1
        );

        public static float[] ToArrayRowFirst(this Matrix4x4 matrix)
        {
            return new float[16] 
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44,
            };
        }

        public static float[] ToArrayRowFirstYUp(this Matrix4x4 matrix)
        {
            var yUpMatrix = Matrix4x4.Multiply(matrix, yUpRhTransform);
            return yUpMatrix.ToArrayRowFirst();
        }
    }
}

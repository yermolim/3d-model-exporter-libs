using System;
using System.Linq;
using System.Numerics;

namespace ModelExporter.Core.ModelDataTypes
{
    public class Face
    {
        public Vector3[] Vertices { get; }

        public Face(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length < 3)
            {
                throw new ArgumentException("Polygon must have at least 3 vertices");
            }

            Vertices = vertices;
        }
    }
}
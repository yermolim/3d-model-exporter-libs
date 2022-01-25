using ModelExporter.Core.ModelDataTypes;

using Poly2Tri.Triangulation.Polygon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ModelExporter.Triangulation
{
    public class Triangulator
    {
        private readonly Face _face;
        private readonly Dictionary<Vector2, Vector3> _verticesSourceMap = new Dictionary<Vector2, Vector3>();
        private readonly Vector2[] _faceVertices;
        private readonly Vector2[][] _holesVertices;

        private bool HasHoles { get => _holesVertices != null; }

        public Triangulator(Face face, Face[] holes)
        {
            _face = face ?? throw new ArgumentException("No face provided");

            if (face.Vertices.Length > 4 
                || (holes != null && holes.Length > 0))
            {
                var (faceVertices, holesVertices) = Convert3dVerticesTo2d(face, holes);
                _faceVertices = faceVertices;
                _holesVertices = holesVertices;
            }
        }

        public Vector3[] Triangulate()
        {
            if (!HasHoles)
            {
                if (_face.Vertices.Length == 3)
                {
                    return _face.Vertices;
                }
                if (_face.Vertices.Length == 4)
                {
                    return new Vector3[6]
                    {
                        _face.Vertices[0],
                        _face.Vertices[1],
                        _face.Vertices[2],
                        _face.Vertices[2],
                        _face.Vertices[3],
                        _face.Vertices[0]
                    };
                }
            }

            var mainPolygonPoints = _faceVertices.Select(x => new PolygonPoint(x.X, x.Y)).ToArray();
            var mainPolygon = new Polygon(mainPolygonPoints);
            if (HasHoles)
            {
                foreach (var holeVertices in _holesVertices)
                {
                    var holePolygonPoints = holeVertices.Select(x => new PolygonPoint(x.X, x.Y)).ToArray();
                    var holePolygon = new Polygon(holePolygonPoints);
                    mainPolygon.AddHole(holePolygon);
                }
            }

            Poly2Tri.P2T.Triangulate(mainPolygon);
            var triangles = mainPolygon.Triangles;
            var triangulatedVertices = triangles
                .Select(x => x.Points)
                .SelectMany(x => x)
                .Select(x => new Vector2((float)x.X, (float)x.Y))
                .ToList();

            var outputVertices = triangulatedVertices
                .Select(x => Retrieve3dVertexByPlanar(x))
                .ToArray();

            return outputVertices;
        }

        private (Vector2[] faceVertices, Vector2[][] holesVertices) Convert3dVerticesTo2d(Face face, Face[] holes)
        {
            var holeIndices = new List<int>();
            var allVertices = new List<Vector3>();
            allVertices.AddRange(face.Vertices);
            if (holes != null && holes.Length > 0)
            {
                foreach (var hole in holes)
                {
                    holeIndices.Add(allVertices.Count);
                    allVertices.AddRange(hole.Vertices);
                }
            }

            var allVertices2d = TransformVerticesTo2d(allVertices.ToArray());
            for (int i = 0; i < allVertices.Count; i++)
            {
                if (_verticesSourceMap.ContainsKey(allVertices2d[i]))
                {
                    throw new ArgumentException("Face not simple: self-intersections found)");
                }
                _verticesSourceMap.Add(allVertices2d[i], allVertices[i]);
            }

            if (holeIndices.Count > 0)
            {
                var faceVertices2d = allVertices2d
                    .Skip(0)
                    .Take(holeIndices[0])
                    .ToArray();
                var holesVertices2d = new Vector2[holeIndices.Count][];
                for (int i = 0; i < holeIndices.Count; i++)
                {
                    var index = holeIndices[i];
                    var length = holes[i].Vertices.Length;
                    holesVertices2d[i] = allVertices2d
                        .Skip(index)
                        .Take(length)
                        .ToArray();
                }
                return (faceVertices2d, holesVertices2d);
            }

            return (allVertices2d, null);
        }

        private Vector3 Retrieve3dVertexByPlanar(Vector2 planarVertex)
        {
            return _verticesSourceMap[planarVertex];
        }

        /// <summary>
        /// Projects all vertices to their common plane (removes z coordinate)
        /// </summary>
        /// <param name="vertices">vertices 3d array. all vertices must belong to same plane!</param>
        /// <returns>vertices 2d array</returns>
        private Vector2[] TransformVerticesTo2d(Vector3[] vertices)
        {
            var axisRemovalResult = TryRemoveAxisFromFaceVertices(vertices, out Vector2[] vertices2d);
            if (!axisRemovalResult)
            {
                vertices2d = ProjectFaceVerticesToPlane(vertices);
            }

            return vertices2d;
        }

        /// <summary>
        /// Projects vertices to plane by removing third axis coord.
        /// Operation succeds only if face is parallel to one of axes
        /// </summary>
        /// <param name="vector3s"></param>
        /// <param name="vector2s"></param>
        /// <returns></returns>
        private bool TryRemoveAxisFromFaceVertices(Vector3[] vector3s, out Vector2[] vector2s)
        {
            vector2s = null;

            if (vector3s.Select(x => x.Z).Distinct().Count() == 1) // face is parallel to Z
            {
                vector2s = vector3s.Select(x => new Vector2(x.X, x.Y)).ToArray();
                return true;
            }
            if (vector3s.Select(x => x.Y).Distinct().Count() == 1) // face is parallel to Y
            {
                vector2s = vector3s.Select(x => new Vector2(x.X, x.Z)).ToArray();
                return true;
            }
            if (vector3s.Select(x => x.X).Distinct().Count() == 1) // face is parallel to X
            {
                vector2s = vector3s.Select(x => new Vector2(x.Y, x.Z)).ToArray();
                return true;
            }

            return false;
        }

        private Vector3 GetTriangleNormal(Vector3 a, Vector3 b, Vector3 c, bool inverted = false)
        {
            Vector3 A;
            Vector3 B;
            Vector3 C;
            if (inverted)
            {
                A = c;
                B = b;
                C = a;
            }
            else
            {
                A = a;
                B = b;
                C = c;
            }

            var AB = Vector3.Subtract(B, A);
            var AC = Vector3.Subtract(C, A);
            var normal = Vector3.Normalize(Vector3.Cross(AB, AC));

            if (float.IsNaN(normal.X))
            {
                throw new ArgumentException("Vertices lay on the same line");
            }

            return normal;
        }

        private bool CheckIfVerticesNotCollinear(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length < 3)
            {
                throw new ArgumentNullException("Less than 3 vertices provided");
            }

            var A = vertices[0];
            var B = vertices[1];
            var C = vertices[2];

            // test points
            //var A = new Vector3(279.267365f, 1280.74573f, 1856.18262f);
            //var B = new Vector3(-265.957672f, -1285.019f, -1616.15247f);
            //var C = new Vector3(-273.475037f, -1320.3949f, -1664.02771f);

            var AB = Vector3.Distance(B, A);
            var BC = Vector3.Distance(C, B);
            var AC = Vector3.Distance(C, A);

            // use rounding to prevent float precision errors
            var diff = Math.Abs(AB + BC - AC);
            var isOnTheSameLine = diff < 0.001;
            return !isOnTheSameLine;
        }

        /// <summary>
        /// Get first three non-collinear points from points array
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private (Vector3 A, Vector3 B, Vector3 C) GetFirstTriangleVertices(Vector3[] vertices)
        {
            var isTriangle = false;
            var selectedIndices = new int[3] { 0, 1, 2 };
            while (!isTriangle)
            {
                var verticesToCheck = new Vector3[3]
                {
                    vertices[selectedIndices[0]],
                    vertices[selectedIndices[1]],
                    vertices[selectedIndices[2]]
                };
                isTriangle = CheckIfVerticesNotCollinear(verticesToCheck);
                if (isTriangle)
                {
                    break;
                }

                selectedIndices[0] += 1;
                selectedIndices[1] += 1;
                selectedIndices[2] += 1;
                if (selectedIndices[2] >= vertices.Length)
                {
                    throw new Exception("All face vertices lay on the same line");
                }
            }

            var A = vertices[selectedIndices[0]];
            var B = vertices[selectedIndices[1]];
            var C = vertices[selectedIndices[2]];

            return (A, B, C);
        }

        /// <summary>
        /// Projects vertices to their common plane (removes z coordinate).
        /// All points must lay on the same plane.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private Vector2[] ProjectFaceVerticesToPlane(Vector3[] vertices)
        {
            // get first 3 non-collinear points
            var (A, B, C) = GetFirstTriangleVertices(vertices);

            // x axis analog
            var AU = Vector3.Normalize(Vector3.Subtract(B, A));
            // z axis analog
            var AN = GetTriangleNormal(A, B, C);
            // y axis analog
            var AV = Vector3.Normalize(Vector3.Cross(AU, AN));

            // basis points for transformations (+ point A)
            var U = Vector3.Add(A, AU);
            var V = Vector3.Add(A, AV);
            var N = Vector3.Add(A, AN);

            // matrix with target coords for A,U,V,N points
            var goalMatrix = new Matrix4x4(
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1,
                1, 1, 1, 1);
            // matrix with initial coords for A,U,V,N points
            var basisMatrix = new Matrix4x4(
                A.X, U.X, V.X, N.X,
                A.Y, U.Y, V.Y, N.Y,
                A.Z, U.Z, V.Z, N.Z,
                1, 1, 1, 1);
            var invertionResult = Matrix4x4.Invert(basisMatrix, out var basisMatrixInverted);
            // affine transformation matrix to apply to points
            var transform = Matrix4x4.Multiply(goalMatrix, basisMatrixInverted);
            // translate transformation matrix columns to rows because Vector3.Transform uses row-based approach
            var transformTranslated = new Matrix4x4(
                transform.M11,
                transform.M21,
                transform.M31,
                0,
                transform.M12,
                transform.M22,
                transform.M32,
                0,
                transform.M13,
                transform.M23,
                transform.M33,
                0,
                transform.M14,
                transform.M24,
                transform.M34,
                1);

            // testVector must be close to 0,0,0. testMatrix must be close to goal matrix
            //var testVector = Vector3.Transform(A, transformTranslated);
            //var testMatrix = Matrix4x4.Multiply(transform, basisMatrix);

            var transformedVertices = vertices
                .Select(x => Vector3.Transform(x, transformTranslated))
                .ToArray();

            var vertices2d = transformedVertices
                .Select(x => new Vector2(x.X, x.Y))
                .ToArray();

            return vertices2d;
        }
    }
}

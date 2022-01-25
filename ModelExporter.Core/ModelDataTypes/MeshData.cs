using ModelExporter.Core.ModelDataTypes.Structs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ModelExporter.Core.ModelDataTypes
{
    public class MeshData : IEquatable<MeshData>
    {
        public static MeshData Combine(IList<MeshData> meshes)
        {
            if (meshes == null || meshes.Count == 0)
            {
                throw new ArgumentNullException("No input meshes provided");
            }
            if (meshes.Count == 1)
            {
                return meshes[0];
            }
            if (meshes.Select(x => x.Color).Distinct().Count() != 1)
            {
                throw new ArgumentException("Input meshes have different colors and can't be combined");
            }

            var positions = meshes.Select(x => x.Positions).SelectMany(x => x).ToArray();

            var normals = meshes[0].Normals != null
                ? meshes.Select(x => x.Normals).SelectMany(x => x).ToArray()
                : null;

            if (normals != null && positions.Length != normals.Length)
            {
                throw new ArgumentException("Meshes total positions count is not equal to normals count");
            }

            return normals != null
                ? new MeshData(positions, normals, meshes[0].Color)
                : new MeshData(positions, meshes[0].Color);
        }

        public static bool TryCombine(IList<MeshData> meshes, out MeshData combinedMesh)
        {
            try
            {
                combinedMesh = Combine(meshes);
            }
            catch
            {
                combinedMesh = null;
            }

            return combinedMesh != null;
        }

        public Vector3[] Positions { get; }
        public Vector3[] Normals { get; }
        public PbrColor Color { get; }

        public bool HasNormals { get => Normals != null && Normals.Length == Positions.Length; }

        public MeshData(IList<Vector3> positions, IList<Vector3> normals, PbrColor color)
        {
            if (positions == null)
            {
                throw new ArgumentNullException("Positions list can't be null");
            }
            if (positions.Count == 0 
                || positions.Count % 3 != 0)
            {
                throw new ArgumentException("Positions count must divide by three");
            }

            if (normals == null
                || normals.Count != positions.Count)
            {
                throw new ArgumentException("Normals count must be equal to positions count");
            }

            Positions = positions.ToArray();
            Normals = normals.ToArray();
            Color = color;
        }

        public MeshData(IList<Vector3> positions, PbrColor color)
        {
            if (positions == null)
            {
                throw new ArgumentNullException("Positions list can't be null");
            }
            if (positions.Count == 0
                || positions.Count % 3 != 0)
            {
                throw new ArgumentException("Positions count must divide by three");
            }

            Positions = positions.ToArray();
            Normals = null;
            Color = color;
        }

        public bool Equals(MeshData other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!Color.Equals(other.Color))
            {
                return false;
            }

            if (Positions == other.Positions && Normals == other.Normals)
            {
                return true;
            }

            if (HasNormals != other.HasNormals)
            {
                return false;
            }

            if (HasNormals)
            {
                return (Positions.SequenceEqual(other.Positions)
                    && Normals.SequenceEqual(other.Normals));
            }

            return Positions.SequenceEqual(other.Positions);
        }

        public MeshMinMax GetMinMax()
        {
            var PXs = new List<float>();
            var PYs = new List<float>();
            var PZs = new List<float>();
            foreach (var position in Positions)
            {
                PXs.Add(position.X);
                PYs.Add(position.Y);
                PZs.Add(position.Z);
            }
            var minPosition = new Vector3(PXs.Min(), PYs.Min(), PZs.Min());
            var maxPosition = new Vector3(PXs.Max(), PYs.Max(), PZs.Max());

            if (!HasNormals)
            {
                return new MeshMinMax(minPosition, maxPosition, null, null);
            }

            var NXs = new List<float>();
            var NYs = new List<float>();
            var NZs = new List<float>();
            foreach (var normal in Normals)
            {
                NXs.Add(normal.X);
                NYs.Add(normal.Y);
                NZs.Add(normal.Z);
            }
            var minNormal = new Vector3(NXs.Min(), NYs.Min(), NZs.Min());
            var maxNormal = new Vector3(NXs.Max(), NYs.Max(), NZs.Max());

            return new MeshMinMax(minPosition, maxPosition, minNormal, maxNormal);
        }

        public (Vector3[] positions, int[] positionIndices) GetDistinctPositions()
        {
            return DistinctVertices(Positions);
        }

        public (Vector3[] normals, int[] normalIndices) GetDistinctNormals()
        {
            return DistinctVertices(Normals);
        }

        private (Vector3[] vertices, int[] indices) DistinctVertices(IList<Vector3> inputVertices)
        {
            var map = new Dictionary<Vector3, int>();
            var indices = new int[inputVertices.Count];
            var distinctVertices = new List<Vector3>();

            for (int i = 0; i < inputVertices.Count; i++)
            {
                var v = Positions[i];
                if (map.TryGetValue(v, out int index))
                {
                    indices[i] = index;
                    continue;
                }
                map[v] = distinctVertices.Count;
                indices[i] = distinctVertices.Count;
                distinctVertices.Add(v);
            }

            return (distinctVertices.ToArray(), indices);
        }
    }

    public class MeshDataComparer : IEqualityComparer<MeshData>
    {
        public bool Equals(MeshData x, MeshData y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(MeshData obj)
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 17) + obj.Color.GetHashCode();
                hash = (hash * 17) + obj.Positions.Length.GetHashCode();
                if (obj.Positions.Length > 1)
                {
                    hash = (hash * 17) 
                        + ((int)(obj.Positions[0].X + obj.Positions[0].Y + obj.Positions[0].Z))
                        + ((int)(obj.Positions[obj.Positions.Length - 1].X + obj.Positions[obj.Positions.Length - 1].Y + obj.Positions[obj.Positions.Length - 1].Z));
                }
                if (obj.Normals != null)
                {
                    hash = (hash * 17) + obj.Normals.Length.GetHashCode();
                    if (obj.Normals.Length > 1)
                    {
                        hash = (hash * 17)
                            + ((int)(obj.Normals[0].X + obj.Normals[0].Y + obj.Normals[0].Z))
                            + ((int)(obj.Normals[obj.Normals.Length - 1].X + obj.Normals[obj.Normals.Length - 1].Y + obj.Normals[obj.Normals.Length - 1].Z));
                    }
                }
                return hash;
            }
        }
    }

    public class MeshMinMax
    {
        public Vector3 MinPosition { get; }
        public Vector3 MaxPosition { get; }
        public Vector3? MinNormal { get; }
        public Vector3? MaxNormal { get; }

        public MeshMinMax(Vector3 minPosition, Vector3 maxPosition, 
            Vector3? minNormal, Vector3? maxNormal)
        {
            MinPosition = minPosition;
            MaxPosition = maxPosition;
            MinNormal = minNormal;
            MaxNormal = maxNormal;
        }
    }
}

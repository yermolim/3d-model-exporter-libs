using ModelExporter.Core.ModelDataTypes.Structs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ModelExporter.Core.ModelDataTypes
{
    public class ItemData
    {        
        public static List<MeshData> DistinctItemsMeshes(IEnumerable<ItemData> items)
        {
            var uniqueMeshesSet = new HashSet<MeshData>(new MeshDataComparer());
            var uniqueMeshes = new List<MeshData>();
            foreach (var mesh in items.Select(x => x.Mesh))
            {
                if (!uniqueMeshesSet.Contains(mesh))
                {
                    uniqueMeshesSet.Add(mesh);
                    uniqueMeshes.Add(mesh);
                }
            }

            return uniqueMeshes;
        }

        public string Handle { get; }

        public MeshData Mesh { get; }

        public Matrix4x4? Transform { get; }

        public Trs? TRS { get; }

        public ItemData(string handle, MeshData mesh, Trs trs)
        {
            Handle = handle;
            Mesh = mesh ?? throw new ArgumentNullException("Mesh can't be null");
            Transform = null;
            TRS = trs;
        }

        public ItemData(string handle, MeshData mesh, Matrix4x4 transform)
        {
            Handle = handle;
            Mesh = mesh ?? throw new ArgumentNullException("Mesh can't be null");
            Transform = transform;
            TRS = null;
        }
    }
}

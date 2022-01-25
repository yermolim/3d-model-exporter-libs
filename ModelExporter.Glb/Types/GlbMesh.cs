using Newtonsoft.Json;

using System.Collections.Generic;

namespace ModelExporter.Glb.Types
{
    class GlbMesh
    {
        public List<GlbMeshPrimitive> Primitives { get; }

        public GlbMesh(List<GlbMeshPrimitive> primitives)
        {
            Primitives = primitives;
        }
    }

    class GlbMeshPrimitive
    {
        public GlbMeshAttributes Attributes { get; }

        public int Indices { get; }

        public int Material { get; }

        public GlbMeshPrimitive(GlbMeshAttributes attributes, int indices, int material)
        {
            Attributes = attributes;
            Indices = indices;
            Material = material;
        }
    }

    class GlbMeshAttributes
    {
        [JsonProperty(PropertyName = "POSITION")]
        public int Position { get; }

        [JsonProperty(PropertyName = "NORMAL")]
        public int? Normal { get; }

        public GlbMeshAttributes(int position, int? normal)
        {
            Position = position;
            Normal = normal;
        }
    }
}

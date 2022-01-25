using System.Collections.Generic;

namespace ModelExporter.Glb.Types
{
    class GlbModel
    {
        public GlbAsset Asset { get; }
        public int Scene { get; }
        public List<GlbScene> Scenes { get; }
        public List<GlbMaterial> Materials { get; }
        public List<GlbNode> Nodes { get; }
        public List<GlbMesh> Meshes { get; }
        public List<GlbAccessor> Accessors { get; }
        public List<GlbBufferView> BufferViews { get; }
        public List<GlbBuffer> Buffers { get; }

        public GlbModel() : 
            this(new List<GlbScene>(), new List<GlbMaterial>(),
                new List<GlbNode>(), new List<GlbMesh>(), new List<GlbAccessor>(),
                new List<GlbBufferView>(), new List<GlbBuffer>())
        {
        }

        public GlbModel(List<GlbScene> scenes, List<GlbMaterial> materials, 
            List<GlbNode> nodes, List<GlbMesh> meshes, List<GlbAccessor> accessors, 
            List<GlbBufferView> bufferViews, List<GlbBuffer> buffers) :
            this(new GlbAsset("Assistant Build GLB exporter", "2.0"), 0, 
                scenes, materials, nodes, meshes, accessors, bufferViews, buffers)
        {
        }

        public GlbModel(GlbAsset asset, 
            int scene,
            List<GlbScene> scenes, 
            List<GlbMaterial> materials, 
            List<GlbNode> nodes, 
            List<GlbMesh> meshes, 
            List<GlbAccessor> accessors, 
            List<GlbBufferView> bufferViews, 
            List<GlbBuffer> buffers)
        {
            Asset = asset;
            Scene = scene;
            Scenes = scenes;
            Materials = materials;
            Nodes = nodes;
            Meshes = meshes;
            Accessors = accessors;
            BufferViews = bufferViews;
            Buffers = buffers;
        }
    }
}

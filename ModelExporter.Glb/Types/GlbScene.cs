using System.Collections.Generic;

namespace ModelExporter.Glb.Types
{
    class GlbScene
    {
        public List<int> Nodes { get; } = new List<int>() { 0 };
        public string Name { get; }

        public GlbScene()
        {
        }

        public GlbScene(string name)
        {
            Name = name;
        }

        public GlbScene(List<int> nodes)
        {
            Nodes = nodes;
        }

        public GlbScene(List<int> nodes, string name) : this(nodes)
        {
            Name = name;
        }
    }
}

namespace ModelExporter.Glb.Types
{
    class GlbNode
    {
        public int Mesh { get; }

        public float[] Matrix { get; }

        public float[] Translation { get; }
        public float[] Rotation { get; }
        public float[] Scale { get; }

        public string Name { get; }

        public GlbNodeExtras Extras { get; }

        public GlbNode(int mesh, float[] matrix, string name) :
            this(mesh, matrix, null, null, null, name, null)
        {
        }

        public GlbNode(int mesh,
            float[] translation, float[] rotation, float[] scale,
            string name) : 
            this(mesh, null, translation, rotation, scale, name, null)
        {
        }

        public GlbNode(int mesh,
            float[] matrix, float[] translation, float[] rotation, float[] scale, 
            string name, GlbNodeExtras extras)
        {
            Mesh = mesh;
            Matrix = matrix;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
            Name = name;
            Extras = extras;
        }
    }

    class GlbNodeExtras
    {

    }
}

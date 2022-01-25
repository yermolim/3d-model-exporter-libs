namespace ModelExporter.Glb.Types
{
    class GlbBufferView
    {
        public TargetType Target { get; }

        public int Buffer { get; }

        public uint ByteOffset { get; }
        public uint ByteLength { get; }

        public GlbBufferView(TargetType target, int buffer, uint byteOffset, uint byteLength)
        {
            Target = target;
            Buffer = buffer;
            ByteOffset = byteOffset;
            ByteLength = byteLength;
        }
    }

    enum TargetType
    {
        Vertices = 34962,
        VertexIndices = 34963
    }
}

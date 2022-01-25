namespace ModelExporter.Glb.Types
{
    class GlbBuffer
    {
        public uint ByteLength { get; }
        public string Uri { get; }

        public GlbBuffer(uint byteLength, string uri )
        {
            ByteLength = byteLength;
            Uri = uri;
        }
    }
}

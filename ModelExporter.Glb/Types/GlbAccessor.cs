namespace ModelExporter.Glb.Types
{
    class GlbAccessor
    {
        public int BufferView { get; }

        public uint ByteOffset { get; }

        public int Count { get; }

        public string Type { get; }

        public ComponentType ComponentType { get; }

        public float[] Min { get; }

        public float[] Max { get; }

        private GlbAccessor(int bufferView, uint byteOffset, int count, 
            string accessorType, ComponentType componentType,
            float[] min, float[] max)
        {
            BufferView = bufferView;
            ByteOffset = byteOffset;
            Count = count;
            Type = accessorType;
            ComponentType = componentType;
            Min = min;
            Max = max;
        }

        public static GlbAccessor GetNewGlbScalarAccessor(int bufferView, uint byteOffset, int count, 
            ComponentType componentType,
            float[] min, float[] max)
        {
            return new GlbAccessor(bufferView, byteOffset, count,
            AccessorTypes.SCALAR, componentType, min, max);
        }
        public static GlbAccessor GetNewGlbVecAccessor(int bufferView, uint byteOffset, int count,
            ComponentType componentType,
            float[] min, float[] max)
        {
            return new GlbAccessor(bufferView, byteOffset, count,
            AccessorTypes.VEC3, componentType, min, max);
        }
    }

    enum ComponentType
    {
        Byte = 5120,
        Ubyte = 5121,
        Short = 5122,
        Ushort = 5123,
        Uint = 5125,
        Float = 5126
    }

    static class AccessorTypes
    {
        public const string SCALAR = "SCALAR";

        public const string VEC3 = "VEC3";
    }
}

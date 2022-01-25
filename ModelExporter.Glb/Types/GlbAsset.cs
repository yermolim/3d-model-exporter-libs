namespace ModelExporter.Glb.Types
{
    readonly struct GlbAsset
    {
        public string Generator { get; }
        public string Version { get; }

        public GlbAsset(string generator, string version)
        {
            Generator = generator;
            Version = version;
        }
    }
}

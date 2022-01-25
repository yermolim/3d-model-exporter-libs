namespace ModelExporter.Core.ModelDataTypes.Structs
{
    public readonly struct PbrColor
    {
        public static PbrColor White { get; } = new PbrColor(255, 255, 255, 255);

        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
        public double Metallic { get; }
        public double Roughness { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0.0 to 1.0</param>
        /// <param name="g">green: 0.0 to 1.0</param>
        /// <param name="b">blue: 0.0 to 1.0</param>
        /// <param name="a">alpha: 0.0 to 1.0</param>
        /// <param name="metallic">0.0 to 1.0</param>
        /// <param name="roughness">0.0 to 1.0</param>
        public PbrColor(double r, double g, double b, double a, double metallic, double roughness)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            Metallic = metallic;
            Roughness = roughness;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0 to 255</param>
        /// <param name="g">green: 0 to 255</param>
        /// <param name="b">blue: 0 to 255</param>
        /// <param name="a">alpha: 0.0 to 1.0</param>
        /// <param name="metallic">0.0 to 1.0</param>
        /// <param name="roughness">0.0 to 1.0</param>
        public PbrColor(byte r, byte g, byte b, double a, double metallic, double roughness)
            : this(r / 255.0, g / 255.0, b / 255.0, a, metallic, roughness) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0 to 255</param>
        /// <param name="g">green: 0 to 255</param>
        /// <param name="b">blue: 0 to 255</param>
        /// <param name="a">alpha: 0 to 255</param>
        /// <param name="metallic">0.0 to 1.0</param>
        /// <param name="roughness">0.0 to 1.0</param>
        public PbrColor(byte r, byte g, byte b, byte a, double metallic, double roughness)
            : this(r / 255.0, g / 255.0, b / 255.0, a / 255.0, metallic, roughness) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0.0 to 1.0</param>
        /// <param name="g">green: 0.0 to 1.0</param>
        /// <param name="b">blue: 0.0 to 1.0</param>
        /// <param name="a">alpha: 0.0 to 1.0</param>
        public PbrColor(double r, double g, double b, double a)
            : this(r, g, b, a, 0.0, 1.0) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0 to 255</param>
        /// <param name="g">green: 0 to 255</param>
        /// <param name="b">blue: 0 to 255</param>
        /// <param name="a">alpha: 0 to 255</param>
        public PbrColor(byte r, byte g, byte b, byte a)
            : this(r / 255.0, g / 255.0, b / 255.0, a / 255.0, 0.0, 1.0) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">red: 0 to 255</param>
        /// <param name="g">green: 0 to 255</param>
        /// <param name="b">blue: 0 to 255</param>
        /// <param name="a">alpha: 0.0 to 1.0</param>
        public PbrColor(byte r, byte g, byte b, double a)
            : this(r / 255.0, g / 255.0, b / 255.0, a, 0.0, 1.0) { }

        public double[] ToArrayColor()
        {
            return new double[4] { R, G, B, A };
        }

        public double[] ToArray()
        {
            return new double[6] { R, G, B, A, Metallic, Roughness };
        }
    }
}

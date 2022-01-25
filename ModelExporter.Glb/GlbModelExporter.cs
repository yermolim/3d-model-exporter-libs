using ModelExporter.Core.Exceptions;
using ModelExporter.Core.Extensions;
using ModelExporter.Core.Interfaces;
using ModelExporter.Core.ModelDataTypes;
using ModelExporter.Core.ModelDataTypes.Structs;
using ModelExporter.Glb.Types;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModelExporter.Glb
{
    /// <summary>
    /// Preview version of basic Glb exporter, that uses previously extracted model geometry data
    /// </summary>
    public class GlbModelExporter : IModelDataExporter, IItemsDataExporter
    {
        private static readonly string defaultCompressorFileName = "GlbCompressor.exe";

        public static byte[] ConvertItemsToGlb(IList<ItemData> items, string fileName)
        {
            byte[] glbBytes;
            using (var glbExporter = new GlbModelExporter())
            {
                glbBytes = glbExporter.Export(items, fileName);
            }
            return glbBytes;
        }

        public static byte[] ConvertItemsToGlb(IList<ItemData> items, string fileName, bool disableCompression)
        {
            byte[] glbBytes;
            using (var glbExporter = new GlbModelExporter(disableCompression))
            {
                glbBytes = glbExporter.Export(items, fileName);
            }
            return glbBytes;
        }

        private readonly ILogger _logger;

        private readonly bool _compressorDisabled;
        private readonly string _compressorExePath;

        public GlbModelExporter()
            : this(null, false, null) { }
        public GlbModelExporter(bool disableCompression) 
            : this(null, disableCompression, null) { }
        public GlbModelExporter(string glbCompressorExePath)
            : this(null, false, glbCompressorExePath) { }
        public GlbModelExporter(ILogger logger)
            : this(logger, false, null) { }
        public GlbModelExporter(ILogger logger, bool disableCompression) 
            : this(logger, disableCompression, null) { }
        public GlbModelExporter(ILogger logger, string glbCompressorExePath) 
            : this(logger, false, glbCompressorExePath) { }

        private GlbModelExporter(ILogger logger, bool disableCompression, string glbCompressorExePath)
        {
            _logger = logger;
            _compressorDisabled = disableCompression;
            _compressorExePath = glbCompressorExePath;
        }

        public byte[] Export(ModelData parsedModel)
        {
            return Export(parsedModel.Items, parsedModel.FileName);
        }

        public byte[] Export(IList<ItemData> parsedItems, string fileName)
        {
            _logger?.LogInformation("Exporting to GLB...");

            var uniqueMeshes = ItemData.DistinctItemsMeshes(parsedItems);

            var (bufferBytes, bufferInfosByMesh) = CreateGlbBuffer(uniqueMeshes);

            var glbModel = CreateGlbModelStructure(uniqueMeshes, parsedItems, fileName,
                bufferInfosByMesh, (uint)bufferBytes.Length);

            var jsonBytes = SerializeGlbModelToBytes(glbModel);

            var glb = PackGlb(bufferBytes, jsonBytes);

            if (!_compressorDisabled)
            {
                var compressedGlb = CompressGlb(glb);
                if (compressedGlb != null)
                {
                    glb = compressedGlb;
                }
            }

            _logger?.LogInformation("OK");

            return glb;
        }

        private (byte[] buffer, Dictionary<MeshData, BufferViewInfo> bufferInfosByMesh) 
            CreateGlbBuffer(IList<MeshData> parsedMeshesUnique)
        {
            int CountMeshDataByteLength(MeshData meshData)
            {
                var verticesCount = meshData.Positions.Length;

                var indicesLengthUnpadded = verticesCount <= 255
                    ? verticesCount * 1 // vertex indices * 1 byte per ubyte index
                    : verticesCount <= 65535
                        ? verticesCount * 2 // vertex indices * 2 bytes per ushort index
                        : verticesCount * 4; // vertex indices * 4 bytes per uint index

                var indicesLength = indicesLengthUnpadded % 4 == 0
                    ? indicesLengthUnpadded
                    : 4 - (indicesLengthUnpadded % 4) + indicesLengthUnpadded;

                var dataLength = meshData.HasNormals
                    ? verticesCount * 24
                    : verticesCount * 12;

                return indicesLength + dataLength;
            }

            int bufferLength = 0;
            try
            {
                bufferLength = checked(parsedMeshesUnique
                    .Select(x => CountMeshDataByteLength(x))
                    .Sum());
            }
            catch (OverflowException ex)
            {
                throw new BufferOverflowException(ex);
            }

            using (MemoryStream stream = new MemoryStream(bufferLength))
            {
                var bufferInfosByMesh = new Dictionary<MeshData, BufferViewInfo>();

                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    foreach (var mesh in parsedMeshesUnique)
                    {
                        // indices
                        IndicesType indicesType;
                        var indicesOffset = (uint)stream.Position;
                        var verticesCount = mesh.Positions.Length;
                        if (verticesCount <= 255)
                        {
                            indicesType = IndicesType.Ubyte;
                            for (int i = 0; i < verticesCount; i++)
                            {
                                writer.Write((byte)i);
                            }
                        }
                        else if (verticesCount <= 65535)
                        {
                            indicesType = IndicesType.Ushort;
                            for (int i = 0; i < verticesCount; i++)
                            {
                                writer.Write((ushort)i);
                            }
                        }
                        else
                        {
                            indicesType = IndicesType.Uint;
                            for (int i = 0; i < verticesCount; i++)
                            {
                                writer.Write((uint)i);
                            }
                        }
                        // write empty bytes if needed (position must divide by 4)
                        while (stream.Position % 4 != 0)
                        {
                            writer.Write((byte)0);
                        }

                        // positions
                        var positionsOffset = (uint)stream.Position;
                        foreach (var position in mesh.Positions)
                        {
                            writer.Write(position.X);
                            writer.Write(position.Y);
                            writer.Write(position.Z);
                        }

                        // normals
                        uint? normalsOffset = null;
                        if (mesh.HasNormals)
                        {
                            normalsOffset = (uint)stream.Position;
                            foreach (var normal in mesh.Normals)
                            {
                                writer.Write(normal.X);
                                writer.Write(normal.Y);
                                writer.Write(normal.Z);
                            }
                        }

                        bufferInfosByMesh.Add(mesh, 
                            new BufferViewInfo(verticesCount, indicesType, indicesOffset, positionsOffset, normalsOffset));
                    }
                }

                stream.Flush();
                var buffer = stream.GetBuffer();

                return (buffer, bufferInfosByMesh);
            }
        }

        #region model structure generation
        private GlbModel CreateGlbModelStructure(IList<MeshData> parsedMeshesUnique,
            IList<ItemData> parsedModelItems, string fileName,
            IDictionary<MeshData, BufferViewInfo> bufferInfosByMesh, uint bufferLength)
        {
            var buffers = CreateGlbBuffers(bufferLength);

            var (materials, materialIndicesByColor) = CreateGlbMaterials(parsedMeshesUnique);

            var (bufferViews, accessors, accessorIndicesByMesh) = CreateGlbBufferViewsWithAccessors(bufferInfosByMesh);

            var meshes = CreateGlbMeshes(parsedMeshesUnique, accessorIndicesByMesh, materialIndicesByColor);

            var nodes = CreateGlbNodes(parsedModelItems, parsedMeshesUnique);

            var scenes = CreateGlbScenes(nodes, fileName);

            var model = new GlbModel(scenes, materials, nodes, meshes, accessors, bufferViews, buffers);
            return model;
        }

        private List<GlbBuffer> CreateGlbBuffers(uint bufferLength)
        {
            var buffers = new List<GlbBuffer>();
            var buffer = new GlbBuffer(bufferLength, null);
            buffers.Add(buffer);

            return buffers;
        }

        private (List<GlbMaterial> materials, Dictionary<PbrColor, int> indicesByColor) 
            CreateGlbMaterials(IList<MeshData> parsedMeshesUnique)
        {
            var modelColors = parsedMeshesUnique
                .Select(x => x.Color)
                .Distinct()
                .ToList();

            var materials = new List<GlbMaterial>();
            var materialIndicesByColor = new Dictionary<PbrColor, int>();

            var matIndex = 0;
            foreach (var color in modelColors)
            {
                var material = new GlbMaterial(color.R, color.G, color.B, color.A, color.Metallic, color.Roughness);
                materials.Add(material);

                materialIndicesByColor.Add(color, matIndex++);
            }

            return (materials, materialIndicesByColor);
        } 

        private (List<GlbBufferView> bufferViews, List<GlbAccessor> accessors,
            Dictionary<MeshData, AccessorIndices> indicesByMesh)
            CreateGlbBufferViewsWithAccessors(IDictionary<MeshData, BufferViewInfo> bufferInfosByMesh)
        {
            var bufferViews = new List<GlbBufferView>();
            var accessors = new List<GlbAccessor>();
            var indicesByMesh = new Dictionary<MeshData, AccessorIndices>();

            var bvIndex = 0;
            var aIndex = 0;

            foreach (var kvp in bufferInfosByMesh)
            {
                var mesh = kvp.Key;
                var meshMinMax = mesh.GetMinMax();

                var bufferInfo = kvp.Value;
                var verticesCount = bufferInfo.VerticesCount;

                uint indicesLength = (uint)(verticesCount * (int)bufferInfo.IndicesType);
                ComponentType indicesComponentType = ComponentType.Ushort;
                switch (bufferInfo.IndicesType)
                {
                    case IndicesType.Ubyte:
                        indicesComponentType = ComponentType.Ubyte;
                        break;
                    case IndicesType.Ushort:
                        indicesComponentType = ComponentType.Ushort;
                        break;
                    case IndicesType.Uint:
                        indicesComponentType = ComponentType.Uint;
                        break;
                }

                var bufferViewIndices = new GlbBufferView(TargetType.VertexIndices, 0,
                    bufferInfo.IndicesOffset, indicesLength);
                bufferViews.Add(bufferViewIndices);

                var accessorIndices = GlbAccessor.GetNewGlbScalarAccessor(bvIndex, 0, verticesCount,
                    indicesComponentType, new float[] { 0 }, new float[] { verticesCount - 1 });
                accessors.Add(accessorIndices);
                var indicesIndex = aIndex++;

                bvIndex++;

                var bufferViewVertexPositions = new GlbBufferView(TargetType.Vertices, 0,
                    bufferInfo.PositionsOffset, (uint)verticesCount * 12);
                bufferViews.Add(bufferViewVertexPositions);

                var accessorVertexPositions = GlbAccessor.GetNewGlbVecAccessor(bvIndex, 0, verticesCount,
                    ComponentType.Float,
                    meshMinMax.MinPosition.ToArray(), 
                    meshMinMax.MaxPosition.ToArray());
                accessors.Add(accessorVertexPositions);
                var positionsIndex = aIndex++;

                bvIndex++;

                int? normalsIndex = null;
                if (mesh.HasNormals)
                {
                    var bufferViewVertexNormals = new GlbBufferView(TargetType.Vertices, 0,
                        bufferInfo.NormalsOffset.Value, (uint)verticesCount * 12);
                    bufferViews.Add(bufferViewVertexNormals);

                    var accessorVertexNormals = GlbAccessor.GetNewGlbVecAccessor(bvIndex, 0, verticesCount,
                        ComponentType.Float,
                        meshMinMax.MinNormal.Value.ToArray(), 
                        meshMinMax.MaxNormal.Value.ToArray());
                    accessors.Add(accessorVertexNormals);
                    normalsIndex = aIndex++;

                    bvIndex++;
                }

                indicesByMesh.Add(mesh, new AccessorIndices(indicesIndex, positionsIndex, normalsIndex));
            }

            return (bufferViews, accessors, indicesByMesh);
        }

        private List<GlbMesh> CreateGlbMeshes(IList<MeshData> parsedMeshesUnique,
            IDictionary<MeshData, AccessorIndices> accessorIndicesByMesh,
            IDictionary<PbrColor, int> materialIndicesByColor)
        {
            var meshes = new List<GlbMesh>();

            foreach (var parsedMesh in parsedMeshesUnique)
            {
                var primitives = new List<GlbMeshPrimitive>();
                var (indices, position, normal) = accessorIndicesByMesh[parsedMesh];
                var material = materialIndicesByColor[parsedMesh.Color];

                var attributes = new GlbMeshAttributes(position, normal);
                var primitive = new GlbMeshPrimitive(attributes, indices, material);
                primitives.Add(primitive);

                var mesh = new GlbMesh(primitives);
                meshes.Add(mesh);
            }

            return meshes;
        }

        private List<GlbNode> CreateGlbNodes(IList<ItemData> parsedModelItems, 
            IList<MeshData> parsedMeshesUnique)
        {
            var indexByUniqueMesh = new Dictionary<MeshData, int>(new MeshDataComparer());
            int index = 0;
            foreach (var uniqueMesh in parsedMeshesUnique)
            {
                indexByUniqueMesh[uniqueMesh] = index++;
            }

            var nodes = new List<GlbNode>();
            foreach (var parsedItem in parsedModelItems)
            {
                var meshIndex = indexByUniqueMesh[parsedItem.Mesh];
                var node = parsedItem.Transform.HasValue
                    ? new GlbNode(meshIndex, parsedItem.Transform.Value.ToArrayRowFirstYUp(), parsedItem.Handle)
                    : new GlbNode(meshIndex, 
                        parsedItem.TRS.Value.Translation.ToArray(), 
                        parsedItem.TRS.Value.Rotation.ToArray(), 
                        parsedItem.TRS.Value.Scale.ToArray(),
                        parsedItem.Handle);
                nodes.Add(node);
            }

            return nodes;
        }

        private List<GlbScene> CreateGlbScenes(IList<GlbNode> nodes, string name)
        {
            var scenes = new List<GlbScene>();
            var scene = new GlbScene(nodes.Select((x, i) => i).ToList(), name);
            scenes.Add(scene);

            return scenes;
        }
        #endregion

        private byte[] SerializeGlbModelToBytes(GlbModel glbModel)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                //Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                    {
                        OverrideSpecifiedNames = false
                    }
                }
            };
            var glbJson = JsonConvert.SerializeObject(glbModel, jsonSettings);

            var bytes = Encoding.UTF8.GetBytes(glbJson);

            return bytes;
        }

        private byte[] PackGlb(byte[] buffer, byte[] json)
        {
            var bufferLength = buffer.Length; // buffer % 4 == 0
            var jsonPadding = 4 - (json.Length % 4);
            var jsonLength = json.Length + jsonPadding;

            int glbLength;
            try
            {
                glbLength = 12 + 8 + jsonLength + 8 + bufferLength;
            }
            catch (OverflowException ex)
            {
                throw new BufferOverflowException(ex);
            }

            using (MemoryStream stream = new MemoryStream(glbLength))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((uint)0x46546C67); // 'glTF' ASCII encoded
                    writer.Write((uint)2); // version 2.0
                    writer.Write((uint)glbLength); // file length

                    writer.Write((uint)(jsonLength)); // json chunk length
                    writer.Write((uint)0x4E4F534A); // chunk type ('JSON' ASCII encoded)
                    writer.Write(json); // json glTF data
                    for (int i = 0; i < jsonPadding; i++) // write spaces if needed to align chunk end
                    {
                        writer.Write(' ');
                    }

                    writer.Write((uint)(bufferLength)); // binary buffer chunk length
                    writer.Write((uint)0x004E4942); // chunk type ('BIN' ASCII encoded)
                    writer.Write(buffer); // binary buffer data
                }

                stream.Flush();
                var glb = stream.GetBuffer();

                return glb;
            }
        }

        private byte[] CompressGlb(byte[] inputBytes)
        {
            // TODO: replace with a configurable solution
            var compressorExePath = GetCompressorExeDefaultPath();
            if (compressorExePath == null)
            {
                _logger?.LogWarning($"Compressor executable not found");
                return null;
            }

            _logger?.LogInformation($"Model compression...");

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.glb");
            File.WriteAllBytes(tempFilePath, inputBytes);

            var processStartInfo = new ProcessStartInfo(compressorExePath, tempFilePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var compressProcess = Process.Start(processStartInfo);
            compressProcess.WaitForExit();

            if (compressProcess.ExitCode != 0)
            {
                _logger?.LogWarning($"Model compression failed");
                return null;
            }

            var outputBytes = File.ReadAllBytes(tempFilePath);
            File.Delete(tempFilePath);
            _logger?.LogInformation($"OK");

            return outputBytes;
        }

        private string GetCompressorExeDefaultPath()
        {
            if (!String.IsNullOrEmpty(_compressorExePath)
                && File.Exists(_compressorExePath))
            {
                return _compressorExePath;
            }

            var currentDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!String.IsNullOrEmpty(currentDirPath))
            {
                var currentDirFilePath = Path.Combine(currentDirPath, defaultCompressorFileName);
                if (File.Exists(currentDirFilePath))
                {
                    return currentDirFilePath;
                }

                var parentDirPath = Directory.GetParent(currentDirPath).FullName;
                var parentDirFilePath = Path.Combine(parentDirPath, defaultCompressorFileName);
                if (File.Exists(parentDirFilePath))
                {
                    return parentDirFilePath;
                }
            }

            return null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GltfModelExporter()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    readonly struct BufferViewInfo
    {
        public int VerticesCount { get; }

        public IndicesType IndicesType { get; }

        public uint IndicesOffset { get; }
        public uint PositionsOffset { get; }
        public uint? NormalsOffset { get; }

        public BufferViewInfo(int verticesCount, IndicesType indicesType, 
            uint indicesOffset, uint positionsOffset, uint? normalsOffset)
        {
            VerticesCount = verticesCount;
            IndicesType = indicesType;
            IndicesOffset = indicesOffset;
            PositionsOffset = positionsOffset;
            NormalsOffset = normalsOffset;
        }
    }
    readonly struct AccessorIndices
    {
        public int IndicesIndex { get; }
        public int PositionsIndex { get; }
        public int? NormalsIndex { get; }

        public AccessorIndices(int indicesIndex, int positionsIndex, int? normalsIndex)
        {
            IndicesIndex = indicesIndex;
            PositionsIndex = positionsIndex;
            NormalsIndex = normalsIndex;
        }

        public void Deconstruct(out int indices, out int position, out int? normal)
        {
            indices = IndicesIndex;
            position = PositionsIndex;
            normal = NormalsIndex;
        }
    }

    enum IndicesType
    {
        Ubyte = 1,
        Ushort = 2,
        Uint = 4
    }
}

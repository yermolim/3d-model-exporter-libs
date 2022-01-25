using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ModelExporter.Core.Interfaces;
using ModelExporter.Core.ModelDataTypes;
using ModelExporter.Core.ModelDataTypes.Structs;
using ModelExporter.Ifc.Types;

namespace ModelExporter.Ifc
{
    /// <summary>
    /// Preview version of basic IFC exporter, that uses previously extracted model geometry data.
    /// </summary>
    public class IfcModelExporter : IModelDataExporter, IItemsDataExporter
    {
        public static byte[] ConvertItemsToGlb(IList<ItemData> items, string fileName)
        {
            byte[] glbBytes;
            using (var glbExporter = new IfcModelExporter())
            {
                glbBytes = glbExporter.Export(items, fileName);
            }
            return glbBytes;
        }

        private readonly ILogger _logger;

        public IfcModelExporter()
            : this(null) { }

        public IfcModelExporter(ILogger logger)
        {
            _logger = logger;
        }

        public byte[] Export(ModelData parsedModel)
        {
            return Export(parsedModel.Items, parsedModel.FileName);
        }

        public byte[] Export(IList<ItemData> items, string fileName)
        {
            _logger?.LogInformation("Exporting to IFC...");
            var ifcLines = GetIfcLines(items, fileName);

            using (var ms = new MemoryStream())
            {
                TextWriter tw = new StreamWriter(ms, Encoding.GetEncoding("iso-8859-1"));
                foreach (var line in ifcLines)
                {
                    var trimmedLine = line.Trim(new char[] { '\uFEFF', '\u200B' }); // using trim to get rid of BOM char
                    tw.WriteLine(trimmedLine);
                }
                tw.Flush();
                ms.Position = 0;
                var bytes = ms.ToArray();

                _logger?.LogInformation("OK");
                return bytes;
            }
        }

        private IList<string> GetIfcLines(IList<ItemData> items, string fileName)
        {
            var ifcLines = new List<string> { "﻿ISO-10303-21;" };
            var headerLines = GetHeaderLines(fileName);
            ifcLines.AddRange(headerLines);

            ifcLines.Add("﻿DATA;"); 
            var contextLines = GetContextLines(Path.GetFileNameWithoutExtension(fileName));
            ifcLines.AddRange(contextLines);

            var colorsStartIndex = 100L;
            var meshesUnique = ItemData.DistinctItemsMeshes(items);
            var colorsUnique = meshesUnique.Select(x => x.Color).Distinct().ToArray();
            var (colorLines, colorIndices, colorsLastIndex) = GetMaterialsLines(colorsUnique, colorsStartIndex);
            ifcLines.AddRange(colorLines);

            var meshesStartIndex = ((colorsStartIndex / 1000) + 1) * 1000;
            var (meshLines, meshIndices, meshesLastIndex) = GetMeshesLines(meshesUnique, meshesStartIndex, colorIndices);
            ifcLines.AddRange(meshLines);

            var itemsStartIndex = ((meshesLastIndex / 1000) + 1) * 1000;
            foreach (var item in items)
            {
                var meshIndex = meshIndices.Find(x => x.mesh.Equals(item.Mesh)).faceSetIndex;
                var itemLines = GetItemLines(item, itemsStartIndex, meshIndex);
                ifcLines.AddRange(itemLines);

                itemsStartIndex += 10;
            }
            ifcLines.Add("ENDSEC;");
            ifcLines.Add("END-ISO-10303-21;");

            return ifcLines;
        }

        private IList<string> GetHeaderLines(string fileName, 
            string author = "", string organization = "",
            string tool = "", string application = "")
        {
            return new string[]
            {
                "HEADER;",
                "FILE_DESCRIPTION(('ViewDefinition[ReferenceView_V1.0]'),'2;1');",
                $"FILE_NAME('{Path.ChangeExtension(fileName, ".ifc")}'," +
                $"'{DateTime.Now.ToString("s")}',('{author}'),('{organization}')," +
                $"'{tool}','{application}','None');",
                $"FILE_SCHEMA(('IFC4'));",
                "ENDSEC;",
            };
        }

        private IList<string> GetContextLines(string projectName)
        {
            return new string[]
            {
                "#1=IFCCARTESIANPOINT((0.,0.,0.));",
                "#2=IFCDIRECTION((0.,0.,1.));",
                "#3=IFCDIRECTION((1.,0.,0.));",
                "#4=IFCAXIS2PLACEMENT3D(#1,#2,#3);",
                "#5=IFCDIRECTION((0.,1.));",

                "#10=IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,1.0E-5,#4,#5);",
                "#11=IFCGEOMETRICREPRESENTATIONSUBCONTEXT('Axis','Model',*,*,*,*,#10,$,.MODEL_VIEW.,$);",
                "#12=IFCGEOMETRICREPRESENTATIONSUBCONTEXT('Body','Model',*,*,*,*,#10,$,.MODEL_VIEW.,$);",

                "#21=IFCSIUNIT(*,.LENGTHUNIT.,$,.METRE.);",
                "#22=IFCSIUNIT(*,.AREAUNIT.,$,.SQUARE_METRE.);",
                "#23=IFCSIUNIT(*,.PLANEANGLEUNIT.,$,.RADIAN.);",
                "#24=IFCSIUNIT(*,.VOLUMEUNIT.,$,.CUBIC_METRE.);",
                "#25=IFCUNITASSIGNMENT((#21,#22,#23,#24));",

                $"#31=IFCPROJECT('{IfcGuid.NewIfcGuid()}',$,'{projectName}',$,$,'default_project',$,(#10),#25);",

                "#40=IFCLOCALPLACEMENT($,#4);",
                $"#41=IFCSITE('{IfcGuid.NewIfcGuid()}',$,'default_site',$,$,#40,$,$,.ELEMENT.,$,$,$,$,$);",
                $"#42=IFCRELAGGREGATES('{IfcGuid.NewIfcGuid()}',$,$,$,#31,(#41));",

                "#50=IFCLOCALPLACEMENT(#40,#4);",
                $"#51=IFCBUILDING('{IfcGuid.NewIfcGuid()}',$,'default_building',$,$,#50,$,$,.ELEMENT.,$,$,$);",
                $"#52=IFCRELAGGREGATES('{IfcGuid.NewIfcGuid()}',$,$,$,#41,(#51));",

                "#60=IFCLOCALPLACEMENT(#50,#4);",
                $"#61=IFCBUILDINGSTOREY('{IfcGuid.NewIfcGuid()}',$,'default_storey',$,$,#60,$,$,.ELEMENT.,$,$,$);",
                $"#62=IFCRELAGGREGATES('{IfcGuid.NewIfcGuid()}',$,$,$,#51,(#61));",
            };
        }

        private (IList<string> lines, Dictionary<PbrColor, long> indices, long lastIndex)
            GetMaterialsLines(IList<PbrColor> colorsUnique, long index)
        {
            var lines = new List<string>();
            var indices = new Dictionary<PbrColor, long>();
            foreach (var color in colorsUnique)
            {
                lines.Add($"#{++index}=IFCCOLOURRGB($,{color.R},{color.G},{color.B});");
                lines.Add($"#{++index}=IFCSURFACESTYLESHADING(#{index - 1},{1 - color.A});");
                lines.Add($"#{++index}=IFCSURFACESTYLE($,.BOTH.,(#{index - 1}));");

                indices.Add(color, index);
            }

            return (lines, indices, index);
        }

        private (IList<string> lines, List<(MeshData mesh, long faceSetIndex)> indices, long lastIndex) 
            GetMeshesLines(IList<MeshData> meshesUnique, long index, Dictionary<PbrColor, long> colorIndices)
        {
            var lines = new List<string>();
            var indices = new List<(MeshData mesh, long faceSetIndex)>();
            foreach (var mesh in meshesUnique)
            {
                var (positions, positionIndices) = mesh.GetDistinctPositions();

                var pointListLine = new StringBuilder($"#{++index}=IFCCARTESIANPOINTLIST3D((");
                foreach (var position in positions)
                {
                    pointListLine.Append($"({position.X},{position.Y},{position.Z}),");
                }
                pointListLine.Length--; // remove trailing comma
                pointListLine.Append("));");
                lines.Add(pointListLine.ToString());

                var faceSetLine = new StringBuilder($"#{++index}=IFCTRIANGULATEDFACESET(#{index - 1},$,.T.,(");
                for (var i = 0; i < positionIndices.Length;)
                {
                    faceSetLine.Append($"({positionIndices[i++] + 1},{positionIndices[i++] + 1},{positionIndices[i++] + 1}),");
                }
                faceSetLine.Length--; // remove trailing comma
                faceSetLine.Append("),$);");
                lines.Add(faceSetLine.ToString());

                lines.Add($"#{++index}=IFCSTYLEDITEM(#{index - 1},(#{colorIndices[mesh.Color]}),$);");

                indices.Add((mesh, index - 1));
            }

            return (lines, indices, index);
        }

        private IList<string> GetItemLines(ItemData item, 
            long currentIndex, long faceSetIndex) {

            var matrix = item.Transform.Value;
            return new string[]
            {                
                $"#{++currentIndex}=IFCCARTESIANPOINT(({matrix.M41},{matrix.M42},{matrix.M43}));",
                $"#{++currentIndex}=IFCDIRECTION(({matrix.M31},{matrix.M32},{matrix.M33}));",
                $"#{++currentIndex}=IFCDIRECTION(({matrix.M11},{matrix.M12},{matrix.M13}));",
                $"#{++currentIndex}=IFCAXIS2PLACEMENT3D(#{currentIndex - 3},#{currentIndex - 2},#{currentIndex - 1});",
                $"#{++currentIndex}=IFCLOCALPLACEMENT(#60,#{currentIndex - 1});",
                $"#{++currentIndex}=IFCSHAPEREPRESENTATION(#12,'Body','Tessellation',(#{faceSetIndex}));",
                $"#{++currentIndex}=IFCPRODUCTDEFINITIONSHAPE($,$,(#{currentIndex - 1}));",
                $"#{++currentIndex}=IFCBUILDINGELEMENTPROXY('{IfcGuid.NewIfcGuid()}',$,'{item.Handle}','{item.Handle}',$,#{currentIndex - 3},#{currentIndex - 1},$,$);",
                $"#{++currentIndex}=IFCRELCONTAINEDINSPATIALSTRUCTURE('{IfcGuid.NewIfcGuid()}',$,'{item.Handle}',$,(#{currentIndex - 1}),#61);",
            };
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IfcModelExporter()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

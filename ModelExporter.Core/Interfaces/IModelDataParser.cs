using ModelExporter.Core.ModelDataTypes;

using System;
using System.Collections.Generic;

namespace ModelExporter.Core.Interfaces
{
    public interface IModelDataParser : IDisposable
    {
        IEnumerable<ModelData> ParseGeometry(string filePath, double trX, double trY, double trZ);
    }
}

using ModelExporter.Core.ModelDataTypes;

using System;

namespace ModelExporter.Core.Interfaces
{
    public interface IModelDataExporter : IDisposable
    {
        byte[] Export(ModelData model);
    }
}

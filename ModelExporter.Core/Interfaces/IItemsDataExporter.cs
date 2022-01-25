using ModelExporter.Core.ModelDataTypes;

using System.Collections.Generic;

namespace ModelExporter.Core.Interfaces
{
    public interface IItemsDataExporter
    {
        byte[] Export(IList<ItemData> parsedItems, string fileName);
    }
}

using ModelExporter.Core.ModelDataTypes;

using System.Collections.Generic;
using System.Numerics;

namespace ModelExporter.Core.Interfaces
{
    public interface IItemsDataParser
    {
        IList<ItemData> ParseGeometry(Vector3? basePoint = null);
    }
}

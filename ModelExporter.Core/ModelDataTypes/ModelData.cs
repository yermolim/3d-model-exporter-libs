using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelExporter.Core.ModelDataTypes
{
    public class ModelData
    {
        public string FileName { get; }

        public ItemData[] Items { get; }

        public ModelData(string fileName, IList<ItemData> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentNullException("No model items passed");
            }

            FileName = fileName;
            Items = items.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    public static class CollectionHelper
    {
        public static ItemType GetIfExists<ItemType>(IList<ItemType> list, int index)
        {
            if (list.Count > index)
            {
                return list[index];
            }
            else
            {
                return default(ItemType);
            }
        }

        public static void DisposeItems<ItemType>(IList<ItemType> list)
        {
            for (int index = list.Count - 1; index >= 0; --index)
            {
                IDisposable disposable = list[index] as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}

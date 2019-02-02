using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    public class WADMProduct
    {
        public readonly string RootName;
        public readonly string WrapOrListName;
        public readonly bool WasList;
        public Dictionary<string, string> Elements;
        public List<Dictionary<string, string>> List;

        public WADMProduct(string RootName, string WrapOrListName, bool WasList)
        {
            this.RootName = RootName;
            this.WrapOrListName = WrapOrListName;
            this.WasList = WasList;
        }
    }
}

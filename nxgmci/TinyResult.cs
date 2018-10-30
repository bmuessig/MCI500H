using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    public class TinyResult
    {
        public readonly bool Success;
        public readonly string ErrorMessage;
        public readonly string RootName;
        public readonly string WrapOrListName;
        public readonly bool WasList;
        public Dictionary<string, string> Elements;
        public List<Dictionary<string, string>> List;

        public TinyResult(string RootName, string WrapOrListName, bool WasList)
        {
            this.Success = true;
            this.RootName = RootName;
            this.WrapOrListName = WrapOrListName;
            this.WasList = WasList;
        }

        public TinyResult(string ErrorMessage)
        {
            this.Success = false;
            this.ErrorMessage = ErrorMessage;
        }
    }
}

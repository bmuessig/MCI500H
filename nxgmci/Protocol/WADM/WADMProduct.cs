using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Used by the WADMParser to return the parsing result.
    /// </summary>
    public class WADMProduct
    {
        /// <summary>
        /// The name of the root node.
        /// </summary>
        public readonly string RootName;

        /// <summary>
        /// Either the name of the content wrap node or the name of the list wrap node.
        /// </summary>
        public readonly string WrapOrListName;

        /// <summary>
        /// Indicates whether a list could be parsed.
        /// </summary>
        public readonly bool WasList;

        /// <summary>
        /// A dictionary of the top level elements.
        /// </summary>
        public Dictionary<string, string> Elements;

        /// <summary>
        /// A list of dictionaries. One dictionary for every list node element.
        /// </summary>
        public List<Dictionary<string, string>> List;

        /// <summary>
        /// Default internal constructor.
        /// </summary>
        /// <param name="RootName">Name of the root node.</param>
        /// <param name="WrapOrListName">Name of the wrap node element or of the list node element.</param>
        /// <param name="WasList">Indicates that a list could be parsed.</param>
        internal WADMProduct(string RootName, string WrapOrListName, bool WasList)
        {
            this.RootName = RootName;
            this.WrapOrListName = WrapOrListName;
            this.WasList = WasList;
        }
    }
}

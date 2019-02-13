using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Used by the WADMParser to return the parsing result.
    /// </summary>
    public class WADMProduct
    {
        // TOOD: Make sure all classes using this check if it has a list

        /// <summary>
        /// The name of the root node.
        /// </summary>
        public readonly string RootName;

        /// <summary>
        /// The name of the second wrapper node.
        /// </summary>
        public readonly string WrapName;

        /// <summary>
        /// The name of the list wrapper node.
        /// </summary>
        public readonly string ListWrapName;

        /// <summary>
        /// The name of the list items.
        /// </summary>
        public readonly string ListItemName;

        /// <summary>
        /// Indicates whether the input is parsed as a list.
        /// </summary>
        public readonly bool HadList;

        /// <summary>
        /// Indicates whether the input is parsed with a wrapper.
        /// </summary>
        public readonly bool HadWrap;

        /// <summary>
        /// Indicates, whether the list is wrapped in another node.
        /// </summary>
        public readonly bool HadWrappedList;

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
            if (WasList)
                this.ListWrapName = WrapOrListName;
            else
                this.WrapName = WrapOrListName;
            this.HadList = WasList;
            this.HadWrap = !WasList;
            this.HadWrappedList = false;
        }

        /// <summary>
        /// Internal constructor for cases in which both a list and a wrap exist.
        /// </summary>
        /// <param name="RootName">Name of the root wrap node.</param>
        /// <param name="WrapName">Name of the second wrap node.</param>
        /// <param name="ListWrapName">Name of the list wrap node.</param>
        /// <param name="ListItemName">Name of the list items.</param>
        internal WADMProduct(string RootName, string WrapName, string ListWrapName, string ListItemName)
        {
            this.RootName = RootName;
            this.WrapName = WrapName;
            this.ListWrapName = ListWrapName;
            this.ListItemName = ListItemName;
            this.HadList = true;
            this.HadWrap = true;
            this.HadWrappedList = true;
        }
    }
}

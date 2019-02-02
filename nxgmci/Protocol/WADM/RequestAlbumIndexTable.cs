using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    public static class RequestAlbumIndexTable
    {
        // This request returns a key-value-pair table of all album ids and their clear text names.
        // We can use this information to map the album ids returned by RequestRawData to strings.

        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        // RequestAlbumIndexTable-Reqest:
        public static string Build()
        {
            return "<requestalbumindextable></requestalbumindextable>";
        }

        // ContentDataSet-Response
        public static ActionResult<ContentDataSet> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return new ActionResult<ContentDataSet>("The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> result = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!result.Success)
                if (!string.IsNullOrWhiteSpace(result.Message))
                    return new ActionResult<ContentDataSet>(result.ToString());
                else
                    return new ActionResult<ContentDataSet>("The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (result.Product == null)
                return new ActionResult<ContentDataSet>("The parsing product was null!");

            // And also make sure our state is correct
            if (result.Product.Elements == null || result.Product.List == null)
                return new ActionResult<ContentDataSet>("The list of parsed elements or list items is null!");

            // Now, make sure our mandatory argument exists
            if (!result.Product.Elements.ContainsKey("updateid"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "updateid"));
            
            // Then, try to parse the parameter
            uint updateID;
            if (!uint.TryParse(result.Product.Elements["updateid"], out updateID))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "updateid"));

            // Allocate our result object
            ContentDataSet set = new ContentDataSet(updateID);

            // Next, pay attention to the list items (yes, there are a lot of them)
            uint elementNo = 0;
            foreach (Dictionary<string, string> listItem in result.Product.List)
            {
                // Increment the element ID to simplify fault-finding
                elementNo++;

                // Make sure that all our elements are non-null
                if (listItem == null)
                    continue;

                // First, make sure our mandatory arguments exist
                if (!listItem.ContainsKey("name"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "name", elementNo));
                if (!listItem.ContainsKey("index"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "index", elementNo));

                // Then, try to parse the parameters
                string name;
                uint index;
                if (string.IsNullOrWhiteSpace((name = listItem["name"])))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as string!", "name", elementNo));
                if (!uint.TryParse(listItem["index"], out index))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "index", elementNo));

                // If we need to, perform sanity checks on the input data
                if (ValidateInput)
                    if (index == 0)
                        return new ActionResult<ContentDataSet>(string.Format("nodeid #{0} == 0", elementNo));

                // Finally, assemble and add the object
                if (!set.AddEntry(name, index, true))
                    return new ActionResult<ContentDataSet>(string.Format("Could not append item #{0} to the list!", elementNo));
            }

            // Finally, return the response
            return new ActionResult<ContentDataSet>(set);
        }

        // ContentDataSet-Structure:
        // elements:            Returned elements
        // updateid		(uint): UNKNOWN! e.g. 422
        public class ContentDataSet
        {
            public List<ContentData> ContentData;
            public readonly uint UpdateID;

            internal ContentDataSet(uint UpdateID)
            {
                this.UpdateID = UpdateID;
                this.ContentData = new List<ContentData>();
            }

            internal ContentDataSet(List<ContentData> ContentData, uint UpdateID)
                : this(UpdateID)
            {
                // Make sure ContentData is initialized
                if (ContentData != null)
                    this.ContentData = ContentData;
            }

            public bool AddEntry(ContentData Data, bool ReplaceDuplicates = true)
            {
                // Perform some input sanity checks
                if(Data == null)
                    return false;
                if (string.IsNullOrWhiteSpace(Data.Name))
                    return false;

                // Just making sure we don't get any null issues
                if (ContentData == null)
                    ContentData = new List<ContentData>();
                else {
                    // If we may not replace duplicates, and have a duplicate, fail
                    if (!ReplaceDuplicates)
                        if (ContainsEntry(Data.Index))
                            return false;

                    // Otherwise just check for a duplicate and remove it if present
                    RemoveEntry(Data.Index);
                }
                
                // Finally, add the new entry
                ContentData.Add(Data);

                // And return success
                return true;
            }

            public bool AddEntry(string Name, uint Index, bool ReplaceDuplicates = true)
            {
                // This is just an easy wrapper to use base types for the arguments
                return AddEntry(new ContentData(Name, Index), ReplaceDuplicates);
            }

            public void RemoveEntry(uint Index)
            {
                // Just making sure we don't get any null issues
                if (ContentData == null)
                {
                    ContentData = new List<ContentData>();
                    return; // This is no failure, since there are entries
                }

                // Loop through all items until we find our offender
                for (int i = 0; i < ContentData.Count; i++)
                    if (ContentData[i].Index == Index)
                    {
                        // If we find it, remove it and exit
                        ContentData.RemoveAt(i);
                        return;
                    }
            }

            public bool ContainsEntry(uint Index)
            {
                // Just making sure we don't get any null issues
                if(ContentData == null)
                {
                    ContentData = new List<ContentData>();
                    return false;
                }

                // Loop through all items until we find a duplicate
                foreach (ContentData data in ContentData)
                    if (data.Index == Index)
                        return true;

                // If not, we don't have a duplicate
                return false;
            }

            public ContentData GetEntry(uint Index)
            {
                // Just making sure we don't get any null issues
                if (ContentData == null)
                {
                    ContentData = new List<ContentData>();
                    return null;
                }

                // Loop through all items until we find our item
                foreach (ContentData data in ContentData)
                    if (data.Index == Index)
                        return data;

                // If we don't find anything return null
                return null;
            }
        }

        // ContentData-Structure:
        // name		    (string):	Name of the album
        // index	    (uint):	    ID number of the album
        // => KeyValuePair(index, name)

        public class ContentData
        {
            public string Name;
            public uint Index;

            internal ContentData(string Name, uint Index)
            {
                this.Name = Name;
                this.Index = Index;
            }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;
                return Name;
            }
        }
    }
}

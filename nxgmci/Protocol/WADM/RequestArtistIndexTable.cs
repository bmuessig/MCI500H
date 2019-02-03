﻿using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    public static class RequestArtistIndexTable
    {
        // This request returns a key-value-pair table of all artist ids and their clear text names.
        // We can use this information to map the artist ids returned by RequestRawData to strings.

        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        // RequestArtistIndexTable-Reqest:
        public static string Build()
        {
            return "<requestartistindextable></requestartistindextable>";
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

        /// <summary>
        /// RequestArtistIndexTable's ContentDataSet reply.
        /// </summary>
        public class ContentDataSet
        {
            /// <summary>
            /// List of returned elements.
            /// </summary>
            public List<ContentData> ContentData;

            /// <summary>
            /// Unknown update ID.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="UpdateID">Unknown update ID.</param>
            internal ContentDataSet(uint UpdateID)
            {
                this.UpdateID = UpdateID;
                this.ContentData = new List<ContentData>();
            }

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="ContentData">List of returned elements.</param>
            /// <param name="UpdateID">Unknown update ID.</param>
            internal ContentDataSet(List<ContentData> ContentData, uint UpdateID)
                : this(UpdateID)
            {
                // Make sure ContentData is initialized
                if (ContentData != null)
                    this.ContentData = ContentData;
            }

            /// <summary>
            /// Adds a new entry to the collection.
            /// </summary>
            /// <param name="Data">The new entry to add.</param>
            /// <param name="ReplaceDuplicates">True, if a conflicting entry should be replaced and false if not.</param>
            /// <returns>True, if the entry could be added and false otherwise.</returns>
            public bool AddEntry(ContentData Data, bool ReplaceDuplicates = true)
            {
                // Perform some input sanity checks
                if (Data == null)
                    return false;
                if (string.IsNullOrWhiteSpace(Data.Name))
                    return false;

                // Just making sure we don't get any null issues
                if (ContentData == null)
                    ContentData = new List<ContentData>();
                else
                {
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

            /// <summary>
            /// Adds a new entry to the collection. Shorthand function for directly adding a name and index.
            /// </summary>
            /// <param name="Name">Name of the new entry.</param>
            /// <param name="Index">Index of the new entry.</param>
            /// <param name="ReplaceDuplicates">True, if a conflicting entry should be replaced and false if not.</param>
            /// <returns>True, if the entry could be added and false otherwise.</returns>
            public bool AddEntry(string Name, uint Index, bool ReplaceDuplicates = true)
            {
                // This is just an easy wrapper to use base types for the arguments
                return AddEntry(new ContentData(Name, Index), ReplaceDuplicates);
            }

            /// <summary>
            /// Attempts to remove an entry by the index. No error is thrown if the entry did not exist.
            /// </summary>
            /// <param name="Index">The index that should be removed from the collection.</param>
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

            /// <summary>
            /// Returns whether an entry with the desired index exists in the collection.
            /// </summary>
            /// <param name="Index">The index that should be searched for.</param>
            /// <returns>True, if the entry exists and false otherwise.</returns>
            public bool ContainsEntry(uint Index)
            {
                // Just making sure we don't get any null issues
                if (ContentData == null)
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

            /// <summary>
            /// Attempts to return an entry by it's index.
            /// </summary>
            /// <param name="Index">The index that should be searched for.</param>
            /// <returns>Returns the entry if it could be found. Null otherwise.</returns>
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

        /// <summary>
        /// RequestArtistIndexTable's ContentDataSet's ContentDataSet.
        /// </summary>
        public class ContentData
        {
            /// <summary>
            /// Name of the artist.
            /// </summary>
            public string Name;

            /// <summary>
            /// Node ID number of the artist.
            /// </summary>
            public uint Index;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Name">Name of the artist.</param>
            /// <param name="Index">Node ID number of the artist.</param>
            internal ContentData(string Name, uint Index)
            {
                this.Name = Name;
                this.Index = Index;
            }

            /// <summary>
            /// Returns the string representation of the entry. Usually returns the artist's name, if available.
            /// </summary>
            /// <returns>A string representation of the object.</returns>
            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;
                return Name;
            }
        }


    }
}

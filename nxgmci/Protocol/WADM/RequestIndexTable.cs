﻿using System.Collections.Generic;
using System;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request returns a dictionary of all artist/album/genre IDs and their cleartext names.
    /// This information can be used to map the artist/album/genre IDs returned by RequestRawData to strings.
    /// </summary>
    public static class RequestIndexTable
    {
        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        /// <summary>
        /// Assembles a RequestArtistIndexTable request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildArtist()
        {
            return "<requestartistindextable></requestartistindextable>";
        }
        
        /// <summary>
        /// Assembles a RequestAlbumIndexTable request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildAlbum()
        {
            return "<requestalbumindextable></requestalbumindextable>";
        }

        /// <summary>
        /// Assembles a RequestGenreIndexTable request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildGenre()
        {
            return "<requestgenreindextable></requestgenreindextable>";
        }
        
        /// <summary>
        /// Parses RequestIndexTable's ContentDataSet and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="ValidateInput">Indicates whether to validate the data values received.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ContentDataSet> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Allocate the result object
            Result<ContentDataSet> result = new Result<ContentDataSet>();

            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return Result<ContentDataSet>.FailMessage(result, "The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> parserResult = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!parserResult.Success)
                if (parserResult.Error != null)
                    return Result<ContentDataSet>.FailErrorMessage(result, parserResult.Error, "The parsing failed!");
                else
                    return Result<ContentDataSet>.FailMessage(result, "The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return Result<ContentDataSet>.FailMessage(result, "The parsing product was null!");

            // And also make sure our state is correct
            if (parserResult.Product.Elements == null || parserResult.Product.List == null)
                return Result<ContentDataSet>.FailMessage(result, "The list of parsed elements or list items is null!");

            // Now, make sure our mandatory argument exists
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");
            
            // Then, try to parse the parameter
            uint updateID;
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");

            // Allocate our result object
            ContentDataCompiler set = new ContentDataCompiler();

            // Next, pay attention to the list items (yes, there are a lot of them)
            uint elementNo = 0;
            foreach (Dictionary<string, string> listItem in parserResult.Product.List)
            {
                // Increment the element ID to simplify fault-finding
                elementNo++;

                // Make sure that all our elements are non-null
                if (listItem == null)
                    continue;

                // First, make sure our mandatory arguments exist
                if (!listItem.ContainsKey("name"))
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "name", elementNo);
                if (!listItem.ContainsKey("index"))
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "index", elementNo);

                // Then, try to parse the parameters
                string name;
                uint index;
                if (string.IsNullOrEmpty((name = listItem["name"])))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "name", elementNo);
                if (!uint.TryParse(listItem["index"], out index))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "index", elementNo);

                // If we need to, perform sanity checks on the input data
                if (ValidateInput)
                    if (index == 0)
                        return Result<ContentDataSet>.FailMessage(result, "nodeid #{0} == 0", elementNo);

                // Finally, assemble and add the object
                if (!set.AddEntry(name, index, true))
                    return Result<ContentDataSet>.FailMessage(result, "Could not append item #{0} to the list!", elementNo);
            }

            // Finally, return the response
            return Result<ContentDataSet>.SucceedProduct(result, new ContentDataSet(set.ToArray(), updateID));
        }

        /// <summary>
        /// List that allows to easily sort out duplicates and to finally compile a ContentData array.
        /// </summary>
        private class ContentDataCompiler : List<ContentData>
        {
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
                // If we may not replace duplicates, and have a duplicate, fail
                if (!ReplaceDuplicates)
                    if (ContainsEntry(Data.Index))
                        return false;

                // Otherwise just check for a duplicate and remove it if present
                RemoveEntry(Data.Index);

                // Finally, add the new entry
                Add(Data);

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
                // Loop through all items until we find our offender
                for (int i = 0; i < Count; i++)
                    if (this[i].Index == Index)
                    {
                        // If we find it, remove it and exit
                        RemoveAt(i);
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
                // Loop through all items until we find a duplicate
                foreach (ContentData data in this)
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
                // Loop through all items until we find our item
                foreach (ContentData data in this)
                    if (data.Index == Index)
                        return data;

                // If we don't find anything return null
                return null;
            }
        }

        /// <summary>
        /// RequestIndexTable's ContentDataSet reply.
        /// </summary>
        public class ContentDataSet
        {
            /// <summary>
            /// List of returned elements.
            /// </summary>
            public readonly ContentData[] ContentData;

            /// <summary>
            /// Modification update ID.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="ContentData">List of returned elements.</param>
            /// <param name="UpdateID">Modification update ID.</param>
            internal ContentDataSet(ContentData[] ContentData, uint UpdateID)
            {
                // Make sure ContentData is initialized
                if (ContentData == null)
                    throw new ArgumentNullException("ContentData");

                // Assign the values
                this.ContentData = ContentData;
                this.UpdateID = UpdateID;
            }

            /// <summary>
            /// Attempts to return an entry by its name.
            /// </summary>
            /// <param name="Name">The name that should be searched for.</param>
            /// <param name="IgnoreCase">If true, the name is searched case insensitive.</param>
            /// <param name="IgnoreWhiteSpace">If true, preceeding and trailing whitespace is ignored.</param>
            /// <returns>Returns the entry if it could be found. Null otherwise.</returns>
            public ContentData FindName(string Name, bool IgnoreCase = false, bool IgnoreWhiteSpace = false)
            {
                // Check if the content data is null (it should never be)
                if (ContentData == null || Name == null)
                    return null;

                // Check if the name needs to be adjusted
                if (IgnoreCase)
                    Name = Name.ToLower();
                if (IgnoreWhiteSpace)
                    Name = Name.Trim();

                // Loop through all items until we find our item
                foreach (ContentData data in ContentData)
                {
                    // Sanity check the item input
                    if (data == null)
                        continue;
                    if (data.Name == null)
                        continue;

                    // Modify the name for comparsion
                    string itemName = data.Name;
                    if (IgnoreCase)
                        itemName = itemName.ToLower();
                    if (IgnoreWhiteSpace)
                        itemName = itemName.Trim();

                    // Check, if the entry matches
                    if (data.Name == Name)
                        return data;
                }

                // If we don't find anything return null
                return null;
            }

            /// <summary>
            /// Attempts to return an entry by its index.
            /// </summary>
            /// <param name="Index">The index that should be searched for.</param>
            /// <returns>Returns the entry if it could be found. Null otherwise.</returns>
            public ContentData FindIndex(uint Index)
            {
                // Check if the content data is null (it should never be)
                if (ContentData == null)
                    return null;

                // Loop through all items until we find our item
                foreach (ContentData data in ContentData)
                {
                    // Sanity check item
                    if (data == null)
                        continue;

                    // Check the index
                    if (data.Index == Index)
                        return data;
                }

                // If we don't find anything return null
                return null;
            }
        }

        /// <summary>
        /// RequestIndexTable's ContentDataSet's ContentData.
        /// </summary>
        public class ContentData
        {
            /// <summary>
            /// Name of the artist/album/genre.
            /// </summary>
            public string Name;

            /// <summary>
            /// Node ID number of the artist/album/genre.
            /// </summary>
            public uint Index;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Name">Name of the artist/album/genre.</param>
            /// <param name="Index">Node ID number of the artist/album/genre.</param>
            internal ContentData(string Name, uint Index)
            {
                this.Name = Name;
                this.Index = Index;
            }

            /// <summary>
            /// Returns the string representation of the entry. Usually returns the artist/album/genre name, if available.
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

using System;
using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This API call provides a DLNA-like folder and media browser with the ability to display cover art.
    /// Additionally, this is one of the single most important API calls to fetch data from the stero.
    /// Though this, cover art, direct URLs, titles, artists, genres, etc. can be fetched directly, which is particularily useful for display and playback.
    /// </summary>
    public static class RequestPlayableNavData
    {
        /// <summary>
        /// ContentDataSet Parser
        /// </summary>
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        /// <summary>
        /// Builds a RequestPlayableData request from the supplied elements.
        /// </summary>
        /// <param name="NodeID">Parent node ID to fetch the child elements from.</param>
        /// <param name="NumElem">Maximum number of elements (0 returns all elements).</param>
        /// <param name="FromIndex">Offset index to base the query on.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildPlayable(uint NodeID, uint NumElem = 0, uint FromIndex = 0)
        {
            int processedNumElem = (int)NumElem; // Potentional overflow here - probably not, as there can only be 30k-ish tracks
            if (NodeID == 0 && NumElem == 0)
                processedNumElem = -1;

            return string.Format(
                "<requestplayabledata>" +
                "<nodeid>{0}</nodeid>" +
                "<numelem>{1}</numelem>" +
                "<fromindex>{2}</fromindex>" +
                "</requestplayabledata>",
                NodeID,
                processedNumElem,
                FromIndex);
        }

        /// <summary>
        /// Builds a RequestNavData request from the supplied elements.
        /// </summary>
        /// <param name="NodeID">Parent node ID to fetch the child elements from.</param>
        /// <param name="NumElem">Maximum number of elements (0 returns all elements).</param>
        /// <param name="FromIndex">Offset index to base the query on.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildNav(uint NodeID, uint NumElem = 0, uint FromIndex = 0)
        {
            int processedNumElem = (int)NumElem; // Potentional overflow here - probably not, as there can only be 30k-ish tracks
            if (NodeID == 0 && NumElem == 0)
                processedNumElem = -1;

            return string.Format(
                "<requestnavdata>" +
                "<nodeid>{0}</nodeid>" +
                "<numelem>{1}</numelem>" +
                "<fromindex>{2}</fromindex>" +
                "</requestnavdata>",
                NodeID,
                processedNumElem,
                FromIndex);
        }

        /// <summary>
        /// Parses a ContentDataSet response.
        /// </summary>
        /// <param name="Response">Text response input from the stereo's server.</param>
        /// <param name="ValidateInput">Indicates whether to verify the contents received after parsing.</param>
        /// <param name="LazySyntax">Indicates whether minor parsing errors are ignored.</param>
        /// <returns>
        /// A result object that contains a serialized version of the response data.
        /// Note, that this will return the base class which has to be type checked and casted to the full, detailed type.
        /// </returns>
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
                    return Result<ContentDataSet>.FailMessage(result, "The parsing failed:\n{0}", parserResult.ToString());
                else
                    return Result<ContentDataSet>.FailMessage(result, "The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return Result<ContentDataSet>.FailMessage(result, "The parsing product was null!");

            // And also make sure our state is correct
            if (parserResult.Product.Elements == null || parserResult.Product.List == null)
                return Result<ContentDataSet>.FailMessage(result, "The list of parsed elements or list items is null!");

            // Now, the request will be parsed and anaylzed
            bool failed = false;
            uint invalidNodeID = 0;
            uint totNumElem = 0, fromIndex = 0, numElem = 0, updateID = 0;
            bool alphanumeric = parserResult.Product.Elements.ContainsKey("alphanumeric");

            // First, check if the request failed
            if (parserResult.Product.Elements.ContainsKey("invalidnodeid"))
            {
                // Set the failure flag
                failed = true;

                // Try to parse the invalid node id
                if (!uint.TryParse(parserResult.Product.Elements["invalidnodeid"], out invalidNodeID))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "invalidnodeid");
            }

            // Now, make sure our mandatory arguments exist
            // If they do, try to parse them
            // Note, that this will try to fetch as much information as possible from a failed request
            // Therefore, every basic argument becomes optional if the requested node was invalid

            // TotNumElem
            if (!parserResult.Product.Elements.ContainsKey("totnumelem"))
            {
                if (!failed)
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "totnumelem");
            }
            else
            {
                if (!uint.TryParse(parserResult.Product.Elements["totnumelem"], out totNumElem))
                    if (!failed)
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "totnumelem");
            }

            // FromIndex
            if (!parserResult.Product.Elements.ContainsKey("fromindex"))
            {
                if (!failed)
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "fromindex");
            }
            else
            {
                if (!uint.TryParse(parserResult.Product.Elements["fromindex"], out fromIndex))
                    if (!failed)
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "fromindex");
            }

            // NumElem
            if (!parserResult.Product.Elements.ContainsKey("numelem"))
            {
                if (!failed)
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "numelem");
            }
            else
            {
                if (!uint.TryParse(parserResult.Product.Elements["numelem"], out numElem))
                    if (!failed)
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "numelem");
            }

            // Update ID
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
            {
                if (!failed)
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");
            }
            else
            {
                if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                    if (!failed)
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");
            }

            // Check, if the request failed and if true, return early
            if (failed)
                return Result<ContentDataSet>.SucceedProduct(result,
                    new ContentDataSet(null, totNumElem, fromIndex, numElem, updateID, alphanumeric, invalidNodeID, failed));

            // If required, perform some sanity checks on the data
            if (ValidateInput)
            {
                if (totNumElem < numElem)
                    return Result<ContentDataSet>.FailMessage(result, "totnumelem < numelem");
                if (fromIndex + numElem > totNumElem)
                    return Result<ContentDataSet>.FailMessage(result, "fromindex + numelem > totnumelem");
                if (parserResult.Product.List.Count != numElem)
                    return Result<ContentDataSet>.FailMessage(result, "Number of list items != numelem");
            }

            // Allocate a list for the items
            List<ContentData> items = new List<ContentData>();

            // Next, pay attention to the list items (yes, there are a lot of them)
            uint elementNo = 0;
            foreach (Dictionary<string, string> listItem in parserResult.Product.List)
            {
                // Increment the element ID to simplify fault-finding
                elementNo++;

                // Make sure that all our elements are non-null
                if (listItem == null)
                    continue;

                // First, attempt to parse the required fields
                // The base class requires the following values: Name, NodeID, ParentID and NodeType
                // Make sure that they exist - the other arguments are appended later
                if (!listItem.ContainsKey("name"))
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "name", elementNo);
                if (!listItem.ContainsKey("nodeid"))
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "nodeid", elementNo);
                if (!listItem.ContainsKey("parentid"))
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "parentid", elementNo);

                // Next, we parse these initial fields, as they are useful for determining and verifying the other fields
                string name;
                uint nodeID, parentID;
                NodeType nodeType = NodeType.Unknown;

                // Name
                if (string.IsNullOrWhiteSpace((name = listItem["name"])))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "name", elementNo);

                // NodeID
                if (!uint.TryParse(listItem["nodeid"], out nodeID))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "nodeid", elementNo);

                // ParentID
                if (!uint.TryParse(listItem["parentid"], out parentID))
                    return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "parentid", elementNo);

                // Verify the mutual exclusivity of the node being either a branch or being playable
                if (listItem.ContainsKey("playable") && listItem.ContainsKey("branch"))
                    return Result<ContentDataSet>.FailMessage(result, "Mutually exclusive parameters '{0}' and '{1}' present in item #{2}!", "playable", "branch", elementNo);

                // Check the type of the node to determine the further parsing
                // The remaining fields are basically optional but still contain essential information
                if (listItem.ContainsKey("playable"))
                {
                    // If desired, check if the node id might be valid
                    if (ValidateInput)
                        if (parentID == 0 || nodeID == 0)
                            return Result<ContentDataSet>.FailMessage(result, "Unexpected parameter '{0}' in item #{1}!", "playable", elementNo);

                    // The node is playable
                    nodeType = NodeType.Playable;

                    // Verify that all common fields are present
                    if (!listItem.ContainsKey("title"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "title");
                    if (!listItem.ContainsKey("url"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "url");
                    if (!listItem.ContainsKey("album"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "album");
                    if (!listItem.ContainsKey("trackno"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "trackno");
                    if (!listItem.ContainsKey("year"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "year");
                    if (!listItem.ContainsKey("likemusic"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "likemusic");
                    if (!listItem.ContainsKey("artist"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "artist");
                    if (!listItem.ContainsKey("genre"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}'!", "genre");

                    // Also check, if the optional DMMCookie is present
                    // For instance Radio recordings omit this parameter
                    bool hasDMMCookie = listItem.ContainsKey("dmmcookie");

                    // Parse all common fields
                    string title, url, album, artist, genre;
                    uint trackNo, year, dmmCookie = 0;
                    bool likeMusic;

                    // Title
                    if (string.IsNullOrEmpty((title = listItem["title"])))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "title", elementNo);
                    // URL
                    if (string.IsNullOrEmpty((url = listItem["url"])))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "url", elementNo);
                    // Album
                    if (string.IsNullOrEmpty((album = listItem["album"])))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "album", elementNo);
                    // TrackNo
                    if (!uint.TryParse(listItem["trackno"], out trackNo))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "trackno", elementNo);
                    // Year
                    if (!uint.TryParse(listItem["year"], out year))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "year", elementNo);
                    // LikeMusic
                    string rawLikeMusic = (string.IsNullOrWhiteSpace(listItem["likemusic"]) ? string.Empty : listItem["likemusic"].ToLower());
                    if (!(likeMusic = (rawLikeMusic == "true")) && rawLikeMusic != "false")
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as bool!", "likemusic", elementNo);
                    // Artist
                    if (string.IsNullOrEmpty((artist = listItem["artist"])))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "artist", elementNo);
                    // Genre
                    if (string.IsNullOrEmpty((genre = listItem["genre"])))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "genre", elementNo);
                    // DMMCookie
                    if (hasDMMCookie)
                        if (!uint.TryParse(listItem["dmmcookie"], out dmmCookie))
                            if (string.IsNullOrWhiteSpace(listItem["dmmcookie"]))
                                hasDMMCookie = false;
                            else
                                return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "dmmcookie", elementNo);

                    // If necessairy, validate the common fields
                    if (ValidateInput && !url.Trim().StartsWith("http://"))
                        return Result<ContentDataSet>.FailMessage(result, "url #{0} is no valid URL!", elementNo);

                    // Next, determine whether additional, fields concerning the album art are available
                    if (listItem.ContainsKey("albumarturl") && listItem.ContainsKey("albumarttnurl"))
                    {
                        // If so, parse these fields too
                        string albumArtUrl, albumArtTnUrl;

                        // AlbumArtURL
                        if (string.IsNullOrEmpty((albumArtUrl = listItem["albumarturl"])))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "albumarturl", elementNo);
                        // AlbumArtTnURL
                        if (string.IsNullOrEmpty((albumArtTnUrl = listItem["albumarttnurl"])))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "albumarttnurl", elementNo);

                        // If necessairy, validate the additional fields
                        if (ValidateInput)
                        {
                            if (!albumArtUrl.Trim().StartsWith("http://"))
                                return Result<ContentDataSet>.FailMessage(result, "albumarturl #{0} is no valid URL!", elementNo);
                            if (!albumArtTnUrl.Trim().StartsWith("http://"))
                                return Result<ContentDataSet>.FailMessage(result, "albumarttnurl #{0} is no valid URL!", elementNo);
                        }
                        
                        // Create and add the enhanced result object
                        items.Add(new ContentDataPlayableArt(name, nodeID, parentID, nodeType,
                            title, url, album, trackNo, likeMusic, artist, genre, hasDMMCookie, dmmCookie, albumArtUrl, albumArtTnUrl));
                    }
                    else // Create and add the basic result object
                        items.Add(new ContentDataPlayable(name, nodeID, parentID, nodeType,
                            title, url, album, trackNo, likeMusic, artist, genre, hasDMMCookie, dmmCookie));
                }
                else if (listItem.ContainsKey("branch"))
                {
                    // The node is a branch
                    nodeType = NodeType.Branch;

                    // First, try to match all fields that belong to the ContentDataBranch parent
                    // Check, if the icontype field exists
                    if (!listItem.ContainsKey("icontype"))
                        return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' in item #{1}!", "icontype", elementNo);

                    // And parse it
                    IconType iconType;
                    uint iconTypeRaw;

                    if (!uint.TryParse(listItem["icontype"], out iconTypeRaw))
                        return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "icontype", elementNo);
                    if (iconTypeRaw > (uint)IconType.AllTracks)
                        return Result<ContentDataSet>.FailMessage(result, "Out of bounds parameter '{0}' in item #{1}!", "icontype", elementNo);
                    iconType = (IconType)iconTypeRaw;

                    // Now, determine the other properties
                    if (listItem.ContainsKey("containertype"))
                    {
                        // The node appears to be the root node
                        // If desired, check if this is indeed the root node
                        if (ValidateInput)
                            if (parentID != 0 || nodeID == 0)
                                return Result<ContentDataSet>.FailMessage(result, "Unexpected parameter '{0}' in item #{1}!", "containertype", elementNo);

                        // Parse the additional parameter
                        ContainerType containerType;
                        uint containerTypeRaw;

                        if (!uint.TryParse(listItem["containertype"], out containerTypeRaw))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "containertype", elementNo);
                        if (containerTypeRaw > (uint)ContainerType.AllTracks)
                            return Result<ContentDataSet>.FailMessage(result, "Out of bounds parameter '{0}' in item #{1}!", "containertype", elementNo);
                        containerType = (ContainerType)containerTypeRaw;

                        // And add the node
                        items.Add(new ContentDataRoot(name, nodeID, parentID, nodeType, iconType, containerType));
                    }
                    else if (listItem.ContainsKey("nooftracks"))
                    {
                        // The node appears to be a playlist node
                        // If desired, check if the node id might be valid
                        if (ValidateInput)
                            if (parentID == 0 || nodeID == 0)
                                return Result<ContentDataSet>.FailMessage(result, "Unexpected parameter '{0}' in item #{1}!", "nooftracks", elementNo);

                        // Parse the additional parameter
                        uint noOfTracks;

                        // NoOfTracks
                        if (!uint.TryParse(listItem["nooftracks"], out noOfTracks))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as uint!", "nooftracks", elementNo);

                        // And add the node
                        items.Add(new ContentDataPlaylistNode(name, nodeID, parentID, nodeType, iconType, noOfTracks));
                    }
                    else if (listItem.ContainsKey("albumarturl") && listItem.ContainsKey("albumarttnurl"))
                    {
                        // The node appears to be an album node with album art
                        // If desired, check if the node id might be valid
                        if (ValidateInput)
                            if (parentID == 0 || nodeID == 0)
                                return Result<ContentDataSet>.FailMessage(result,
                                    "Unexpected parameter '{0}' or '{1}' in item #{2}!", "albumarturl", "albumarttnurl", elementNo);

                        // Parse the additional parameters
                        string albumArtUrl, albumArtTnUrl;

                        // AlbumArtURL
                        if (string.IsNullOrEmpty((albumArtUrl = listItem["albumarturl"])))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "albumarturl", elementNo);
                        // AlbumArtTnURL
                        if (string.IsNullOrEmpty((albumArtTnUrl = listItem["albumarttnurl"])))
                            return Result<ContentDataSet>.FailMessage(result, "Could not parse parameter '{0}' in item #{1} as string!", "albumarttnurl", elementNo);

                        // If necessairy, validate the additional fields
                        if (ValidateInput)
                        {
                            if (!albumArtUrl.Trim().StartsWith("http://"))
                                return Result<ContentDataSet>.FailMessage(result, "albumarturl #{0} is no valid URL!", elementNo);
                            if (!albumArtTnUrl.Trim().StartsWith("http://"))
                                return Result<ContentDataSet>.FailMessage(result, "albumarttnurl #{0} is no valid URL!", elementNo);
                        }

                        // And add the node
                        items.Add(new ContentDataAlbumNodeArt(name, nodeID, parentID, nodeType, iconType, albumArtUrl, albumArtTnUrl));
                    }
                    else
                    {
                        // The node is a standard branch node
                        // If desired, check if the node id might be valid
                        if (ValidateInput)
                            if (parentID == 0 || nodeID == 0)
                                return Result<ContentDataSet>.FailMessage(result, "Unexpected parameter '{0}' in item #{1}!", "branch", elementNo);

                        // Since all information is already collected, just add the node.
                        items.Add(new ContentDataBranch(name, nodeID, parentID, nodeType, iconType));
                    }
                }
                else
                    return Result<ContentDataSet>.FailMessage(result, "Could not locate parameter '{0}' or '{1}' in item #{2}!", "playable", "branch", elementNo);
            } // End of foreach-loop

            // Finally, return the response
            return Result<ContentDataSet>.SucceedProduct(result, new ContentDataSet(items.ToArray(), totNumElem, fromIndex, numElem, updateID, alphanumeric));
        }

        /*
        /// <summary>
        /// Parses the root node for it's container node IDs. This is an essential function to fetch a complete list of tracks.
        /// </summary>
        /// <param name="RootDataSet">Root node input, as returned by the parser.</param>
        /// <param name="RejectIncomplete">If set to true, an error will also be returned if some, but not all containers could be found.</param>
        /// <param name="SkipInvalidChilds">If set to true, invalid child nodes are skipped and the process is not aborted.</param>
        /// <returns>
        /// Returns a Result yielding a dictionary with all node ID namespaces and their names.
        /// If an error occured, the Result will reflect the error.
        /// </returns>
        public static Result<Dictionary<ContainerType, uint>> ParseRoot(ContentDataSet RootDataSet, bool RejectIncomplete = true, bool SkipInvalidChilds = false)
        {
            // TODO: Potentionally make a wrapper for this to do the XML parsing as well

            // Allocate the result object
            Result<Dictionary<ContainerType, uint>> result = new Result<Dictionary<ContainerType, uint>>();

            // Input sanity check
            if (RootDataSet == null)
                return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The root dataset is null!");

            // The node is simply invalid - no exception needs to be thrown here
            if (RootDataSet.InvalidNodeID > 0 || RootDataSet.NumElem == 0 || RootDataSet.TotNumElem == 0 || RootDataSet.ContentData == null)
                return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The node's parameters are invalid!");

            // In this case, the data set is also invalid, since there may not be zero elements inside the root node
            if (RootDataSet.ContentData.Count == 0)
                return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The node does not contain any child elements!");

            // Allocate the result dictionary
            Dictionary<ContainerType, uint> resultDict = new Dictionary<ContainerType, uint>();
            // Also run a loop-counter
            int counter = 0;

            // Loop through the list of returned items
            foreach (ContentData data in RootDataSet.ContentData)
            {
                // Check, whether the node is somehow zero
                if (data == null)
                {
                    // Increment the counter
                    counter++;

                    // Check if we can skip the invalid entry or if we have to return an error
                    if (SkipInvalidChilds)
                        continue;

                    // If we might not skip the error, we return an error
                    return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The child node {0} is null!", counter);
                }

                // If any of our elements has a parent ID that is non-zero, there must be a problem
                if (data.ParentID != 0)
                    return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The ID of the parent node is non-zero!");

                // Validate the child node's contents
                if (data.NodeID == 0 || data.ContainerType <= ContainerType.None || data.ContainerType > ContainerType.AllTracks)
                {
                    // Increment the counter
                    counter++;

                    // Check if we can skip the invalid entry or if we have to return an error
                    if (SkipInvalidChilds)
                        continue;

                    // If we might not skip the error, we return an error
                    return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "The parameters of child node {0} are invalid!", counter);
                }

                // Now, check if the item already exists and fail if true
                if (resultDict.ContainsKey(data.ContainerType))
                    return null;

                // If everything appears to be fine, add the element
                resultDict.Add(data.ContainerType, data.NodeID);

                // Increment the counter
                counter++;
            }

            // Check, if we don't require all containers to be present and in that case, return the dictionary early
            if (!RejectIncomplete && resultDict.Count > 0)
                return Result<Dictionary<ContainerType, uint>>.SucceedProduct(result, resultDict);

            // Finally, if required, check if all items are present
            if (!resultDict.ContainsKey(ContainerType.Albums) || !resultDict.ContainsKey(ContainerType.AllTracks) || !resultDict.ContainsKey(ContainerType.Artists)
                || !resultDict.ContainsKey(ContainerType.Genres) || !resultDict.ContainsKey(ContainerType.Playlists))
                return Result<Dictionary<ContainerType, uint>>.FailMessage(result, "Some required child nodes could not be found!");

            // On success, return the resulting and complete dictionary
            return Result<Dictionary<ContainerType, uint>>.SucceedProduct(result, resultDict);
        }*/

        // ContentDataSet-Structure:
        // elements:            Returned elements
        // totnumelem	(uint): Total number of elements that could potentionally be queried
        // fromindex	(uint): Copy of the request start index parameter
        // numelem		(uint):	Number of elements returned in this query
        // updateid		(uint): UNKNOWN! e.g. 422
        // invalidnodeid(uint): Only returned if the request failed. It will then return the queried, invalid, node id.
        public class ContentDataSet
        {
            /// <summary>
            /// List of returned elements.
            /// </summary>
            public ContentData[] ContentData;

            /// <summary>
            /// Total number of elements that could potentionally be queried.
            /// </summary>
            public readonly uint TotNumElem;
            
            /// <summary>
            /// Echo of the requested offset index to base the query on.
            /// </summary>
            public readonly uint FromIndex;
            
            /// <summary>
            /// Number of elements returned in this query.
            /// </summary>
            public readonly uint NumElem;
            
            /// <summary>
            /// Modification update ID.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Possibly indicates that the result is alphanumerically sorted by name.
            /// </summary>
            public readonly bool Alphanumeric;

            /// <summary>
            /// In case that the request failed, this echos the ID of the supplied invalid node.
            /// </summary>
            public readonly uint InvalidNodeID;
            
            /// <summary>
            /// Indicates whether the request failed.
            /// </summary>
            public readonly bool Failed;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="ContentData">List of returned elements.</param>
            /// <param name="TotNumElem">Total number of elements that could potentionally be queried.</param>
            /// <param name="FromIndex">Echo of the requested offset index to base the query on.</param>
            /// <param name="NumElem">Number of elements returned in this query.</param>
            /// <param name="UpdateID">Modification update ID.</param>
            /// <param name="Alphanumeric">Possibly indicates that the result is alphanumerically sorted by name.</param>
            /// <param name="InvalidNodeID">In case that the request failed, this echos the ID of the supplied invalid node.</param>
            /// <param name="Failed">Indicates whether the request failed.</param>
            internal ContentDataSet(ContentData[] ContentData, uint TotNumElem, uint FromIndex, uint NumElem, uint UpdateID, bool Alphanumeric,
                uint InvalidNodeID = 0, bool Failed = false)
            {
                this.ContentData = ContentData;
                this.TotNumElem = TotNumElem;
                this.FromIndex = FromIndex;
                this.NumElem = NumElem;
                this.UpdateID = UpdateID;
                this.Alphanumeric = Alphanumeric;
                this.InvalidNodeID = InvalidNodeID;
                this.Failed = Failed;
            }
        }

        /*
         * Track info
         * name             -- always present
         * title            -- always present
         * nodeid           -- always present
         *                  ++ for tracks, this can always be idmask'd to get the universal track id
         *                  ++ for artists, this can also be idmask'd to get the universal artist id, but only when using the Artists node (RequestArtistIndexTable)
         *                  ++ for albums, this can also be idmask'd to get the universal album id, but only when using the Albums node (RequestAlbumIndexTable)
         * parentid         -- always present; in All Songs, equal to said node; might be helpful to determine bitmasks
         * playable         -- always present; node - only for playlists/subitems
         * url              -- always present
         * album            -- optional, reads "No Album" if not present
         * [albumarturl]    -- optional, omitted if not present
         * [albumarttnurl]  -- optional, omitted if not present, always present with albumarturl
         * trackno          -- appears to be zero if not present in id3
         * year             -- appears to be zero if not present in id3
         * likemusic        -- always present
         * 
            <name>Sick of Me</name>
            <title>Sick of Me</title>
            <nodeid>402683184</nodeid>
            <parentid>385875968</parentid>
            <playable />
            <url>http://192.168.10.3:80/media/30/30000.mp3</url>
            <album>Aggressive (Deluxe Edition)</album>
            <albumarturl>http://192.168.10.3:80/jpeg/0/382.jpg</albumarturl>
            <albumarttnurl>http://192.168.10.3:80/jpeg/0/382.tn.jpg</albumarttnurl>
            <trackno>6</trackno>
            <year>2017</year>
            <likemusic>false</likemusic>
            <artist>Beartooth</artist>
            <genre>Hard Rock &amp; Metal</genre>
            <dmmcookie>735906930</dmmcookie>
        */

        // ContentData-Structure:
        // name		    (string):	Track title
        // nodeid	    (uint):	    Special ID number that has to be masked with & idmask to get the file's path
        // album	    (uint):	    ID number of the album set it belongs to
        // trackno	    (uint):	    Positional index of the track in the album (might potentionally be string not int)
        // artist	    (uint):	    ID number of the artist it belongs to
        // genre	    (uint):	    ID number of the genre it belongs to
        // year		    (uint):	    Year that the track was published / recorded in (might potentionally be string not int)
        // mediatype	(uint):	    Type of the media (refers to the urimetadata table of media types)
        // dmmcookie	(uint):	    UNKNOWN! e.g. 1644662629

        /// <summary>
        /// Indicates the type and function of a node.
        /// </summary>
        public enum NodeType : byte
        {
            /// <summary>
            /// The function is not known.
            /// </summary>
            Unknown,

            /// <summary>
            /// The node can be played back by the stereo and will yield a playable URL.
            /// </summary>
            Playable,

            /// <summary>
            /// The node is a branch and can be browsed.
            /// </summary>
            Branch
        }

        /// <summary>
        /// Indicates the programatically identified and verified type of the current container.
        /// </summary>
        public enum NodePath
        {
            /// <summary>
            /// Global root node.
            /// Path: Root (Branch)
            /// Prefix: 0000 0000
            /// </summary>
            Root,

            /// <summary>
            /// Top playlists node.
            /// Path: Root > Playlists (Branch)
            /// Prefix: 0001 1011
            /// </summary>
            Playlists,

            /// <summary>
            /// Top genres node.
            /// Path: Root > Genres (Branch)
            /// Prefix: 0001 0010
            /// </summary>
            Genres,

            /// <summary>
            /// Top all tracks node.
            /// Path: Root > All tracks (Branch)
            /// Prefix: 0001 0111
            /// </summary>
            AllTracks,
            
            /// <summary>
            /// Single track sub-node of all tracks.
            /// Path: Root > All tracks > Track (Playable)
            /// Prefix: 0001 1000
            /// </summary>
            AllTracks_Track,

            /// <summary>
            /// Top albums node.
            /// Path: Root > Albums (Branch)
            /// Prefix: 0000 0100
            /// </summary>
            Albums,

            /// <summary>
            /// Single album sub-node of albums.
            /// Path: Root > Albums > Album (Branch)
            /// Prefix: 0000 0101
            /// </summary>
            Albums_Album,

            /// <summary>
            /// Single track sub-node of single album node.
            /// Path: Root > Albums > Album > Track (Playable)
            /// Prefix: 0000 0110
            /// </summary>
            Albums_Album_Track,

            /// <summary>
            /// Top artists node.
            /// Path: Root > Artists (Branch)
            /// Prefix: 0000 1010
            /// </summary>
            Artists,

            /// <summary>
            /// Single artist sub-node of artists.
            /// Path: Root > Artists > Artist (Branch)
            /// Prefix: 0000 1011
            /// </summary>
            Artists_Artist,

            /// <summary>
            /// Single album sub node of single artist sub-node.
            /// Path: Root > Artists > Artist > Album (Branch)
            /// Prefix: 0000 1100
            /// </summary>
            Artists_Artist_Album,

            /// <summary>
            /// Single track sub node of single album sub-node.
            /// Path: Root > Artists > Artist > Album > Track (Playable)
            /// Prefix: 0000 1101
            /// </summary>
            Artists_Artist_Album_Track
        }

        /// <summary>
        /// Indicates the type and function of a container node.
        /// This enum matches the values returned in the containertype field.
        /// </summary>
        public enum ContainerType : sbyte
        {
            /// <summary>
            /// Indicates that no container type had been supplied.
            /// </summary>
            None = -1,

            /// <summary>
            /// Indicates the Playlist node.
            /// Available fields:
            /// Example node format: 0001 1011 XXXX XXXX XXXX XXXX XXXX XXXX
            /// </summary>
            Playlists = 0,

            /// <summary>
            /// Indicates the Albums node.
            /// Example node format: 0000 0100 XXXX XXXX XXXX XXXX XXXX XXXX
            /// </summary>
            Albums = 1,

            /// <summary>
            /// Indicates the Artists node.
            /// Example node format: 0000 1010 XXXX XXXX XXXX XXXX XXXX XXXX
            /// </summary>
            Artists = 2,

            /// <summary>
            /// Indicates the Genre mode.
            /// Example node format: 0001 0010 XXXX XXXX XXXX XXXX XXXX XXXX
            /// </summary>
            Genres = 3,

            /// <summary>
            /// Indicates the All tracks node.
            /// Example node format: 0001 0111 XXXX XXXX XXXX XXXX XXXX XXXX
            /// </summary>
            AllTracks = 4
        }

        /// <summary>
        /// Indicates the type of icon associated with a branch child node.
        /// This could be used in an interactive, branch-based media browser.
        /// </summary>
        public enum IconType : sbyte
        {
            /// <summary>
            /// Indicates that no icon type had been supplied.
            /// </summary>
            None = -1,
            
            /// <summary>
            /// Represents a Playlist icon.
            /// </summary>
            Playlist = 0,

            /// <summary>
            /// Represents an Album icon.
            /// </summary>
            Album = 1,

            /// <summary>
            /// Represents an Artist icon.
            /// </summary>
            Artist = 2,

            /// <summary>
            /// Represents a Genre icon.
            /// </summary>
            Genre = 3,

            /// <summary>
            /// Represents an All tracks icon.
            /// </summary>
            AllTracks = 4
        }

        /// <summary>
        /// Common ContentData node base class.
        /// </summary>
        public class ContentData
        {
            /// <summary>
            /// The name or title of the node. May be equal to the title for tracks.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The unique ID of this node.
            /// </summary>
            public readonly uint NodeID;

            /// <summary>
            /// Determines the path to the current node using it's node ID.
            /// </summary>
            public NodePath NodePath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            /// The unique ID of the parent node.
            /// </summary>
            public readonly uint ParentID;

            /// <summary>
            /// Determines the path to the parent node using it's node ID.
            /// </summary>
            public NodePath ParentPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            /// The type of the node (e.g. playable, branch, etc.).
            /// </summary>
            public readonly NodeType NodeType; // Node / Playable

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            internal ContentData(string Name, uint NodeID, uint ParentID, NodeType NodeType)
            {
                this.Name = Name;
                this.NodeID = NodeID;
                this.ParentID = ParentID;
                this.NodeType = NodeType;
            }
        }

        /// <summary>
        /// Class for playable nodes.
        /// </summary>
        public class ContentDataPlayable : ContentData
        {
            /// <summary>
            /// The title of the track.
            /// </summary>
            public readonly string Title;

            /// <summary>
            /// The public URI of the media file.
            /// </summary>
            public readonly string URL;

            /// <summary>
            /// The title of the album.
            /// </summary>
            public readonly string Album;

            /// <summary>
            /// The number of the track in the album.
            /// </summary>
            public readonly uint TrackNo;

            /// <summary>
            /// The year of release of the album.
            /// </summary>
            public readonly uint Year;

            /// <summary>
            /// Stores whether the track has the LikeMusic flag set.
            /// </summary>
            public readonly bool LikeMusic;

            /// <summary>
            /// The name of the artist(s) involved in the track.
            /// </summary>
            public readonly string Artist;

            /// <summary>
            /// The genre of the track.
            /// </summary>
            public readonly string Genre;

            /// <summary>
            /// Indicates whether a DMMCookie is present.
            /// </summary>
            public readonly bool HasDMMCookie;

            /// <summary>
            /// The DMMCookie. A so far unknown variable.
            /// </summary>
            public readonly uint DMMCookie;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="Title">The title of the track.</param>
            /// <param name="URL">The public URI of the media file.</param>
            /// <param name="Album">The title of the album.</param>
            /// <param name="TrackNo">The number of the track in the album.</param>
            /// <param name="LikeMusic">Stores whether the track has the LikeMusic flag set.</param>
            /// <param name="Artist">The name of the artist(s) involved in the track.</param>
            /// <param name="Genre">The genre of the track.</param>
            /// <param name="HasDMMCookie">Indicates whether a DMMCookie is present.</param>
            /// <param name="DMMCookie">The DMMCookie.</param>
            internal ContentDataPlayable(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                string Title, string URL, string Album, uint TrackNo, bool LikeMusic, string Artist, string Genre, bool HasDMMCookie, uint DMMCookie)
                : base(Name, NodeID, ParentID, NodeType)
            {
                this.Title = Title;
                this.URL = URL;
                this.Album = Album;
                this.TrackNo = TrackNo;
                this.LikeMusic = LikeMusic;
                this.Artist = Artist;
                this.Genre = Genre;
                this.HasDMMCookie = HasDMMCookie;
                this.DMMCookie = DMMCookie;
            }
        }

        /// <summary>
        /// Class for playable nodes with album art.
        /// </summary>
        public class ContentDataPlayableArt : ContentDataPlayable
        {
            /// <summary>
            /// The public URI of the track's album art. The linked file has to be decrypted.
            /// </summary>
            public readonly string AlbumArtURL;

            /// <summary>
            /// The public URI of the track's album art thumbnail. The linked file has to be decrypted.
            /// </summary>
            public readonly string AlbumArtTnURL;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="Title">The title of the track.</param>
            /// <param name="URL">The public URI of the media file.</param>
            /// <param name="Album">The title of the album.</param>
            /// <param name="TrackNo">The number of the track in the album.</param>
            /// <param name="LikeMusic">Stores whether the track has the LikeMusic flag set.</param>
            /// <param name="Artist">The name of the artist(s) involved in the track.</param>
            /// <param name="Genre">The genre of the track.</param>
            /// <param name="HasDMMCookie">Indicates whether a DMMCookie is present.</param>
            /// <param name="DMMCookie">The DMMCookie.</param>
            /// <param name="AlbumArtURL">The public URI of the track's album art.</param>
            /// <param name="AlbumArtTnURL">The public URI of the track's album art thumbnail.</param>
            internal ContentDataPlayableArt(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                string Title, string URL, string Album, uint TrackNo, bool LikeMusic, string Artist, string Genre, bool HasDMMCookie, uint DMMCookie,
                string AlbumArtURL, string AlbumArtTnURL)
                : base(Name, NodeID, ParentID, NodeType, Title, URL, Album, TrackNo, LikeMusic, Artist, Genre, HasDMMCookie, DMMCookie)
            {
                this.AlbumArtURL = AlbumArtURL;
                this.AlbumArtTnURL = AlbumArtTnURL;
            }
        }

        /// <summary>
        /// Common class for branch nodes.
        /// </summary>
        public class ContentDataBranch : ContentData
        {
            /// <summary>
            /// The type of icon associated with a branch child node to be used in an interactive media browser (e.g. Playlist, Album, etc.).
            /// </summary>
            public readonly IconType IconType;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="IconType">The type of icon associated with a branch child node.</param>
            internal ContentDataBranch(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                IconType IconType)
                : base(Name, NodeID, ParentID, NodeType)
            {
                this.IconType = IconType;
            }
        }

        /// <summary>
        /// Class of the root node.
        /// </summary>
        public class ContentDataRoot : ContentDataBranch
        {
            /// <summary>
            /// The container type. This is used to indicate the function of a root node without checking the potentionally language-specific label (e.g. Playlists, Albums, etc.).
            /// </summary>
            public readonly ContainerType ContainerType;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="IconType">The type of icon associated with a branch child node.</param>
            /// <param name="ContainerType">The container type indicating the node function.</param>
            internal ContentDataRoot(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                IconType IconType,
                ContainerType ContainerType)
                : base(Name, NodeID, ParentID, NodeType, IconType)
            {
                this.ContainerType = ContainerType;
            }
        }

        /// <summary>
        /// Class of the playlist nodes.
        /// </summary>
        public class ContentDataPlaylistNode : ContentDataBranch
        {
            /// <summary>
            /// The number of tracks contained within the current playlist. This appears to be broken and always stuck at zero.
            /// </summary>
            public readonly uint NoOfTracks;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="IconType">The type of icon associated with a branch child node.</param>
            /// <param name="NoOfTracks">The number of tracks contained within the current playlist.</param>
            internal ContentDataPlaylistNode(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                IconType IconType,
                uint NoOfTracks)
                : base(Name, NodeID, ParentID, NodeType, IconType)
            {
                this.NoOfTracks = NoOfTracks;
            }
        }

        /// <summary>
        /// Class of the album nodes with album art.
        /// </summary>
        public class ContentDataAlbumNodeArt : ContentDataBranch
        {
            /// <summary>
            /// The public URI of the album's art. The linked file has to be decrypted.
            /// </summary>
            public readonly string AlbumArtURL;

            /// <summary>
            /// The public URI of the album art's thumbnail. The linked file has to be decrypted.
            /// </summary>
            public readonly string AlbumArtTnURL;

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">The name or title of the node.</param>
            /// <param name="NodeID">The unique ID of this node.</param>
            /// <param name="ParentID">The unique ID of the parent node.</param>
            /// <param name="NodeType">The type of the node (e.g. playable, branch, etc.).</param>
            /// <param name="IconType">The type of icon associated with a branch child node.</param>
            /// <param name="NoOfTracks">The number of tracks contained within the current playlist.</param>
            /// <param name="AlbumArtURL">The public URI of the album's art.</param>
            /// <param name="AlbumArtTnURL">The public URI of the album art's thumbnail.</param>
            internal ContentDataAlbumNodeArt(
                string Name, uint NodeID, uint ParentID, NodeType NodeType,
                IconType IconType,
                string AlbumArtURL, string AlbumArtTnURL)
                : base(Name, NodeID, ParentID, NodeType, IconType)
            {
                this.AlbumArtURL = AlbumArtURL;
                this.AlbumArtTnURL = AlbumArtTnURL;
            }
        }
    }
}

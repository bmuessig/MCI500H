﻿using System;
using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This API call provides a DLNA-like folder and media browser with the ability to display cover art.
    /// Additionally, this is one of the single most important API calls to fetch data from the stero.
    /// Though this, cover art, direct URLs, titles, artists, genres, etc. can be fetched directly, which is particularily useful for display and playback.
    /// </summary>
    public static class RequestPlayableData
    {
        /// <summary>
        /// ContentDataSet Parser
        /// </summary>
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        /// <summary>
        /// Builds a RequestArtistIndexTable request from the supplied elements.
        /// </summary>
        /// <param name="NodeID">Parent node ID to fetch the child elements from.</param>
        /// <param name="NumElem">Maximum number of elements (0 returns all elements).</param>
        /// <returns></returns>
        public static string Build(uint NodeID, uint NumElem = 0)
        {
            int processedNumElem = (int)NumElem; // Potentional overflow here
            if (NodeID == 0 && NumElem == 0)
                processedNumElem = -1;

            return string.Format(
                "<requestplayabledata>"+
                "<nodeid>{0}</nodeid>"+
                "<numelem>{1}</numelem>"+
                "</requestplayabledata>",
                NodeID,
                NumElem);
        }

        /// <summary>
        /// Parses a ContentDataSet response.
        /// </summary>
        /// <param name="Response">Text response input from the stereo's server.</param>
        /// <param name="ValidateInput">Indicates whether to verify the contents received after parsing.</param>
        /// <param name="LazySyntax">Indicates whether minor parsing errors are ignored.</param>
        /// <returns></returns>
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

            // Now, make sure our mandatory arguments exist
            if (!result.Product.Elements.ContainsKey("totnumelem"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "totnumelem"));
            if (!result.Product.Elements.ContainsKey("fromindex"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "fromindex"));
            if (!result.Product.Elements.ContainsKey("numelem"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "numelem"));
            if (!result.Product.Elements.ContainsKey("updateid"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "updateid"));

            // Then, try to parse the parameters
            uint totNumElem, fromIndex, numElem, updateID;
            bool alphanumeric = result.Product.Elements.ContainsKey("alphanumeric");

            if (!uint.TryParse(result.Product.Elements["totnumelem"], out totNumElem))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "totnumelem"));
            if (!uint.TryParse(result.Product.Elements["fromindex"], out fromIndex))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "fromindex"));
            if (!uint.TryParse(result.Product.Elements["numelem"], out numElem))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "numelem"));
            if (!uint.TryParse(result.Product.Elements["updateid"], out updateID))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "updateid"));

            // If required, perform some sanity checks on the data
            if (ValidateInput)
            {
                if (totNumElem < numElem)
                    return new ActionResult<ContentDataSet>("totnumelem < numelem");
                if (fromIndex + numElem > totNumElem)
                    return new ActionResult<ContentDataSet>("fromindex + numelem > totnumelem");
                if (result.Product.List.Count != numElem)
                    return new ActionResult<ContentDataSet>("Number of list items != numelem");
            }

            // Allocate a list for the items
            List<ContentData> items = new List<ContentData>();

            // Next, pay attention to the list items (yes, there are a lot of them)
            uint elementNo = 0;
            foreach (Dictionary<string, string> listItem in result.Product.List)
            {
                // Increment the element ID to simplify fault-finding
                elementNo++;

                // Make sure that all our elements are non-null
                if (listItem == null)
                    continue;

                /*
                <contentdataset>
                    <contentdata>
                        <name>Playlists</name>
                        <nodeid>452984832</nodeid>
                        <containertype>0</containertype>
                        <icontype>0</icontype>
                        <parentid>0</parentid>
                        <branch />
                    </contentdata>
                    <contentdata>
                        <name>Artists</name>
                        <nodeid>167772160</nodeid>
                        <containertype>2</containertype>
                        <icontype>2</icontype>
                        <parentid>0</parentid>
                        <branch />
                    </contentdata>
                    <contentdata>
                        <name>Albums</name>
                        <nodeid>67108864</nodeid>
                        <containertype>1</containertype>
                        <icontype>1</icontype>
                        <parentid>0</parentid>
                        <branch />
                    </contentdata>
                    <contentdata>
                        <name>Genres</name>
                        <nodeid>301989888</nodeid>
                        <containertype>3</containertype>
                        <icontype>3</icontype>
                        <parentid>0</parentid>
                        <branch />
                    </contentdata>
                    <contentdata>
                        <name>All tracks</name>
                        <nodeid>385875968</nodeid>
                        <containertype>4</containertype>
                        <icontype>4</icontype>
                        <parentid>0</parentid>
                        <branch />
                    </contentdata>
                    <totnumelem>5</totnumelem>
                    <fromindex>0</fromindex>
                    <numelem>5</numelem>
                    <updateid>542</updateid>
                    <alphanumeric />
                </contentdataset>
                 */

                /*
            <contentdataset>
                <contentdata>
                    <name>Favorites</name>
                    <nodeid>469762179</nodeid>
                    <icontype>0</icontype>
                    <nooftracks>0</nooftracks>
                    <parentid>452984832</parentid>
                    <branch />
                </contentdata>
                <totnumelem>1</totnumelem>
                <fromindex>0</fromindex>
                <numelem>1</numelem>
                <updateid>542</updateid>
                <alphanumeric />
            </contentdataset>
                 */

                // First, make sure our mandatory arguments exist
                if (!listItem.ContainsKey("name"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "name", elementNo));
                if (!listItem.ContainsKey("title"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "title", elementNo));
                if (!listItem.ContainsKey("nodeid"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "nodeid", elementNo));
                if (!listItem.ContainsKey("parentid"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "parentid", elementNo));
                if (!listItem.ContainsKey("url"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "url", elementNo));
                if (!listItem.ContainsKey("album"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "album", elementNo));
                if (!listItem.ContainsKey("trackno"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "trackno", elementNo));
                if (!listItem.ContainsKey("year"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "year", elementNo));
                if (!listItem.ContainsKey("artist"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "artist", elementNo));
                if (!listItem.ContainsKey("genre"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "genre", elementNo));
                if (!listItem.ContainsKey("dmmcookie"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "dmmcookie", elementNo));

                if (!listItem.ContainsKey("containertype"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "containertype", elementNo));
                if (!listItem.ContainsKey("playable"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "playable", elementNo));
                if (!listItem.ContainsKey("albumarturl"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "albumarturl", elementNo));
                if (!listItem.ContainsKey("albumarttnurl"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "albumarttnurl", elementNo));
                if (!listItem.ContainsKey("likemusic"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "likemusic", elementNo));

                // Then, try to parse the parameters
                string name;
                uint nodeID, album, trackNo, artist, genre, year, mediaType, dmmCookie;
                if (string.IsNullOrWhiteSpace((name = listItem["name"])))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as string!", "name", elementNo));
                if (!uint.TryParse(listItem["nodeid"], out nodeID))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "nodeid", elementNo));
                if (!uint.TryParse(listItem["album"], out album))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "album", elementNo));
                if (!uint.TryParse(listItem["trackno"], out trackNo))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "trackno", elementNo));
                if (!uint.TryParse(listItem["artist"], out artist))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "artist", elementNo));
                if (!uint.TryParse(listItem["genre"], out genre))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "genre", elementNo));
                if (!uint.TryParse(listItem["year"], out year))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "year", elementNo));
                if (!uint.TryParse(listItem["mediatype"], out mediaType))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "mediatype", elementNo));
                if (!uint.TryParse(listItem["dmmcookie"], out dmmCookie))
                    return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' in item #{1} as uint!", "dmmcookie", elementNo));

                // If we need to, perform sanity checks on the input data
                if (ValidateInput)
                {
                    if (nodeID == 0)
                        return new ActionResult<ContentDataSet>(string.Format("nodeid #{0} == 0", elementNo));
                    if (album == 0)
                        return new ActionResult<ContentDataSet>(string.Format("album #{0} == 0", elementNo));
                    if (genre == 0)
                        return new ActionResult<ContentDataSet>(string.Format("genre #{0} == 0", elementNo));
                }

                // Finally, assemble and add the object
               // items.Add(new ContentData(name, nodeID, album, trackNo, artist, genre, year, mediaType, dmmCookie));
            }

            // Finally, return the response
            return new ActionResult<ContentDataSet>(new ContentDataSet(items, totNumElem, fromIndex, numElem, updateID));
        }

        // ContentDataSet-Structure:
        // elements:            Returned elements
        // totnumelem	(uint): Total number of elements that could potentionally be queried
        // fromindex	(uint): Copy of the request start index parameter
        // numelem		(uint):	Number of elements returned in this query
        // updateid		(uint): UNKNOWN! e.g. 422
        // invalidnodeid(uint): Only returned if the request failed. Then it will return the queried node id.
        public class ContentDataSet
        {
            public List<ContentData> ContentData;
            public readonly uint TotNumElem;
            public readonly uint FromIndex;
            public readonly uint NumElem;
            public readonly uint UpdateID;
            public readonly uint InvalidNodeID;

            internal ContentDataSet(uint TotNumElem, uint FromIndex, uint NumElem, uint UpdateID)
            {
                this.TotNumElem = TotNumElem;
                this.FromIndex = FromIndex;
                this.NumElem = NumElem;
                this.UpdateID = UpdateID;
            }

            internal ContentDataSet(List<ContentData> ContentData, uint TotNumElem, uint FromIndex, uint NumElem, uint UpdateID)
                : this(TotNumElem, FromIndex, NumElem, UpdateID)
            {
                this.ContentData = ContentData;
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
            /// 
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
        /// Parses the root node for it's container node IDs. This is an essential function to fetch a complete list of tracks.
        /// </summary>
        /// <param name="RootDataSet">Root node input, as returned by the parser.</param>
        /// <param name="RejectIncomplete">If set to true, an error will also be returned if some, but not all containers could be found.</param>
        /// <param name="SkipInvalidChilds">If set to true, invalid child nodes are skipped and the process is not aborted.</param>
        /// <returns>
        /// Returns an ActionResult yielding a dictionary with all node ID namespaces and their names.
        /// If an error occured, the ActionResult reflects the error.
        /// </returns>
        public static ActionResult<Dictionary<ContainerType, uint>> ParseRoot(ContentDataSet RootDataSet, bool RejectIncomplete = true, bool SkipInvalidChilds = false)
        {
            // Input sanity check
            if (RootDataSet == null)
                throw new ArgumentNullException("RootDataSet"); // TODO: Potentionally replace this with an action result

            // The node is simply invalid - no exception needs to be thrown here
            if (RootDataSet.InvalidNodeID > 0 || RootDataSet.NumElem == 0 || RootDataSet.TotNumElem == 0 || RootDataSet.ContentData == null)
                return new ActionResult<Dictionary<ContainerType, uint>>("The node's parameters are invalid!");

            // In this case, the data set is also invalid, since there may not be zero elements inside the root node
            if (RootDataSet.ContentData.Count == 0)
                return new ActionResult<Dictionary<ContainerType, uint>>("The node does not contain any child elements!");

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
                    return new ActionResult<Dictionary<ContainerType, uint>>(string.Format("The child node {0} is null!", counter));
                }

                // If any of our elements has a parent ID that is non-zero, there must be a problem
                if (data.ParentID != 0)
                    return new ActionResult<Dictionary<ContainerType,uint>>("The ID of the parent node is non-zero!");

                // Validate the child node's contents
                if (data.NodeID == 0 || data.ContainerType <= ContainerType.None || data.ContainerType > ContainerType.AllTracks)
                {
                    // Increment the counter
                    counter++;

                    // Check if we can skip the invalid entry or if we have to return an error
                    if (SkipInvalidChilds)
                        continue;
                    
                    // If we might not skip the error, we return an error
                    return new ActionResult<Dictionary<ContainerType, uint>>(string.Format("The parameters of child node {0} are invalid!", counter));
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
                return new ActionResult<Dictionary<ContainerType, uint>>(resultDict);

            // Finally, if required, check if all items are present
            if (!resultDict.ContainsKey(ContainerType.Albums) || !resultDict.ContainsKey(ContainerType.AllTracks) || !resultDict.ContainsKey(ContainerType.Artists)
                || !resultDict.ContainsKey(ContainerType.Genres) || !resultDict.ContainsKey(ContainerType.Playlists))
                return new ActionResult<Dictionary<ContainerType, uint>>("Some required child nodes could not be found!");

            // On success, return the resulting and complete dictionary
            return new ActionResult<Dictionary<ContainerType,uint>>(resultDict);
        }

        public class ContentData
        {
                /*"name"
                "title"
                "nodeid"
                "parentid"
                "url"
                "album"
                "trackno"
                "year"
                "artist"
                "genre"
                "dmmcookie"

                "containertype"
                "playable"
                "albumarturl"
                "albumarttnurl"
                "likemusic"*/

            /// <summary>
            /// The name or title of the node. May be equal to the title for tracks.
            /// </summary>
            public string Name;
            
            /// <summary>
            /// The title of the track.
            /// </summary>
            public string Title;
            
            /// <summary>
            /// The unique ID of this node.
            /// </summary>
            public uint NodeID;
            
            /// <summary>
            /// The unique ID of the parent node.
            /// </summary>
            public uint ParentID;
            
            /// <summary>
            /// The public URI to the media file.
            /// </summary>
            public string URL;

            /// <summary>
            /// The title of the album.
            /// </summary>
            public uint Album;

            /// <summary>
            /// The number of the track in the album.
            /// </summary>
            public uint TrackNo;

            /// <summary>
            /// The year of release of the album.
            /// </summary>
            public uint Year;

            /// <summary>
            /// The name of the artist(s) involved in the track.
            /// </summary>
            public uint Artist;

            /// <summary>
            /// The genre of the track.
            /// </summary>
            public uint Genre;

            /// <summary>
            /// The DMMCookie. A so far unknown variable.
            /// </summary>
            public uint DMMCookie;

            // -- Optional

            /// <summary>
            /// The container type. This is used to indicate the function of a root node without checking the potentionally language-specific label (e.g. Playlists, Albums, etc.).
            /// </summary>
            public ContainerType ContainerType = ContainerType.None;

            /// <summary>
            /// The type of icon associated with a branch child node to be used in an interactive media browser (e.g. Playlist, Album, etc.).
            /// </summary>
            public IconType IconType = IconType.None;

            /// <summary>
            /// The type of the node (e.g. playable, branch, etc.).
            /// </summary>
            public NodeType NodeType;

            /// <summary>
            /// The public URI to the track's album art. The linked file has to be decrypted.
            /// </summary>
            public string AlbumArtURL;

            /// <summary>
            /// The public URI to the track's album art thumbnail. The linked file has to be decrypted.
            /// </summary>
            public string AlbumArtTnURL;

            /// <summary>
            /// Stores whether the track has the LikeMusic flag set.
            /// </summary>
            public bool LikeMusic;

            internal ContentData()
            {
            }

            internal ContentData(string Name, string Title, uint NodeID, uint ParentID, string URL,
                uint Album, uint TrackNo, uint Year, uint Artist, uint Genre, uint DMMCookie)
            {
                this.Name = Name;
                this.NodeID = NodeID;
                this.Album = Album;
                this.TrackNo = TrackNo;
                this.Artist = Artist;
                this.Genre = Genre;
                this.Year = Year;
                this.DMMCookie = DMMCookie;
            }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return NodeID.ToString();
                return Name;
            }
        }
    }
}
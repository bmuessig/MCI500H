using System.Collections.Generic;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request is used to fetch title data in chunks or as a whole.
    /// It accepts both a start index (skip) and a max. items parameter (count).
    /// Using the parameters 0,0 will fetch all titles. This is not recommended for large databases.
    /// The stereo has limited RAM and processing capabilities and a database with 1000s of titles may overflow.
    /// It is recommended to fetch 100 titles at a time. The first request will return a total number of titles.
    /// This number can be used to generate the correct number of requests to fetch all titles successfully.
    /// The 0,0 method is not used by the official application, whereas the 100 element method is used.
    /// </summary>
    public static class RequestRawData
    {
        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);
        
        /// <summary>
        /// Assembles a RequestRawData request to be passed to the stereo.
        /// </summary>
        /// <param name="FromIndex">First index to be included into the query.</param>
        /// <param name="NumElem">Number of elements to be queried. Use zero to query all elements.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(uint FromIndex, uint NumElem = 0)
        {
            return string.Format(
                "<requestrawdata>" +
                "<requestparameters>" +
                "<fromindex>{0}</fromindex>" +
                "<numelem>{1}</numelem>" +
                "</requestparameters>" +
                "</requestrawdata>",
                FromIndex,
                NumElem);
        }

        /// <summary>
        /// Parses RequestRawData's ContentDataSet and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="ValidateInput">Indicates whether to validate the data values received.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
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
                if(totNumElem < numElem)
                    return new ActionResult<ContentDataSet>("totnumelem < numelem");
                if(fromIndex + numElem > totNumElem)
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

                // First, make sure our mandatory arguments exist
                if (!listItem.ContainsKey("name"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "name", elementNo));
                if (!listItem.ContainsKey("nodeid"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "nodeid", elementNo));
                if (!listItem.ContainsKey("album"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "album", elementNo));
                if (!listItem.ContainsKey("trackno"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "trackno", elementNo));
                if (!listItem.ContainsKey("artist"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "artist", elementNo));
                if (!listItem.ContainsKey("genre"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "genre", elementNo));
                if (!listItem.ContainsKey("year"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "year", elementNo));
                if (!listItem.ContainsKey("mediatype"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "mediatype", elementNo));
                if (!listItem.ContainsKey("dmmcookie"))
                    return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}' in item #{1}!", "dmmcookie", elementNo));

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
                items.Add(new ContentData(name, nodeID, album, trackNo, artist, genre, year, mediaType, dmmCookie));
            }

            // Finally, return the response
            return new ActionResult<ContentDataSet>(new ContentDataSet(items, totNumElem, fromIndex, numElem, updateID));
        }

        /// <summary>
        /// RequestRawData's ContentDataSet reply.
        /// </summary>
        public class ContentDataSet
        {
            /// <summary>
            /// List of returned elements.
            /// </summary>
            public List<ContentData> ContentData;

            /// <summary>
            /// Total number of elements that could potentionally be queried.
            /// </summary>
            public readonly uint TotNumElem;

            /// <summary>
            /// Echo of the request start index parameter.
            /// </summary>
            public readonly uint FromIndex;

            /// <summary>
            /// Number of elements returned in this query.
            /// </summary>
            public readonly uint NumElem;

            /// <summary>
            /// Unknown update ID.
            /// </summary>
            public readonly uint UpdateID;

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

        /// <summary>
        /// RequestRawData's ContentDataSet's ContentData.
        /// </summary>
        public class ContentData
        {
            /// <summary>
            /// Title of the track.
            /// </summary>
            public string Name;

            /// <summary>
            /// Universal track node ID number that has to be bitwise AND'ed with idmask.
            /// </summary>
            public uint NodeID;

            /// <summary>
            /// Universal album node ID number that has to be bitwise AND'ed with idmask.
            /// </summary>
            public uint Album;

            /// <summary>
            /// Positional index of the track in the album.
            /// </summary>
            public uint TrackNo;

            /// <summary>
            /// Universal artist node ID number that has to be bitwise AND'ed with idmask.
            /// </summary>
            public uint Artist;

            /// <summary>
            /// Universal genre node ID number that has to be bitwise AND'ed with idmask.
            /// </summary>
            public uint Genre;

            /// <summary>
            /// Year that the track was published / recorded in.
            /// </summary>
            public uint Year;

            /// <summary>
            /// File format of the media item (index into the urimetadata table of media types).
            /// </summary>
            public uint MediaType;

            /// <summary>
            /// Unknown DMMCookie. e.g. 1644662629.
            /// </summary>
            public uint DMMCookie;

            /// <summary>
            /// Parameterless internal constructor.
            /// </summary>
            internal ContentData()
            {
            }

            /// <summary>
            /// Internal constructor.
            /// </summary>
            /// <param name="Name">Title of the track.</param>
            /// <param name="NodeID">Universal track node ID number that has to be bitwise AND'ed with idmask.</param>
            /// <param name="Album">Universal album node ID number that has to be bitwise AND'ed with idmask.</param>
            /// <param name="TrackNo">Positional index of the track in the album.</param>
            /// <param name="Artist">Universal artist node ID number that has to be bitwise AND'ed with idmask.</param>
            /// <param name="Genre">Universal genre node ID number that has to be bitwise AND'ed with idmask.</param>
            /// <param name="Year">Year that the track was published / recorded in.</param>
            /// <param name="MediaType">File format of the media item (index into the urimetadata table of media types).</param>
            /// <param name="DMMCookie">Unknown DMMCookie. e.g. 1644662629.</param>
            internal ContentData(string Name, uint NodeID, uint Album, uint TrackNo, uint Artist, uint Genre, uint Year, uint MediaType, uint DMMCookie)
            {
                this.Name = Name;
                this.NodeID = NodeID;
                this.Album = Album;
                this.TrackNo = TrackNo;
                this.Artist = Artist;
                this.Genre = Genre;
                this.Year = Year;
                this.MediaType = MediaType;
                this.DMMCookie = DMMCookie;
            }

            /// <summary>
            /// Returns the string representation of the entry. Usually returns the album name, if available.
            /// </summary>
            /// <returns>A string representation of the object.</returns>
            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return NodeID.ToString();
                return Name;
            }
        }
    }
}

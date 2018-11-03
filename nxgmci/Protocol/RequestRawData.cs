using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using nxgmci.Parsers;

namespace nxgmci.Protocol
{
    public static class RequestRawData
    {
        // This request is used to fetch title data in chunks or as a whole.
        // It accepts both a start index (skip) and a max. items parameter (count).
        // Using the parameters 0,0 will fetch all titles. This is not recommended for large databases.
        // The stereo has limited RAM and processing capabilities and a database with 1000s of titles may overflow.
        // It is recommended to fetch 100 titles at a time. The first request will return a total number of titles.
        // This number can be used to generate the correct number of requests to fetch all titles successfully.
        // The 0,0 method is not used by the official application.

        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        // RequestRawData-Reqest:
        // fromindex (uint):  First index to be included into the response
        // numelem   (uint):  Number of elements to be included; 0 means all elements
        public static string Build(uint FromIndex, uint NumElem)
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

        // ContentDataSet-Response
        public static ActionResult<ContentDataSet> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return new ActionResult<ContentDataSet>("The response may not be null!");

            // Then, parse the response
            WADMResult result = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!result.Success)
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    return new ActionResult<ContentDataSet>(result.ErrorMessage);
                else
                    return new ActionResult<ContentDataSet>("The parsing failed for unknown reasons!");

            // And also make sure our state is correct
            if (result.Elements == null || result.List == null)
                return new ActionResult<ContentDataSet>("The list of parsed elements or list items is null!");

            // Now, make sure our mandatory arguments exist
            if (!result.Elements.ContainsKey("totnumelem"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "totnumelem"));
            if (!result.Elements.ContainsKey("fromindex"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "fromindex"));
            if (!result.Elements.ContainsKey("numelem"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "numelem"));
            if (!result.Elements.ContainsKey("updateid"))
                return new ActionResult<ContentDataSet>(string.Format("Could not locate parameter '{0}'!", "updateid"));
            
            // Then, try to parse the parameters
            uint totNumElem, fromIndex, numElem, updateID;
            if (!uint.TryParse(result.Elements["totnumelem"], out totNumElem))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "totnumelem"));
            if (!uint.TryParse(result.Elements["fromindex"], out fromIndex))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "fromindex"));
            if (!uint.TryParse(result.Elements["numelem"], out numElem))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "numelem"));
            if (!uint.TryParse(result.Elements["updateid"], out updateID))
                return new ActionResult<ContentDataSet>(string.Format("Could not parse parameter '{0}' as uint!", "updateid"));

            // If required, perform some sanity checks on the data
            if (ValidateInput)
            {
                if(totNumElem < numElem)
                    return new ActionResult<ContentDataSet>("totnumelem < numelem");
                if(fromIndex + numElem > totNumElem)
                    return new ActionResult<ContentDataSet>("fromindex + numelem > totnumelem");
                if (result.List.Count != numElem)
                    return new ActionResult<ContentDataSet>("Number of list items != numelem");
            }

            // Allocate a list for the items
            List<ContentData> items = new List<ContentData>();

            // Next, pay attention to the list items (yes, there are a lot of them)
            uint elementNo = 0;
            foreach (Dictionary<string, string> listItem in result.List)
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

        // ContentDataSet-Structure:
        // elements:            Returned elements
        // totnumelem	(uint): Total number of elements that could potentionally be queried
        // fromindex	(uint): Copy of the request start index parameter
        // numelem		(uint):	Number of elements returned in this query
        // updateid		(uint): UNKNOWN! e.g. 422
        public class ContentDataSet
        {
            public List<ContentData> ContentData;
            public readonly uint TotNumElem;
            public readonly uint FromIndex;
            public readonly uint NumElem;
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

        public class ContentData
        {
            public string Name;
            public uint NodeID;
            public uint Album;
            public uint TrackNo;
            public uint Artist;
            public uint Genre;
            public uint Year;
            public uint MediaType;
            public uint DMMCookie;

            internal ContentData()
            {
            }

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

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return NodeID.ToString();
                return Name;
            }
        }
    }
}

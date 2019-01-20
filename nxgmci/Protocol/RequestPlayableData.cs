using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol
{
    public static class RequestPlayableData
    {
        // This call provides a DLNA-like folder and media browser with the ability to display cover art.

        // ContentDataSet Parser
        private readonly static WADMParser parser = new WADMParser("contentdataset", "contentdata", true);

        // RequestArtistIndexTable-Reqest:
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NodeID">Parent node ID to fetch the child elements from</param>
        /// <param name="NumElem">Maximum number of elements (0 returns all elements)</param>
        /// <returns></returns>
        public static string Build(uint NodeID, uint NumElem)
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
            bool alphanumeric = result.Elements.ContainsKey("alphanumeric");

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
                if (totNumElem < numElem)
                    return new ActionResult<ContentDataSet>("totnumelem < numelem");
                if (fromIndex + numElem > totNumElem)
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

                /*
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

            public string Name;
            public string Title;
            public uint NodeID;
            public uint ParentID;
            public string URL;
            public uint Album;
            public uint TrackNo;
            public uint Year;
            public uint Artist;
            public uint Genre;
            public uint DMMCookie;

            public bool HasContainerType;
            public uint ContainerType;
            public bool Playable;
            public string AlbumArtURL;
            public string AlbumArtTnURL;
            public bool LikeMusic;

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

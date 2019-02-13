namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request returns some library statistics along the current update ID.
    /// </summary>
    public static class QueryDatabase
    {
        // QueryDatabase Parser
        private readonly static WADMParser parser = new WADMParser("querydatabase", "responseparameters", false);

        /// <summary>
        /// Assembles a QueryDatabase request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build()
        {
            return "<querydatabase></querydatabase>";
        }

        /// <summary>
        /// Parses QueryDatabase's ResponseParameters and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ResponseParameters> Parse(string Response, bool LazySyntax = false)
        {
            // Allocate the result object
            Result<ResponseParameters> result = new Result<ResponseParameters>();

            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return result.FailMessage("The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> parserResult = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!parserResult.Success)
                if (!string.IsNullOrWhiteSpace(parserResult.Message))
                    return result.Fail("The parsing failed!", parserResult.Error);
                else
                    return result.FailMessage("The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return result.FailMessage("The parsing product was null!");

            // And also make sure the state is correct
            if (parserResult.Product.Elements == null)
                return result.FailMessage("The list of parsed elements is null!");

            // Now, make sure the mandatory arguments exist
            if (!parserResult.Product.Elements.ContainsKey("noofplaylist"))
                return result.FailMessage("Could not locate parameter '{0}'!", "noofplaylist");
            if (!parserResult.Product.Elements.ContainsKey("noofartist"))
                return result.FailMessage("Could not locate parameter '{0}'!", "noofartist");
            if (!parserResult.Product.Elements.ContainsKey("noofalbum"))
                return result.FailMessage("Could not locate parameter '{0}'!", "noofalbum");
            if (!parserResult.Product.Elements.ContainsKey("noofgenre"))
                return result.FailMessage("Could not locate parameter '{0}'!", "noofgenre");
            if (!parserResult.Product.Elements.ContainsKey("nooftrack"))
                return result.FailMessage("Could not locate parameter '{0}'!", "nooftrack");
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return result.FailMessage("Could not locate parameter '{0}'!", "updateid");
            
            // Then, try to parse the parameters
            uint noOfPlaylist, noOfArtist, noOfAlbum, noOfGenre, noOfTrack, updateID;
            if (!uint.TryParse(parserResult.Product.Elements["noofplaylist"], out noOfPlaylist))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "noofplaylist");
            if (!uint.TryParse(parserResult.Product.Elements["noofartist"], out noOfArtist))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "noofartist");
            if (!uint.TryParse(parserResult.Product.Elements["noofalbum"], out noOfAlbum))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "noofalbum");
            if (!uint.TryParse(parserResult.Product.Elements["noofgenre"], out noOfGenre))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "noofgenre");
            if (!uint.TryParse(parserResult.Product.Elements["nooftrack"], out noOfTrack))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "nooftrack");
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "updateid");

            // Finally, return the response
            return result.Succeed(new ResponseParameters(noOfPlaylist, noOfArtist, noOfAlbum, noOfGenre, noOfTrack, updateID));
        }

        /// <summary>
        /// GetUpdateID's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The total number of playlists.
            /// </summary>
            public readonly uint NoOfPlaylist;

            /// <summary>
            /// The total number of artists.
            /// </summary>
            public readonly uint NoOfArtist;

            /// <summary>
            /// The total number of albums.
            /// </summary>
            public readonly uint NoOfAlbum;

            /// <summary>
            /// The total number of genres.
            /// </summary>
            public readonly uint NoOfGenre;

            /// <summary>
            /// The total number of tracks.
            /// </summary>
            public readonly uint NoOfTrack;

            /// <summary>
            /// The current update ID.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="NoOfPlaylist">The total number of playlists.</param>
            /// <param name="NoOfArtist">The total number of artists.</param>
            /// <param name="NoOfAlbum">The total number of albums.</param>
            /// <param name="NoOfGenre">The total number of genres.</param>
            /// <param name="NoOfTrack">The total number of tracks.</param>
            /// <param name="UpdateID">The current update ID.</param>
            internal ResponseParameters(uint NoOfPlaylist, uint NoOfArtist, uint NoOfAlbum, uint NoOfGenre, uint NoOfTrack, uint UpdateID)
            {
                this.NoOfPlaylist = NoOfPlaylist;
                this.NoOfArtist = NoOfArtist;
                this.NoOfAlbum = NoOfAlbum;
                this.NoOfGenre = NoOfGenre;
                this.NoOfTrack = NoOfTrack;
                this.UpdateID = UpdateID;
            }
        }
    }
}
